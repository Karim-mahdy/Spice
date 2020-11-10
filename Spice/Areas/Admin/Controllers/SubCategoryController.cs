using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using Spice.Utility;

namespace Spice.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = (SD.ManagerUser))]
    public class SubCategoryController : Controller
    {
        [TempData]

        public string StatusMassage { get; set; }

        private readonly ApplicationDbContext _db;
        public SubCategoryController(ApplicationDbContext db)
        {
            _db = db;
        }
        public async Task<IActionResult> Index()
        {
            var subCategory = await _db.SubCategory.Include(x => x.Category).ToListAsync();
            return View(subCategory);
        }

        //Get Create 
        public async Task<IActionResult> Create()
        {
            SubCategoryAndCategoryViewModel model = new SubCategoryAndCategoryViewModel()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = new Models.SubCategory(),
                SubCategoryList = await _db.SubCategory.OrderBy(x => x.Name).Select(p => p.Name).Distinct().ToListAsync(),
            };
            return View(model);
        }

        //Post Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubCategoryAndCategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                var deesSubCategoryExists = _db.SubCategory.Include(p => p.Category).Where(p => p.Name == model.SubCategory.Name && p.Category.Id == model.SubCategory.CategoryId);
                if (deesSubCategoryExists.Count() > 0)
                {
                    StatusMassage = "Error : SubCategory Exists Under" + deesSubCategoryExists.First().Category.Name + "Category. please use another name. ";
                }
                else
                {
                    _db.SubCategory.Add(model.SubCategory);
                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            SubCategoryAndCategoryViewModel Vmodel = new SubCategoryAndCategoryViewModel()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = new Models.SubCategory(),
                SubCategoryList = await _db.SubCategory.OrderBy(x => x.Name).Select(p => p.Name).Distinct().ToListAsync(),
                StatusMassage = StatusMassage
            };
            return View(Vmodel);


        }
        [ActionName("GetSubCategory")]
        public async Task<IActionResult> GetSubCategory(int id)
        {
            List<SubCategory> subCategories = new List<SubCategory>();
            subCategories = await (from subCategory in _db.SubCategory
                                   where subCategory.CategoryId == id
                                   select subCategory).ToListAsync();
            return Json(new SelectList(subCategories, "Id", "Name"));
        }

        //Get Edit 
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                NotFound();
            }
            var subCategory = await _db.SubCategory.SingleOrDefaultAsync(m => m.Id == id);
            if (subCategory == null)
            {
                NotFound();
            }
            SubCategoryAndCategoryViewModel model = new SubCategoryAndCategoryViewModel()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = subCategory,
                SubCategoryList = await _db.SubCategory.OrderBy(x => x.Name).Select(p => p.Name).Distinct().ToListAsync(),
            };
            return View(model);
        }

        //Post Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SubCategoryAndCategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                var deesSubCategoryExists = _db.SubCategory.Include(p => p.Category).Where(p => p.Name == model.SubCategory.Name && p.Category.Id == model.SubCategory.CategoryId);
                if (deesSubCategoryExists.Count() > 0)
                {
                    StatusMassage = "Error : SubCategory Exists Under" + deesSubCategoryExists.First().Category.Name + "Category. please use another name. ";
                }
                else
                {
                    var subCatFormDb = await _db.SubCategory.FindAsync(id);
                    subCatFormDb.Name = model.SubCategory.Name;


                    await _db.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
            }
            SubCategoryAndCategoryViewModel Vmodel = new SubCategoryAndCategoryViewModel()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = new Models.SubCategory(),
                SubCategoryList = await _db.SubCategory.OrderBy(x => x.Name).Select(p => p.Name).Distinct().ToListAsync(),
                StatusMassage = StatusMassage
            };
            Vmodel.SubCategory.Id = id; // for fetch subcategory id
            return View(Vmodel);

        }

        //Get Details
        public async Task<IActionResult> Details(int id)
        {
            var subgategory = await _db.SubCategory.FindAsync(id);
            SubCategoryAndCategoryViewModel model = new SubCategoryAndCategoryViewModel()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = subgategory,

            };

            return View(model);

            //var subgategory = await _db.SubCategory.FindAsync(id);
            //SubCategoryAndCategoryViewModel model = new SubCategoryAndCategoryViewModel()
            //{
            //    SubCategory = subgategory,

            //};
            // var Category = _db.Category.Find(subgategory.CategoryId);
            // ViewBag.category = Category.Name;
            //return View (model);

        }

        //get delete
        public async Task<IActionResult> Delete(int id)
        {
            var subgategory = await _db.SubCategory.FindAsync(id);
            SubCategoryAndCategoryViewModel model = new SubCategoryAndCategoryViewModel()
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = subgategory,
            };

            return View(model);
        }

        // POST Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<IActionResult> DeletePost(int id)
        {
            var subgategory = await _db.SubCategory.FindAsync(id);
            _db.SubCategory.Remove(subgategory);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
