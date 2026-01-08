using Antlr.Runtime.Collections;
using Calculated_Fields.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
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
            Results ??= new Dictionary<int, string>();
        }

        [BindProperty(SupportsGet = true)]
        public List<TextField> AllFields { get; set; }
        public Dictionary<int,string> Results { get; set; }
        [BindProperty]
        public HashSet<int> GettingRenamed { get; set; } = new HashSet<int>();

        public async Task<IActionResult> OnGetAsync(){
            AllFields = await _context.TextField.ToListAsync();
            foreach (TextField field in AllFields) {
                if (field.type.Equals(Calculated_Fields.Models.Type.CALCULATED)) {
                    Calculater(field);
                }
            }
            return Page();
        }   

        public async Task<IActionResult> OnPostCalculateAsync() {
            foreach (TextField field in AllFields) {
                var toBeUpdated = _context.TextField.Find(field.Id);
                if (toBeUpdated != null) {
                    toBeUpdated.name = field.name;
                    toBeUpdated.value = field.value;
                    if (toBeUpdated.type.Equals(Calculated_Fields.Models.Type.CALCULATED)) {
                        Calculater(toBeUpdated);
                    }
                }
            }
            _context.SaveChanges();
            AllFields = await _context.TextField.ToListAsync();
            GettingRenamed.Clear();
            return Page();
        }
        
        /*public async Task<IActionResult> OnPostRenameAsync(int Id) {
            GettingRenamed.Add(Id);
            Console.WriteLine(GettingRenamed.Contains(1));
            return Page();
        }*/

        public async Task<IActionResult> OnPostAddFieldAsync(string name, string value, string type) {
            TextField newTextField = new TextField(name,value,(string.Equals(type,"on")? true:false));
            _context.TextField.Add(newTextField);
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveFieldAsync(int Id) {
            try {
                _context.TextField.Remove(_context.TextField.Find(Id));
                await _context.SaveChangesAsync();
            }
            catch {
                Console.WriteLine("Field with specific Id does not exist.");
            }
            return RedirectToPage();
        }

        public void Calculater(TextField toBeUpdated) {
            var expression = new Expression(toBeUpdated.value);
            bool valid = true;
            foreach (TextField field2 in AllFields) {
                if (field2.Id != toBeUpdated.Id) {
                    if (double.TryParse(field2.value, out double subResult))
                        expression.Parameters[field2.name] = double.Parse(field2.value);
                    else {
                        //this only works if the field was already calculated, if any field was used before calculation it will recieve a value of 0
                        if (Results.TryGetValue(field2.Id, out string resultValue) && double.TryParse(resultValue,out double ParsedResult))
                            expression.Parameters[field2.name] = ParsedResult;
                        else if (! Results.ContainsKey(field2.Id)) {
                            Results[toBeUpdated.Id] = "One of the fields was used before calculation.";
                            valid = false;
                            break;
                        }
                        else
                            expression.Parameters[field2.name] = 0;
                    }
                }
            }
            if (valid) {
                try {
                    Results[toBeUpdated.Id] = expression.Evaluate().ToString();
                }
                catch (NCalc.EvaluationException) {
                    Results[toBeUpdated.Id] = "You used invalid syntax.";
                }
                catch {
                    Results[toBeUpdated.Id] = "An unkown error has occurred while calculating.";
                }
            }
        }
    }
    
}
