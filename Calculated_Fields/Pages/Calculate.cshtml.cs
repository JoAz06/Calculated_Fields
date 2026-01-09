using Antlr.Runtime.Collections;
using Calculated_Fields.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NCalc;
using System.Collections;
using System.Diagnostics.Eventing.Reader;

namespace Calculated_Fields.Pages{
    
    public class CalculateModel : PageModel{
        private readonly Calculated_Fields.Data.CalculatedTextFieldContext _context;
        public CalculateModel(Calculated_Fields.Data.CalculatedTextFieldContext context)
        {
            _context = context;
            AllFields ??= new List<TextField>();
            Results ??= new Dictionary<int, double>();
        }

        [BindProperty(SupportsGet = true)]
        public List<TextField> AllFields { get; set; }
        public Dictionary<int,double> Results { get; set; }

        public async Task<IActionResult> OnGetAsync(){
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
            Results[toBeUpdated.Id] = Calculate(toBeUpdated, callStack);
        }

        private double Calculate(TextField toBeUpdated, HashSet<string> callStack) {
            if(Results.ContainsKey(toBeUpdated.Id)) {
                Console.WriteLine("Value was used from cache");
                return Results[toBeUpdated.Id];

            }
            if (callStack.Contains(toBeUpdated.name)) {
                throw new Exception("Circular reference occured");
            }
            callStack.Add(toBeUpdated.name);
            var expression = new Expression(toBeUpdated.value);
            expression.EvaluateParameter += (name, args) =>
            {
                TextField internalField = AllFields.FirstOrDefault(field => field.name == name)!;
                if (internalField == null) {
                    throw new Exception("Variable does not exist");
                }
                else {
                    args.Result = Calculate(internalField,callStack);
                }
            };
            double result = double.Parse(expression.Evaluate().ToString()!);
            callStack.Remove(toBeUpdated.name);
            Results[toBeUpdated.Id] = result;
            return result;
        }
    }
    
}
