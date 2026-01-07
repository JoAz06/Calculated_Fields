using Calculated_Fields.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using NCalc;

namespace Calculated_Fields.Pages{
    
    public class CalculateModel : PageModel{
        private readonly Calculated_Fields.Data.CalculatedTextFieldContext _context;
        public CalculateModel(Calculated_Fields.Data.CalculatedTextFieldContext context)
        {
            _context = context; 
        }
        [BindProperty(SupportsGet = true)]
        public TextField text1 { get; set; }
        [BindProperty]
        public bool ShowRename1 { get; set; } = false;
        [BindProperty(SupportsGet = true)]
        public TextField text2 { get; set; }
        [BindProperty]
        public bool ShowRename2 { get; set; } = false;
        [BindProperty(SupportsGet = true)]
        public TextField text3 { get; set; }
        [BindProperty]
        public bool ShowRename3 { get; set; } = false;
        public string result;
        public async Task<IActionResult> OnGetAsync()
        {
            text1 = _context.TextField.FirstOrDefault(x => x.Id == 1);
            text2 = _context.TextField.FirstOrDefault(x => x.Id == 2);
            text3 = _context.TextField.FirstOrDefault(x => x.Id == 3);
            var expression = new Expression(text3.value);
            expression.Parameters[text1.name] = double.Parse(text1.value);
            expression.Parameters[text2.name] = double.Parse(text2.value);
            try
            {
                result = expression.Evaluate().ToString();
            }
            catch
            {
                result = "Error in calculation";
            }
            ShowRename1 = false;
            ShowRename2 = false;
            ShowRename3 = false;
            return Page();
        }   

        public async Task<IActionResult> OnPostCalculateAsync() {
            _context.TextField.FirstOrDefault(x => x.Id == 1).value = text1.value;
            _context.TextField.FirstOrDefault(x => x.Id == 2).value = text2.value;
            _context.TextField.FirstOrDefault(x => x.Id == 3).value = text3.value;
            _context.TextField.FirstOrDefault(x => x.Id == 1).name = text1.name;
            _context.TextField.FirstOrDefault(x => x.Id == 2).name = text2.name;
            _context.TextField.FirstOrDefault(x => x.Id == 3).name = text3.name;
            _context.SaveChanges();
            text1 = _context.TextField.FirstOrDefault(x => x.Id == 1);
            text2 = _context.TextField.FirstOrDefault(x => x.Id == 2);
            text3 = _context.TextField.FirstOrDefault(x => x.Id == 3);
            var expression = new Expression(text3.value);
            
            expression.Parameters[text1.name] = double.Parse(text1.value);
            expression.Parameters[text2.name] = double.Parse(text2.value);
            try { 
            result = expression.Evaluate().ToString();
            }
            catch
            {
                result = "Error in calculation";
            }
            ShowRename1 = false;
            ShowRename2 = false;
            ShowRename3 = false;
            return Page();
        }

        public async Task<IActionResult> OnPostRename1Async()
        {
            ShowRename1= true;
            return Page();
        }

        public async Task<IActionResult> OnPostRename2Async()
        {
            ShowRename2 = true;
            return Page();
        }

        public async Task<IActionResult> OnPostRename3Async()
        {
            ShowRename3 = true;
            return Page();
        }
    }
    
}
