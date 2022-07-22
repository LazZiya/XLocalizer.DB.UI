using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using XLocalizer.DB.Models;
using XLocalizer.DB.UI.Areas.XLocalizerCommon.Models;
using LazZiya.TagHelpers.Alerts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace XLocalizer.DB.UI.Areas.XLocalizerBS5.Pages.Resources
{
    public class IndexModel : PageModel
    {
        private readonly IDbResourceManager _resManager;
        private readonly IDbCultureManager _culManager;

        public IndexModel(IDbResourceManager manager, IDbCultureManager cManger)
        {
            _resManager = manager;
            _culManager = cManger;
        }

        [BindProperty(SupportsGet = true)]
        public int P { get; set; } = 1;
        [BindProperty(SupportsGet = true)]
        public int S { get; set; } = 10;
        public int TotalRecords { get; set; } = 0;

        /// <summary>
        /// use for resource search 
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public ResourceSearchModel SearchModel { get; set; }

        public IEnumerable<XDbResource> Resources { get; set; }
        public IEnumerable<SelectListItem> Cultures { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            (Resources, TotalRecords) = await ListResourcesAsync();
            await GetCulturesListAsync();

            return Page();
        }


        private async Task GetCulturesListAsync()
        {
            (var cultures, var _) = await _culManager.CulturesListAsync(1, 848, null);
            Cultures = cultures.Select(x => new SelectListItem
            {
                Text = x.EnglishName,
                Value = x.ID,
            });
        }

        private async Task<(IEnumerable<XDbResource> resources, int totalRecords)> ListResourcesAsync()
        {
            var searchExpressions = new List<Expression<Func<XDbResource, bool>>> { };

            if (!string.IsNullOrWhiteSpace(SearchModel.Query))
            {
                searchExpressions.Add(x => x.Key != null && x.Key.Contains(SearchModel.Query)
                || x.Comment != null && x.Comment.Contains(SearchModel.Query)
                || x.Value != null && x.Value.Contains(SearchModel.Query));
            }

            if (SearchModel.ID != null && SearchModel.ID.Value > 0)
            {
                searchExpressions.Add(x => x.ID == SearchModel.ID);
            }

            if (!string.IsNullOrEmpty(SearchModel.Culture))
            {
                searchExpressions.Add(x => x.CultureID == SearchModel.Culture);
            }

            return await _resManager.ResourcesSetListAsync<XDbResource>(P, S, searchExpressions);
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            if (id == 0)
            {
                TempData.Danger("Resource ID can't be empty");
                return RedirectToPage("Index");
            }

            var entity = await _resManager.GetResourceAsync<XDbResource>(x => x.ID == id);
            if (entity == null)
            {
                TempData.Danger("Resource not found!");
                return RedirectToPage("Index");
            }

            if (await _resManager.DeleteResourceAsync<XDbResource>(x => x.Key == entity.Key))
                TempData.Success("Deleted!");
            else
                TempData.Danger("Unknown error occord!");

            return RedirectToPage("Index");
        }
    }
}
