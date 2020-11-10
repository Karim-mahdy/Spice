using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Spice.Data;
using Spice.Models;
using Spice.Models.ViewModels;
using Spice.Utility;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Spice.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        [BindProperty]
        public OrderDetailsCart detailsCart { get; set; }
        public readonly IEmailSender EmailSender;
        
        public CartController(ApplicationDbContext db , IEmailSender emailSender )

        {
            EmailSender = emailSender;
            _db = db;
        }
        public async Task< IActionResult> Index()
        {

            detailsCart = new OrderDetailsCart()
            {
                OrderHeader = new Models.OrderHeader()
            };
            detailsCart.OrderHeader.OrderTotal = 0;

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var cart =  _db.ShoppingCart.Where(x => x.ApplicationUserId == claim.Value);

            if (cart!=null)
            {
                detailsCart.ListCart = cart.ToList();
            }
            foreach (var list in detailsCart.ListCart)
            {
                list.MenuItem = await _db.MenuItems.FirstOrDefaultAsync(m=>m.Id==list.MenuItemId);
                detailsCart.OrderHeader.OrderTotal = detailsCart.OrderHeader.OrderTotal + (list.MenuItem.Price * list.Count);
                
            }
            detailsCart.OrderHeader.OrderTotalOriginal = detailsCart.OrderHeader.OrderTotal;
            if (HttpContext.Session.GetString("ssCouponCode") != null)
            {
                detailsCart.OrderHeader.CouponCode = HttpContext.Session.GetString("ssCouponCode");
                var couponFromDb = await _db.Coupon.Where(x => x.Name == detailsCart.OrderHeader.CouponCode).FirstOrDefaultAsync();
                detailsCart.OrderHeader.OrderTotal = Discount(couponFromDb, detailsCart.OrderHeader.OrderTotalOriginal);
            }
           
            return View(detailsCart);
        }

        //Summary

        public async Task<IActionResult> Summary()
        {

            detailsCart = new OrderDetailsCart()
            {
                OrderHeader = new Models.OrderHeader()
            };
            detailsCart.OrderHeader.OrderTotal = 0;

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var cart = _db.ShoppingCart.Where(x => x.ApplicationUserId == claim.Value);
            ApplicationUser applicationUser = await _db.ApplicationUser.Where(x => x.Id == claim.Value).FirstOrDefaultAsync();
            if (cart != null)
            {
                detailsCart.ListCart = cart.ToList();
            }
            foreach (var list in detailsCart.ListCart)
            {
                list.MenuItem = await _db.MenuItems.FirstOrDefaultAsync(m => m.Id == list.MenuItemId);
                detailsCart.OrderHeader.OrderTotal = detailsCart.OrderHeader.OrderTotal + (list.MenuItem.Price * list.Count);

            }
            detailsCart.OrderHeader.OrderTotalOriginal = detailsCart.OrderHeader.OrderTotal;
            detailsCart.OrderHeader.PickupName = applicationUser.Name;
            detailsCart.OrderHeader.PickupNumber = applicationUser.PhoneNumber;
            detailsCart.OrderHeader.PickupDate = DateTime.Now;
            if (HttpContext.Session.GetString("ssCouponCode") != null)
            {
                detailsCart.OrderHeader.CouponCode = HttpContext.Session.GetString("ssCouponCode");
                var couponFromDb = await _db.Coupon.Where(x => x.Name == detailsCart.OrderHeader.CouponCode).FirstOrDefaultAsync();
                detailsCart.OrderHeader.OrderTotal = Discount(couponFromDb, detailsCart.OrderHeader.OrderTotalOriginal);
            }

            return View(detailsCart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Summary")]
        public async Task<IActionResult> SummaryPost(string stripeToken)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            detailsCart.ListCart = await _db.ShoppingCart.Where(c => c.ApplicationUserId == claim.Value).ToListAsync();
            detailsCart.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            detailsCart.OrderHeader.OrderDate = DateTime.Now;
            detailsCart.OrderHeader.UserId = claim.Value;
            detailsCart.OrderHeader.Status = SD.PaymentStatusPending;
            detailsCart.OrderHeader.PickupTime = Convert.ToDateTime(detailsCart.OrderHeader.PickupDate.ToShortDateString() + " " + detailsCart.OrderHeader.PickupTime.ToShortTimeString());

            List<OrderDetails> orderDetailsList = new List<OrderDetails>();
            _db.OrderHeader.Add(detailsCart.OrderHeader);
            await _db.SaveChangesAsync();
            detailsCart.OrderHeader.OrderTotalOriginal = 0;

            foreach (var item in detailsCart.ListCart)
            {
                item.MenuItem = await _db.MenuItems.FirstOrDefaultAsync(m => m.Id == item.MenuItemId);
                OrderDetails orderDetails = new OrderDetails
                {
                    MenuItemId = item.MenuItemId,
                    OrderId = detailsCart.OrderHeader.Id,
                    Description = item.MenuItem.Descrition,
                    Name = item.MenuItem.Name,
                    Price = item.MenuItem.Price,
                    Count = item.Count
                };
                detailsCart.OrderHeader.OrderTotalOriginal += orderDetails.Count * orderDetails.Price;
                _db.OrderDetails.Add(orderDetails);
            }

            if (HttpContext.Session.GetString("ssCouponCode") != null)
            {
                detailsCart.OrderHeader.CouponCode = HttpContext.Session.GetString("ssCouponCode");
                var couponFromDb = await _db.Coupon.Where(x => x.Name == detailsCart.OrderHeader.CouponCode).FirstOrDefaultAsync();
                detailsCart.OrderHeader.OrderTotal = Discount(couponFromDb, detailsCart.OrderHeader.OrderTotalOriginal);
            }
            else
            {
                detailsCart.OrderHeader.OrderTotal = detailsCart.OrderHeader.OrderTotalOriginal;
            }
            detailsCart.OrderHeader.CouponCodeDiscount = detailsCart.OrderHeader.OrderTotalOriginal - detailsCart.OrderHeader.OrderTotal;
            await _db.SaveChangesAsync();
            _db.ShoppingCart.RemoveRange(detailsCart.ListCart);
            HttpContext.Session.SetInt32(SD.ssShoppingCartCount, 0);
            await _db.SaveChangesAsync();

            var options = new ChargeCreateOptions
            {
                Amount = Convert.ToInt32(detailsCart.OrderHeader.OrderTotal * 100),
                Currency = "usd",
                Description = "Order ID :" + detailsCart.OrderHeader.Id,
                Source = stripeToken
              
            };
            var service = new ChargeService();
            Charge charge = service.Create(options);
            if (charge.BalanceTransactionId == null)
            {
                detailsCart.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
            }
            else
            {
                detailsCart.OrderHeader.TransactionId = charge.BalanceTransactionId;

            }
            if (charge.Status.ToLower() == "succeeded")
            {
                //send email to customer
                await EmailSender.SendEmailAsync(_db.Users.Where(x => x.Id == claim.Value).FirstOrDefault().Email, "Spice - order Created " + detailsCart.OrderHeader.Id.ToString(), "order has been submitted successfuly");
                detailsCart.OrderHeader.PaymentStatus = SD.PaymentStatusApproved;
                detailsCart.OrderHeader.Status = SD.StatusSubmitted;


            }
            else
            {
                detailsCart.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
            }
            await _db.SaveChangesAsync();
            //return RedirectToAction("Index", "Home");
            return RedirectToAction("Confirm","Order",new { id = detailsCart.OrderHeader.Id });
        }
        public IActionResult AddCoupon()
        {
            if (detailsCart.OrderHeader.CouponCode==null)
            {
                detailsCart.OrderHeader.CouponCode = "";
            }
            HttpContext.Session.SetString("ssCouponCode", detailsCart.OrderHeader.CouponCode);            
            return RedirectToAction(nameof(Index));
        }

        public IActionResult RemoveCoupon()
        {
          HttpContext.Session.SetString("ssCouponCode", string.Empty);
          return RedirectToAction(nameof(Index));
        }


        // Plus item Count
        public async Task<IActionResult> Plus(int id)
        {
            var cart = await _db.ShoppingCart.FirstOrDefaultAsync(x => x.Id == id);
            cart.Count += 1;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Minus item Count
        public async Task<IActionResult> Minus(int id)
        {
            var cart = await _db.ShoppingCart.FirstOrDefaultAsync(x => x.Id == id);
            if (cart.Count==1)
            {
                _db.ShoppingCart.Remove(cart);
                await _db.SaveChangesAsync();

                var cnt =  _db.ShoppingCart.Where(x => x.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
                HttpContext.Session.SetInt32("ssCartCount", cnt);
            }
            else
            {
                cart.Count -= 1;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Remove item 
        public async Task<IActionResult> Remove(int id)
        {
            var cart = await _db.ShoppingCart.FirstOrDefaultAsync(x => x.Id == id);
           
                _db.ShoppingCart.Remove(cart);
                await _db.SaveChangesAsync();

                var cnt = _db.ShoppingCart.Where(x => x.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
                HttpContext.Session.SetInt32("ssCartCount", cnt);

            return RedirectToAction(nameof(Index));
        }


        //Fun GET DisCount
        public static double Discount (Models.Coupon couponFromDb , double OrginalOrderTota)
        {
            if (couponFromDb==null)
            {
                return OrginalOrderTota;
            }
            if (couponFromDb.MinimumAmount>OrginalOrderTota)
            {
                return OrginalOrderTota;
            }
            else
            {
                if (Convert.ToInt32(couponFromDb.CouponType)==(int)Models.Coupon.ECouponType.Doller)
                {
                    return Math.Round(OrginalOrderTota - couponFromDb.Discount, 2);
                }
                if (Convert.ToInt32(couponFromDb.CouponType) == (int)Models.Coupon.ECouponType.Present)
                {
                    return Math.Round(OrginalOrderTota - OrginalOrderTota* couponFromDb.Discount/100, 2);
                }
            }
            return OrginalOrderTota;
        }
    }
}
