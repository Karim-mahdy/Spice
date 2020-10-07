using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Spice.Data;
using Spice.Models;

namespace Spice.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db;
        public CategoryController(ApplicationDbContext db)
        {
            _db = db;
        }
        //get
        public async Task<IActionResult> Index()
        {
            return View(await _db.Category.ToListAsync());
        }
        //GET For Create
        public IActionResult Create()
        {
            return View();
        }

        //Post Create 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                _db.Category.Add(category);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }


        //Get Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var item = await _db.Category.FindAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            return View(item);
        }


        //Post Edit
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                _db.Category.Update(category);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));

            }
            return View(category);
        }

        //GET DELETE
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var item = await _db.Category.FindAsync(id);

            if (item == null)
            {
                return NotFound();
            }
            return View(item);
        }

        // Post Delete
        [HttpPost , ActionName("Delete")]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> DeleteConfirmed(int id)
        {
                var item = await _db.Category.FindAsync(id);

                if (item == null)
                {
                    return NotFound();
                }
                _db.Category.Remove(item);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
        }

        // GET Details

        public async Task<IActionResult> Details (int id)
        {
                if (id == null)
                {
                    return NotFound();
                }
                var item = await _db.Category.FindAsync(id);
                if (item==null)
                {
                    return NotFound();
                }
                return View(item); 
        }

    }
}
