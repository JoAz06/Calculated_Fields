using Calculated_Fields.Data;
using Calculated_Fields.Data.Exceptions;
using Calculated_Fields.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NCalc;
using NCalc.Exceptions;
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

        public List<TextField> AllFields { get; set; }
        public Dictionary<int, FieldResult> Results { get; set; }
        public List<FunctionInfo> Functions { get; set; } = new List<FunctionInfo> {
            new("Abs", "Abs()", "Abs(number)"),
            new("Acos", "Acos()", "Acos(number)"),
            new("Asin", "Asin()", "Asin(number)"),
            new("Atan", "Atan()", "Atan(number)"),
            new("Avg", "Avg(,)", "Avg(a,b,...)"),
            new("Ceiling", "Ceiling()", "Ceiling(number)"),
            new("Cos", "Cos()", "Cos(number)"),
            new("Exp", "Exp()", "Exp(number)"),
            new("Floor", "Floor()", "Floor(number)"),
            new("IEEERemainder", "IEEERemainder(,)", "IEEERemainder(numerator,devider)"),
            new("if", "if(,,)", "if(condition,true,false)"),
            new("Ln", "Ln()", "if(number)"),
            new("Log", "Log(,)", "Log(number,base)"),
            new("Log10", "Log10()", "Log10(number)"),
            new("Max", "Max(,)", "Max(a,b,...)"),
            new("Min", "Min(,)", "Min(a,b,...)"),
            new("Pow", "Pow(,)", "Pow(number,power)"),
            new("Round", "Round()", "Round(number)"),
            new("Sign", "Sign()", "Sign(number)"),
            new("Sin", "Sin()", "Sin(number)"),
            new("Sqrt", "Sqrt()", "Sqrt(number)"),
            new("Sum", "Sum(,)", "Sum(a,b,...)"),
            new("Tan", "Tan()", "Tan(number)"),
            new("Truncate", "Truncate()", "Truncate(number)")
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
            AllFields = await _context.TextField.ToListAsync();
            foreach (var field in AllFields) {
                var key = $"fieldValue_{field.Id}";
                if (Request.Form.TryGetValue(key, out var value)) {
                    if (value.ToString().Equals(null) || value.Equals("")) {
                        Results[field.Id] = new FieldResult(false, "#NULL!");
                        field.value = 0.ToString();
                    }
                    else {
                        field.value = value;
                        field.name = Request.Form[$"fieldName_{field.Id}"].ToString().Trim();
                        if (field.type == Calculated_Fields.Models.Type.CALCULATED) {
                            Calculate(field);
                        }   
                    }
                }
            }

            await _context.SaveChangesAsync();
            AllFields = await _context.TextField.ToListAsync();
            return Page();
        }

        public async Task<IActionResult> OnGetUpdateAsync(int Id, string name, string value) {
            Console.WriteLine(value);
            AllFields = await _context.TextField.ToListAsync();
            if (! AllFields.Any(x => x.Id == Id)) {
                return new JsonResult(new {
                    success = false,
                    error = "TextField does not exist"
                }) {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
            else if (string.IsNullOrWhiteSpace(name)) {
                return new JsonResult(new {
                    success = false,
                    error = "Invalid name"
                }) {
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
            else if (string.IsNullOrWhiteSpace(value)) {
                value = 0.ToString();
            }
            TextField toBeUpdated = _context.TextField.Find(Id);
            toBeUpdated.name = name.Trim();
            toBeUpdated.value = value;
            _context.TextField.Update(toBeUpdated);
            await _context.SaveChangesAsync();
            Calculate(toBeUpdated);
            return new JsonResult(new {
                success = true,
                data = Results[Id]
            }) {
                StatusCode = StatusCodes.Status200OK
            };
        }

        public async Task<IActionResult> OnPostAddFieldAsync(string name, string value, string type) {
            AllFields = await _context.TextField.ToListAsync();
            if (string.IsNullOrWhiteSpace(name) || AllFields.Contains(new TextField(name, "0", false))) {
                return RedirectToPage();
            }
            else if (string.IsNullOrWhiteSpace(value)) {
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
            catch (Exception ex) {
                switch (ex.GetType().Name) {
                    case "CircularException":
                        Results[toBeUpdated.Id] = new FieldResult(false, "#CIRCULAR!");
                    break;
                    case "NCalcFunctionNotFoundException":
                        Results[toBeUpdated.Id] = new FieldResult(false, "#NAME?");
                        break;
                    case "NCalcParameterNotDefinedException":
                        Results[toBeUpdated.Id] = new FieldResult(false, "#REF!");
                        break;
                    case "NCalcParserException":
                        Results[toBeUpdated.Id] = new FieldResult(false, "#SYNTAX!");
                        break;
                    case "NCalcEvaluationException":
                        Results[toBeUpdated.Id] = new FieldResult(false, "#VALUE!");
                        break;
                    case "FormatException":
                        Results[toBeUpdated.Id] = new FieldResult(false, "#CALC!");
                        break;
                    default:
                        Results[toBeUpdated.Id] = new FieldResult(false, ex.Message);
                    break;
                }
            }
        }

        private FieldResult Calculate(TextField toBeUpdated, HashSet<string> callStack) {
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
                        throw new NCalcParameterNotDefinedException(name);
                    }
                    else {
                        FieldResult result = Calculate(internalField, callStack);
                        if (result.valid) {
                            args.Result = result.result;
                        }
                        else if (result.result.Equals("#CIRCULAR!")) {
                            throw new CircularException();
                        }
                        else if (result.result.Equals("#REF!")) {
                            throw new NCalcParameterNotDefinedException("#REF!");
                        }
                    }
                };
                expression.EvaluateFunction += (name, args) =>
                {
                    switch (name.ToLower()) {
                        case "sum":
                            if (args.Parameters.Length < 1) {
                                throw new NCalcParserException("#SYNTAX!");
                            }
                            double sum = 0;
                            foreach (var argument in args.Parameters) {
                                sum+= double.Parse(argument.Evaluate().ToString()!);
                            }
                            args.Result = sum;
                        break;
                        case "avg":
                            if (args.Parameters.Length < 1) {
                                throw new NCalcParserException("#SYNTAX!");
                            }
                            sum = 0;
                            foreach (var argument in args.Parameters) {
                                sum += double.Parse(argument.Evaluate().ToString()!);
                            }
                            args.Result = sum/args.Parameters.Length;
                        break;
                        case "max":
                            if (args.Parameters.Length < 1) {
                                throw new NCalcParserException("#SYNTAX!");
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
                        case "min":
                            if (args.Parameters.Length < 1) {
                                throw new NCalcParserException("#SYNTAX!");
                            }
                            double min = double.NaN;
                            foreach (var argument in args.Parameters) {
                                if (min.Equals(double.NaN))
                                    min = double.Parse(argument.Evaluate().ToString()!);
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
                string result;
                result = expression.Evaluate().ToString();
                if(result == null) {
                    throw new Exception("Null");
                }
                callStack.Remove(toBeUpdated.name);
                Results[toBeUpdated.Id] = new FieldResult(true, result.ToString());
                return Results[toBeUpdated.Id];
            }
        }
    }
}
