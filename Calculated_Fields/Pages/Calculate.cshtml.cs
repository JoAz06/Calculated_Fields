using Antlr.Runtime.Collections;
using Calculated_Fields.Data;
using Calculated_Fields.Data.Exceptions;
using Calculated_Fields.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NCalc;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Security.Cryptography.Xml;

namespace Calculated_Fields.Pages{
    
    public class CalculateModel : PageModel {
        private readonly Calculated_Fields.Data.CalculatedTextFieldContext _context;
        public CalculateModel(Calculated_Fields.Data.CalculatedTextFieldContext context)
        {
            _context = context;
            AllFields ??= new List<TextField>();
            Results ??= new Dictionary<int, FieldResult>();
        }

        [BindProperty(SupportsGet = true)]
        public List<TextField> AllFields { get; set; }
        public Dictionary<int, FieldResult> Results { get; set; }

        public List<(string name, string equation, string tooltip)> Functions = new List<(string name, string, string tooltip)> {
            ("Abs", "Abs()", "Abs(number)"),
            ("Acos", "Acos()", "Acos(number)"),
            ("Asin", "Asin()", "Asin(number)"),
            ("Atan", "Atan()", "Atan(number)"),
            ("Average (Custom)", "Average(,)", "Average(a,b,...)"),
            ("Ceiling", "Ceiling()", "Ceiling(number)"),
            ("Cos", "Cos()", "Cos(number)"),
            ("Exp", "Exp()", "Exp(number)"),
            ("Floor", "Floor()", "Floor(number)"),
            ("IEEERemainder", "IEEERemainder(,)", "IEEERemainder(numerator,devider)"),
            ("if", "if(,,)", "if(condition,true,false)"),
            ("Ln", "Ln()", "if(number)"),
            ("Log", "Log(,)", "Log(number,base)"),
            ("Log10", "Log10()", "Log10(number)"),
            ("Max (Custom)", "Max(,)", "Max(a,b,...)"),
            ("Min (Custom)", "Min(,)", "Min(a,b,...)"),
            ("Pow", "Pow(,)", "Pow(number,power)"),
            ("Round", "Round()", "Round(number)"),
            ("Sign", "Sign()", "Sign(number)"),
            ("Sin", "Sin()", "Sin(number)"),
            ("Sqrt", "Sqrt()", "Sqrt(number)"),
            ("Sum (Custom)", "Sum(,)", "Sum(a,b,...)"),
            ("Tan", "Tan()", "Tan(number)"),
            ("Truncate", "Truncate()", "Truncate(number)")
        };

        public async Task<IActionResult> OnGetAsync() {
            AllFields = await _context.TextField.ToListAsync();
            foreach (TextField field in AllFields) {
                if (field.type.Equals(Calculated_Fields.Models.Type.CALCULATED)) {
                    Calculate(field);
                }
            }
            return Page();
        }

        public async Task<IActionResult> OnPostCalculateAsync() {
            foreach (TextField field in AllFields) {
                var toBeUpdated = _context.TextField.Find(field.Id);
                if (toBeUpdated != null) {
                    toBeUpdated.name = field.name.Trim();
                    toBeUpdated.value = field.value;
                    if (toBeUpdated.type.Equals(Calculated_Fields.Models.Type.CALCULATED)) {
                        Calculate(toBeUpdated);
                    }
                }
            }
            _context.SaveChanges();
            AllFields = await _context.TextField.ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAddFieldAsync(string name, string value, string type) {
            AllFields = await _context.TextField.ToListAsync();
            if (string.IsNullOrWhiteSpace(name) || AllFields.Contains(new TextField(name, "0", false))) {
                return RedirectToPage();
            }
            if (string.IsNullOrWhiteSpace(value)) {
                value = 0.ToString();
            }
            TextField newTextField = new(name.Trim(),value,string.Equals(type,"on"));
            _context.TextField.Add(newTextField);
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveFieldAsync(int Id) {
            if(_context.TextField.Find(Id) != null) {
                _context.TextField.Remove(_context.TextField.Find(Id)!);
                await _context.SaveChangesAsync();
            }
            else {
                Console.WriteLine("Field with specific Id does not exist.");
            }
            return RedirectToPage();
        }

        public void Calculate(TextField toBeUpdated) {
            HashSet<string> callStack = new();
            try {
                Results[toBeUpdated.Id] = Calculate(toBeUpdated, callStack);
            }
            catch (CircularException ex) {
                Results[toBeUpdated.Id] = new FieldResult(false, "Circular reference occured");
            }
            catch (MissingFieldException ex) {
                Results[toBeUpdated.Id] = new FieldResult(false, "Variable does not exist");
            }
            catch (NCalc.EvaluationException ex) {
                Results[toBeUpdated.Id] = new FieldResult(false, "Invalid Syntax");
            }
            catch (Exception ex) {
                Results[toBeUpdated.Id] = new FieldResult(false, ex.Message);
            }
        }

        private FieldResult Calculate(TextField toBeUpdated, HashSet<string> callStack) {
            try {
                if (Results.ContainsKey(toBeUpdated.Id)) {
                    return Results[toBeUpdated.Id];
                }
                else if (callStack.Contains(toBeUpdated.name)) {
                    throw new CircularException();
                }
                else {
                    callStack.Add(toBeUpdated.name);
                    var expression = new Expression(toBeUpdated.value);
                    expression.EvaluateParameter += (name, args) => {
                        TextField internalField = AllFields.FirstOrDefault(field => field.name == name);
                        if (internalField == null) {
                            throw new MissingFieldException();
                        }
                        else {
                            FieldResult result = Calculate(internalField, callStack);
                            if (result.valid) {
                                args.Result = double.Parse(Calculate(internalField, callStack).result);
                            }
                            else if (result.result.Equals("Circular reference occured")) {
                                throw new CircularException();
                            }
                            else if (result.result.Equals("Variable does not exist")) {
                                throw new MissingFieldException();
                            }
                        }
                    };
                    expression.EvaluateFunction += (name, args) =>
                    {
                        switch (name) {
                            case "Sum":
                                if (args.Parameters.Length < 2) {
                                    throw new Exception("Minimum number of arguments is 2.");
                                }
                                double sum = 0;
                                foreach (var argument in args.Parameters) {
                                    sum+= double.Parse(argument.Evaluate().ToString()!);
                                }
                                args.Result = sum;
                                break;
                            case "Avg":
                                if (args.Parameters.Length < 2) {
                                    throw new Exception("Minimum number of arguments is 2.");
                                }
                                sum = 0;
                                foreach (var argument in args.Parameters) {
                                    sum += double.Parse(argument.Evaluate().ToString()!);
                                }
                                args.Result = sum/args.Parameters.Length;
                                break;
                            case "Max":
                                if (args.Parameters.Length < 2) {
                                    throw new Exception("Minimum number of arguments is 2.");
                                }
                                double max = double.NaN;
                                foreach (var argument in args.Parameters) {
                                    if(max.Equals(double.NaN))
                                        max = double.Parse(argument.Evaluate().ToString()!);
                                    else {
                                        double current = double.Parse(argument.Evaluate().ToString()!);
                                        if (current > max) {
                                            max = current;
                                        }
                                    }
                                }
                                args.Result = max;
                                break;
                            case "Min":
                                if (args.Parameters.Length < 2) {
                                    throw new Exception("Minimum number of arguments is 2.");
                                }
                                double min = double.NaN;
                                foreach (var argument in args.Parameters) {
                                    if (min.Equals(double.NaN))
                                        max = double.Parse(argument.Evaluate().ToString()!);
                                    else {
                                        double current = double.Parse(argument.Evaluate().ToString()!);
                                        if (current < min) {
                                            min = current;
                                        }
                                    }
                                }
                                args.Result = min;
                                break;
                        }
                    };

                    double result;
                    try {
                        result = double.Parse(expression.Evaluate().ToString()!);
                    }catch (NCalc.EvaluationException ex) {
                        Results[toBeUpdated.Id] = new FieldResult(false, "Invalid Syntax");
                        throw ex;
                    }catch(Exception ex) {
                        Results[toBeUpdated.Id] = new FieldResult(false, ex.Message);
                        throw ex;
                    }
                    callStack.Remove(toBeUpdated.name);
                    Results[toBeUpdated.Id] = new FieldResult(true, result.ToString());
                    return Results[toBeUpdated.Id];
                }
            }
            catch (Exception ex) {
                Results[toBeUpdated.Id] = new FieldResult(false, ex.Message);
                throw ex;
            }
        }
    }
}
