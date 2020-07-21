using System.Threading.Tasks;
using XLocalizer.DB.Models;
using XLocalizer.DB.UI.Areas.XLocalizer.Models;
using LazZiya.TagHelpers.Alerts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace XLocalizer.DB.UI.Areas.XLocalizer.Pages.Resources
{
    public class EditModel : PageModel
    {
        private readonly IDbResourceManager _resManager;

        public EditModel(IDbResourceManager manager)
        {
            _resManager = manager;
        }

        [BindProperty]
        public ResourceEditModel InputModel { get; set; }

        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; }

        [BindProperty(SupportsGet = true)]
        public int ID { get; set; }
        public async Task<IActionResult> OnGetAsync()
        {
            var entity = await _resManager.GetResourceAsync<XDbResource>(x => x.ID == ID);

            if (entity == null)
            {
                TempData.Warning("Resource not found!");
                return RedirectToPage("Index");
            }

            InputModel = new ResourceEditModel
            {
                Key = entity.Key
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var updated = await _resManager.BulkUpdateAsync<XDbResource>(InputModel.Key, InputModel.NewKey);

            if (updated > 0)
                TempData.Success($"Resources updated. Total localized instances: '{updated}'");
            else
                TempData.Danger("Resource not updated");

            return RedirectToPage("Index");
        }
    }
}
