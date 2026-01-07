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
            GettingRenamed ??= new HashSet<int>();
        }

        [BindProperty(SupportsGet = true)]
        public List<TextField> AllFields { get; set; }
        public Dictionary<int,string> Results { get; set; }
        public HashSet<int> GettingRenamed { get; set; }

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
            GettingRenamed ??= new HashSet<int>();
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
            return Page();
        }

        public async Task<IActionResult> OnPostRenameAsync(int Id) {
            GettingRenamed.Add(Id);
            Console.WriteLine(GettingRenamed.Contains(1));
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddField() {
            return Page();
        }

        public void Calculater(TextField toBeUpdated) {
            var expression = new Expression(toBeUpdated.value);
            foreach (TextField field2 in AllFields) {
                if (field2.Id != toBeUpdated.Id)
                    expression.Parameters[field2.name] = double.Parse(field2.value);
            }
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
