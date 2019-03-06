using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using MemberService.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace MemberService.Areas.Identity.Pages.Account.Manage
{
    public partial class IndexModel : PageModel
    {
        private readonly UserManager<MemberUser> _userManager;
        private readonly MemberContext _memberContext;

        public IndexModel(
            UserManager<MemberUser> userManager,
            MemberContext memberContext)
        {
            _userManager = userManager;
            _memberContext = memberContext;
        }

        [Display(Name = "E-post")]
        public string Email { get; set; }

        public IReadOnlyCollection<Payment> Payments { get; private set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Display(Name = "Fullt navn")]
            public string FullName { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);

            var user = await _memberContext.Users
                .Include(x => x.Payments)
                .SingleUser(userId);

            if (user == null)
            {
                return base.NotFound($"Unable to load user with ID '{userId}'.");
            }

            Email = user.Email;

            Payments = user.Payments
                .OrderByDescending(p => p.PayedAt)
                .ToList();

            Input = new InputModel
            {
                FullName = user.FullName
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (Input.FullName != user.FullName)
            {
                user.FullName = Input.FullName;
            }

            await _userManager.UpdateAsync(user);

            StatusMessage = "Navnet ditt har blitt lagret :)";
            return RedirectToPage();
        }
    }
}
