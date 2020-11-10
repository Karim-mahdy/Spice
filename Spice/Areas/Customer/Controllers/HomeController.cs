using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;

namespace Spice.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var claimIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);
            IndexViewModel IndexVM = new IndexViewModel();
            IndexVM.MenuItems = await _db.MenuItems.Include(x => x.SubCategory).Include(x => x.SubCategory.Category).ToListAsync();
            IndexVM.Category = await _db.Category.ToListAsync();
            IndexVM.Coupon = await _db.Coupon.ToListAsync();
            if (claim != null)
            {
                var cnt = _db.ShoppingCart.Where(x => x.ApplicationUserId == claim.Value).ToList().Count;
                HttpContext.Session.SetInt32("ssCartCount", cnt);
            }
            return View(IndexVM);
          

        }

        [Authorize]
        public async Task<IActionResult> Detials(int id)
        {
            var menuItemFromDb = await _db.MenuItems.Include(u => u.SubCategory).Include(u => u.SubCategory.Category).FirstOrDefaultAsync(x=>x.Id==id);
            ShoppingCart cartobj = new ShoppingCart()
            {
                //MenuItemId=menuItemFromDb.Id,
                MenuItem = menuItemFromDb
            };
            return View(cartobj);
        }

        //Post
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Detials(ShoppingCart shoppingCart)
        {
            shoppingCart.Id = 0;
            var claimIdentity = (ClaimsIdentity)this.User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);
            shoppingCart.ApplicationUserId = claim.Value;
            shoppingCart.MenuItemId = shoppingCart.MenuItem.Id;

            var cartObjFromDb = await _db.ShoppingCart.Where(x => x.ApplicationUserId == shoppingCart.ApplicationUserId && x.MenuItemId == shoppingCart.MenuItemId).FirstOrDefaultAsync();
            if (cartObjFromDb==null)
            {
                _db.ShoppingCart.Add(shoppingCart);
            }
            else
            {
                cartObjFromDb.Count += shoppingCart.Count;
                
            }       
           
            await _db.SaveChangesAsync();
            var count = _db.ShoppingCart.Where(x => x.ApplicationUserId == shoppingCart.ApplicationUserId).ToList().Count();
            HttpContext.Session.SetInt32("ssCartCount", count);
            return RedirectToAction(nameof(Index));
        }



















        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
