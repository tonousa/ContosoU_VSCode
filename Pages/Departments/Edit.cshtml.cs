using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Data;
using ContosoUniversity.Models;

namespace ContosoUniversity.Pages.Departments
{
    public class EditModel : PageModel
    {
        private readonly ContosoUniversity.Data.SchoolContext _context;

        public EditModel(ContosoUniversity.Data.SchoolContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Department Department { get; set; }
        public SelectList InstructorNameSL { get; set; } // replaces viewdata["InstructorID"]

        public async Task<IActionResult> OnGetAsync(int id)
        {
            //get department
            Department = await _context.Departments
                .Include(d => d.Administrator) // eager loading
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.DepartmentID == id);

            //test if nothing was returned
            if (Department == null)
            {
                return NotFound();
            }

            //populate selectlist
            InstructorNameSL = new SelectList(_context.Instructors, "ID", "FirstMidName");

            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // fetch current department, ConcurrencyToken may have changed
            var departmentToUpdate = await _context.Departments
                .Include(d => d.Administrator)
                .FirstOrDefaultAsync(d => d.DepartmentID == id);

            if (departmentToUpdate == null)
            {
                return HandleDeletedDepartment();
            }

            // assigned a new guid to entity to save, corresponding to this edit
            departmentToUpdate.ConcurrencyToken = Guid.NewGuid();
            // sets the entity original token value to the one sent on post
            _context.Entry(departmentToUpdate).Property(d => d.ConcurrencyToken)
                                    .OriginalValue = Department.ConcurrencyToken;

            // entity update successful
            if (await TryUpdateModelAsync<Department>(
                departmentToUpdate,
                "Department",
                s => s.Name, s => s.StartDate, s => s.Budget, s => s.InstructorID
            ))
            {
                try
                {
                     await _context.SaveChangesAsync();
                     return RedirectToPage("./Index");
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    var exceptionEntry = ex.Entries.Single();
                    var clientValues = (Department)exceptionEntry.Entity;
                    var databaseEntry = exceptionEntry.GetDatabaseValues();
                    // department was deleted, display page with error
                    if (databaseEntry == null)
                    {
                        ModelState.AddModelError(string.Empty, "Unable to save" + 
                        " Department was deleted by another user");
                        return Page();
                    }
                    // if deparment exists
                    var dbValues = (Department)databaseEntry.ToObject();
                    await setDbErrorMessage(dbValues, clientValues, _context);

                    //save the current ConcurrencyToken, the next postback matches, unless new issues happen
                    Department.ConcurrencyToken = dbValues.ConcurrencyToken;

                    //clear model errors for the next postback
                    ModelState.Remove($"{nameof(Department)}.{nameof(Department.ConcurrencyToken)}");
                }
            }

            InstructorNameSL = new SelectList(_context.Instructors, "ID", "FullName", departmentToUpdate.InstructorID);

            return Page();
        }

        private async Task setDbErrorMessage(Department dbValues, Department clientValues, SchoolContext context)
        {
            if (dbValues.Name != clientValues.Name)
            {
                ModelState.AddModelError("Department.Name", $"Current value: {dbValues.Name}");
            }
            if (dbValues.Budget != clientValues.Budget)
            {
                ModelState.AddModelError("Department.Budget", $"Current value: {dbValues.Budget}");
            }
            if (dbValues.StartDate != clientValues.StartDate)
            {
                ModelState.AddModelError("Department.StartDate", $"Current value: {dbValues.StartDate}");
            }
            if (dbValues.InstructorID != clientValues.InstructorID)
            {
                Instructor dbInstructor = await _context.Instructors.FindAsync(dbValues.InstructorID);
                ModelState.AddModelError("Department.InstructorID", $"Current value: {dbInstructor?.FullName}");
            }

            ModelState.AddModelError(string.Empty, "record was modified by another user " 
                + "edit cancelled, displaying current values in db"  
                + "if you still want to edit, click Save");
        }

        private IActionResult HandleDeletedDepartment()
        {
            //var deletedDepartment = new Department();

            ModelState.AddModelError(string.Empty, "department was deleted by another user");
            InstructorNameSL = new SelectList(_context.Instructors, "ID", "FullName", Department.InstructorID);
            return Page();
        }

        private bool DepartmentExists(int id)
        {
            return _context.Departments.Any(e => e.DepartmentID == id);
        }
    }
}
