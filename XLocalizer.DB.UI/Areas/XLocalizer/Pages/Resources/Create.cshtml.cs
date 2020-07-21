using System.Threading.Tasks;
using XLocalizer.DB.Models;
using LazZiya.TagHelpers.Alerts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using XLocalizer.DB.UI.Areas.XLocalizer.Models;

namespace XLocalizer.DB.UI.Areas.XLocalizer.Pages.Resources
{
    public class CreateModel : PageModel
    {
        private readonly IDbResourceManager _resManager;
        private readonly IDbCultureManager _cMan;

        public CreateModel(IDbResourceManager manager, IDbCultureManager cman)
        {
            _resManager = manager;
            _cMan = cman;
        }

        [BindProperty]
        public ResourceInputModel InputModel { get; set; }

        public IEnumerable<SelectListItem> Cultures { get; set; }

        public async Task OnGet()
        {
            await GetCulturesListAsync();
        }

        private async Task GetCulturesListAsync()
        {
            (var cultures, var _) = await _cMan.CulturesListAsync(1, 848, null);
            Cultures = cultures.Select(x => new SelectListItem
            {
                Text = x.EnglishName,
                Value = x.ID,
            });
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await GetCulturesListAsync();
                return Page();
            }

            var entity = await _resManager.GetResourceAsync<XDbResource>(x => x.Key == InputModel.Key && x.CultureID == InputModel.CultureID);
            if (entity != null)
            {
                TempData.Warning("Resource already exists!");
                await GetCulturesListAsync();
                return Page();
            }

            entity = new XDbResource
            {
                Key = InputModel.Key,
                Value = InputModel.Value,
                CultureID = InputModel.CultureID,
                Comment = InputModel.Comment,
                IsActive = InputModel.IsActive
            };

            if (await _resManager.AddResourceAsync<XDbResource>(entity))
                TempData.Success("Resource added.");
            else
                TempData.Danger("Resource not added");

            return RedirectToPage("Index");
        }
    }
}
