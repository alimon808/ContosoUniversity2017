using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ContosoUniversity.Data;
using ContosoUniversity.Data.Entities;
using ContosoUniversity.Data.Interfaces;
using System;
using ContosoUniversity.Web;

namespace ContosoUniversity.Controllers
{
    public class CoursesController : Controller
    {
        private readonly IRepository<Course> _courseRepo;
        private readonly IRepository<Department> _departmentRepo;
        private readonly IModelBindingHelperAdaptor _modelBindingHelperAdaptor;

        public CoursesController(IRepository<Course> courseRepo, IRepository<Department> departmentRepo, IModelBindingHelperAdaptor modelBindingHelperAdaptor)
        {
            _courseRepo = courseRepo;
            _departmentRepo = departmentRepo;
            _modelBindingHelperAdaptor = modelBindingHelperAdaptor;
        }

        public async Task<IActionResult> Index()
        {
            var courses = _courseRepo.GetAll().Include(c => c.Department);
            return View(await courses.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _courseRepo.GetAll()
                .Include(c => c.Department)
                .AsGatedNoTracking()
                .SingleOrDefaultAsync(m => m.ID == id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        public IActionResult Create()
        {
            PopulateDepartmentsDropDownList();
            return View();
        }

        private void PopulateDepartmentsDropDownList(object selectedDepartment = null)
        {
            var departmentsQuery = from d in _departmentRepo.GetAll()
                                   orderby d.Name
                                   select d;
            ViewBag.DepartmentID = new SelectList(departmentsQuery.AsNoTracking(), "DepartmentID", "Name", selectedDepartment);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CourseNumber,Credits,DepartmentID,Title")] Course course)
        {
            if (ModelState.IsValid)
            {
                await _courseRepo.AddAsync(course);
                await _courseRepo.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            PopulateDepartmentsDropDownList(course.DepartmentID);
            return View(course);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _courseRepo.GetAll().AsGatedNoTracking().SingleOrDefaultAsync(m => m.ID == id);
            if (course == null)
            {
                return NotFound();
            }

            PopulateDepartmentsDropDownList(course.DepartmentID);
            return View(course);
        }

        //todo: refactor - remove TryUpdateModelAsync to perform unit test
        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var courseToUpdate = await _courseRepo.GetAll().SingleOrDefaultAsync(c => c.ID == id);

            if (courseToUpdate == null)
            {
                return NotFound();
            }

            if (await _modelBindingHelperAdaptor.TryUpdateModelAsync<Course>(this, courseToUpdate, "", c => c.Credits, c => c.DepartmentID, c => c.Title))
            {
                try
                {
                    courseToUpdate.ModifiedDate = DateTime.UtcNow;
                    await _courseRepo.SaveChangesAsync();
                    return RedirectToAction("Index");
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Unable to save changes.  Try again, and if the problem persists, see your system administrator");
                }
            }

            PopulateDepartmentsDropDownList(courseToUpdate.DepartmentID);
            return View(courseToUpdate);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var course = await _courseRepo.GetAll()
                .Include(c => c.Department)
                .AsNoTracking()
                .SingleOrDefaultAsync(m => m.ID == id);
            if (course == null)
            {
                return NotFound();
            }

            return View(course);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _courseRepo.GetAll().SingleOrDefaultAsync(m => m.ID == id);
            _courseRepo.Delete(course);
            await _courseRepo.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> UpdateCourseCredits(int? multiplier)
        {
            if (multiplier != null)
            {
                ViewData["RowsAffected"] = await _courseRepo.ExecuteSqlCommandAsync($"UPDATE Contoso.Course SET Credits = Credits * {multiplier}");
            }
            return View();
        }

        private bool CourseExists(int id)
        {
            return _courseRepo.GetAll().Any(e => e.ID == id);
        }
    }
}
