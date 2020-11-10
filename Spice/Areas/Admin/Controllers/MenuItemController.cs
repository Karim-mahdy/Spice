using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using Spice.Utility;

namespace Spice.Areas.Admin.Controllers
{
    [Authorize(Roles = (SD.ManagerUser))]
    [Area("Admin")]
    public class MenuItemController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _webHostEnvironment;
        [BindProperty]
        public MenuItemViewModel MenuItemVm { get; set; }
        public MenuItemController(ApplicationDbContext db, IWebHostEnvironment webHostEnvironment)
        {
            _db = db;
            _webHostEnvironment = webHostEnvironment;
            MenuItemVm = new MenuItemViewModel()
            {
                Category = _db.Category,
                SubCategory = _db.SubCategory,
                MenuItem = new Models.MenuItem()
            };
        }
        public async Task<IActionResult> Index()
        {
            //var menuitem =await _db.MenuItems.Include(m => m.Category).Include(m => m.SubCategory).ToListAsync();
            var menuitem = await _db.MenuItems.Include(m => m.SubCategory.Category).Include(m => m.SubCategory).ToListAsync();
            return View(menuitem);
        }

        //GET-CREATE
        public IActionResult Create()
        {
         
            ViewBag.ListofCategory = new SelectList(MenuItemVm.Category, "Id", "Name");
            return View(MenuItemVm);
        }

        //Post-Create

        [HttpPost]

        [ValidateAntiForgeryToken]
        [ActionName("Create")]
        public async Task<IActionResult> CreatePost(IFormCollection formValues)
        {
            //MenuItemVm.MenuItem.SubCategoryId = Convert.ToInt32(Request.Form["SubCategoryId"]);
            MenuItemVm.MenuItem.SubCategoryId = Convert.ToInt32(Request.Form["SubCategoryId"]);
            MenuItemVm.MenuItem.SubCategory = null;
            //var subcategory = _db.SubCategory.Find(MenuItemVm.MenuItem.SubCategoryId);
            //MenuItemVm.MenuItem.SubCategory = subcategory;
            _db.MenuItems.Add(MenuItemVm.MenuItem);
            await _db.SaveChangesAsync();

            //wor on save images

            string webRootPath = _webHostEnvironment.WebRootPath;
            var files = HttpContext.Request.Form.Files;
            var menuItemFromDb = await _db.MenuItems.FindAsync(MenuItemVm.MenuItem.Id);
            var temp = formValues["Description"];
            MenuItemVm.MenuItem.Descrition = temp;
            if (files.Count > 0)
            {
                var upload = Path.Combine(webRootPath, "Images");
                var extention = Path.GetExtension(files[0].FileName);
                using (var filestream = new FileStream(Path.Combine(upload, MenuItemVm.MenuItem.Id + extention), FileMode.Create))
                {
                    files[0].CopyTo(filestream);

                }
                menuItemFromDb.Image = @"\Images\" + MenuItemVm.MenuItem.Id + extention;

                //files has been uploaded
            }
            else
            {
                var uploads = Path.Combine(webRootPath, @"Images\" + SD.DefaultFoodImage);
                System.IO.File.Copy(uploads, webRootPath + @"\Images\" + MenuItemVm.MenuItem.Id + ".png");
                menuItemFromDb.Image = @"\Images\" + MenuItemVm.MenuItem.Id + ".png";
                //no files was uploaded,so use default
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        //GET-Edit
        public async Task<IActionResult> Edit(int id)
        {
            MenuItemVm.MenuItem = await _db.MenuItems.Include(x => x.SubCategory).Include(x => x.SubCategory.Category).SingleOrDefaultAsync(X => X.Id == id);
            //var menuItem = await _db.MenuItems.FindAsync(id);
            //MenuItemVm.MenuItem = menuItem;

            //// pass data of subcategory to vm.menuitem.subcategory = null
            //var subcategory = _db.SubCategory.Find(MenuItemVm.MenuItem.SubCategoryId);
            //MenuItemVm.MenuItem.SubCategory = subcategory;
            //ViewBag.ListofCategory = new SelectList(MenuItemVm.Category, "Id", "Name");
            return View(MenuItemVm);
        }

        //Post-Edit
        [HttpPost]

        [ValidateAntiForgeryToken]
        [ActionName("Edit")]
        public async Task<IActionResult> EditPost(int? id, IFormCollection formValues)
        {
            try
            {
                if (id == null)
                {
                    return NotFound();
                }
                var temp = formValues["Description"];

                // MenuItemVm.MenuItem.SubCategoryId = Convert.ToInt32(Request.Form["SubCategoryId"].ToString());
                var menuItemFromDb = await _db.MenuItems.Include(x => x.SubCategory).Include(x => x.SubCategory.Category).SingleOrDefaultAsync(X => X.Id == id);
                //var subcategory = _db.SubCategory.Find(MenuItemVm.MenuItem.SubCategoryId);
                //MenuItemVm.MenuItem.SubCategory = subcategory;

                //work on save images

                string webRootPath = _webHostEnvironment.WebRootPath;
                var files = HttpContext.Request.Form.Files;

                if (files.Count > 0)
                {
                    //new image has been uploaded
                    var upload = Path.Combine(webRootPath, "Images");
                    var extention_new = Path.GetExtension(files[0].FileName);


                    // delete the orginal image
                    var imagePath = Path.Combine(webRootPath, menuItemFromDb.Image.TrimStart('\\'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                    using (var filestream = new FileStream(Path.Combine(upload, menuItemFromDb.Id+ extention_new), FileMode.Create))
                    {
                        files[0].CopyTo(filestream);

                    }
                    menuItemFromDb.Image = @"\Images\" + menuItemFromDb.Id + extention_new;

                    //files has been uploaded
                }

                menuItemFromDb.Name = MenuItemVm.MenuItem.Name;
                menuItemFromDb.Descrition = temp;
                menuItemFromDb.Price = MenuItemVm.MenuItem.Price;
                menuItemFromDb.SubCategory.CategoryId = MenuItemVm.MenuItem.SubCategory.CategoryId;
                menuItemFromDb.SubCategoryId = MenuItemVm.MenuItem.SubCategoryId;
                menuItemFromDb.Spicyness = MenuItemVm.MenuItem.Spicyness;


                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {

                throw;
            }
        }

        // Detials
        public async Task<IActionResult> Details(int id)
        {
            MenuItemVm.MenuItem = await _db.MenuItems.Include(x => x.SubCategory).Include(x => x.SubCategory.Category).SingleOrDefaultAsync(X => X.Id == id);
            return View(MenuItemVm);
        }

        //Delete Get
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            //find item 
            MenuItemVm.MenuItem = await _db.MenuItems.Include(x => x.SubCategory).Include(x => x.SubCategory.Category).SingleOrDefaultAsync(X => X.Id == id);
            return View(MenuItemVm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        //Delete-Post
        public async Task<IActionResult> DeletePost(int id)
        {

            MenuItemVm.MenuItem = await _db.MenuItems.Include(x => x.SubCategory).Include(x => x.SubCategory.Category).SingleOrDefaultAsync(X => X.Id == id);

            // delete the orginal image
            string webRootPath = _webHostEnvironment.WebRootPath;

            var imagePath = Path.Combine(webRootPath, MenuItemVm.MenuItem.Image.TrimStart('\\'));
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }
            _db.MenuItems.Remove(MenuItemVm.MenuItem);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

    }
}
