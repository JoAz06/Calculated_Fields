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
            if (string.IsNullOrWhiteSpace(name)) {
                return RedirectToPage();
            }
            if (string.IsNullOrWhiteSpace(value)) {
                value = 0.ToString();
            }
            TextField newTextField = new(name.Trim(),value,(string.Equals(type,"on") ? true:false));
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
                    double result;
                    try {
                        result = double.Parse(expression.Evaluate().ToString()!);
                    }catch (NCalc.EvaluationException ex) {
                        Results[toBeUpdated.Id] = new FieldResult(false, "Invalid Syntax");
                        throw ex;
                    }
                    callStack.Remove(toBeUpdated.name);
                    Results[toBeUpdated.Id] = new FieldResult(true, result.ToString());
                    return Results[toBeUpdated.Id];
                }
            }
            catch (CircularException ex) {
                Results[toBeUpdated.Id] = new FieldResult(false, "Circular reference occured");
                throw ex;
            }
            catch (MissingFieldException ex) {
                Results[toBeUpdated.Id] = new FieldResult(false, "Variable does not exist");
                throw ex;
            }
        }
    }
}
