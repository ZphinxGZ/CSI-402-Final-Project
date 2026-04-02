using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CSI_402_Final_Project.Models;
using CSI_402_Final_Project.Security;
using CSI_402_Final_Project.ViewModel;

namespace CSI_402_Final_Project.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly Projectcsi402dbContext _db;

    public ProfileController(Projectcsi402dbContext db)
    {
        _db = db;
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdClaim, out var userId))
            return userId;
        return null;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var user = _db.Users
            .Include(u => u.Shippingaddresses)
            .FirstOrDefault(u => u.Id == userId);

        if (user == null)
            return NotFound();

        return View(user);
    }

    [HttpGet]
    public IActionResult Edit()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var user = _db.Users.Find(userId);
        if (user == null)
            return NotFound();

        var vm = new ProfileEditViewModel
        {
            Name = user.Name ?? "",
            Lastname = user.Lastname ?? "",
            Email = user.Email ?? "",
            PhoneNumber = user.PhoneNumber
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(ProfileEditViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var user = _db.Users.Find(userId);
        if (user == null)
            return NotFound();

        user.Name = vm.Name;
        user.Lastname = vm.Lastname;
        user.PhoneNumber = vm.PhoneNumber;

        _db.SaveChanges();

        TempData["SwalIcon"] = "success";
        TempData["SwalTitle"] = "Profile Updated";
        TempData["SwalMessage"] = "Your profile has been updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ChangePassword(ChangePasswordViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var user = _db.Users.Find(userId);
        if (user == null)
            return NotFound();

        if (!PasswordHasher.Verify(vm.CurrentPassword, user.Password))
        {
            ModelState.AddModelError(nameof(vm.CurrentPassword), "Current password is incorrect.");
            return View(vm);
        }

        user.Password = PasswordHasher.Hash(vm.NewPassword);
        _db.SaveChanges();

        TempData["SwalIcon"] = "success";
        TempData["SwalTitle"] = "Password Changed";
        TempData["SwalMessage"] = "Your password has been changed successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Addresses()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var addresses = _db.Shippingaddresses
            .Where(a => a.UserId == userId)
            .ToList();

        return View(addresses);
    }

    [HttpGet]
    public IActionResult AddAddress()
    {
        return View(new ShippingAddressEditViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddAddress(ShippingAddressEditViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var address = new Shippingaddress
        {
            UserId = userId,
            Address = vm.Address,
            City = vm.City,
            PostalCode = vm.PostalCode
        };

        _db.Shippingaddresses.Add(address);
        _db.SaveChanges();

        TempData["SwalIcon"] = "success";
        TempData["SwalTitle"] = "Address Added";
        TempData["SwalMessage"] = "Shipping address has been added.";

        return RedirectToAction(nameof(Addresses));
    }

    [HttpGet]
    public IActionResult EditAddress(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var address = _db.Shippingaddresses
            .FirstOrDefault(a => a.Id == id && a.UserId == userId);

        if (address == null)
            return NotFound();

        var vm = new ShippingAddressEditViewModel
        {
            Id = address.Id,
            Address = address.Address ?? "",
            City = address.City,
            PostalCode = address.PostalCode
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditAddress(int id, ShippingAddressEditViewModel vm)
    {
        if (id != vm.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(vm);

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var address = _db.Shippingaddresses
            .FirstOrDefault(a => a.Id == id && a.UserId == userId);

        if (address == null)
            return NotFound();

        address.Address = vm.Address;
        address.City = vm.City;
        address.PostalCode = vm.PostalCode;

        _db.SaveChanges();

        TempData["SwalIcon"] = "success";
        TempData["SwalTitle"] = "Address Updated";
        TempData["SwalMessage"] = "Shipping address has been updated.";

        return RedirectToAction(nameof(Addresses));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteAddress(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var address = _db.Shippingaddresses
            .FirstOrDefault(a => a.Id == id && a.UserId == userId);

        if (address == null)
            return NotFound();

        _db.Shippingaddresses.Remove(address);
        _db.SaveChanges();

        TempData["SwalIcon"] = "success";
        TempData["SwalTitle"] = "Address Deleted";
        TempData["SwalMessage"] = "Shipping address has been deleted.";

        return RedirectToAction(nameof(Addresses));
    }
}
