using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using ContosoUniversity.Data;
using ContosoUniversity.Models;
using ContosoUniversity.ViewModels;

namespace ContosoUniversity.Pages.Students
{
    public class CreateVMModel : PageModel
    {
        private readonly ContosoUniversity.Data.SchoolContext _context;

        public CreateVMModel(ContosoUniversity.Data.SchoolContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            StudentVM = new StudentVM {
                EnrollmentDate = DateTime.Now,
                FirstMidName = "Joe VM",
                LastName = "Smith VM"
            };

            return Page();
        }

        [BindProperty]
        public StudentVM StudentVM { get; set; }

        // To protect from overposting attacks, see https://aka.ms/RazorPagesCRUD
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var entry = _context.Add(new Student());
            entry.CurrentValues.SetValues(StudentVM);
            await _context.SaveChangesAsync();
            return RedirectToPage("./Index");
        }
    }
}
