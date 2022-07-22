using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using XLocalizer.DB.Models;
using XLocalizer.DB.UI.Areas.XLocalizerCommon.Models;
using LazZiya.TagHelpers.Alerts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace XLocalizer.DB.UI.Areas.XLocalizerBS5.Pages.Cultures
{
    [ValidateAntiForgeryToken]
    public class IndexModel : PageModel
    {
        private readonly IDbCultureManager _culManager;
        private readonly IDbResourceExporter _exporter;
        private readonly IApplicationLifetime _applicationLifetime;
        private readonly ILogger _logger;

        public IndexModel(IDbCultureManager manager, IDbResourceExporter exporter, IApplicationLifetime lifetime, ILogger<IndexModel> logger)
        {
            _culManager = manager;
            _exporter = exporter;
            _applicationLifetime = lifetime;
            _logger = logger;
        }

        public IActionResult OnPostRestartApp()
        {
            _applicationLifetime.StopApplication();
            return new EmptyResult();
        }

        public IEnumerable<XDbCulture> SupportedCultures { get; set; }

        // Page number
        [BindProperty(SupportsGet = true)]
        public int P { get; set; } = 1;

        // Page size
        [BindProperty(SupportsGet = true)]
        public int S { get; set; } = 10;

        public int TotalRecords { get; set; } = 0;

        // Search keywords
        [BindProperty(SupportsGet = true)]
        public string Q { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public bool? Def { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? Act { get; set; }

        public ICollection<CultureItemModel> SystemCultures { get; set; }

        // total number of items exported to resource file
        private int GrossTotalNewItems { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            (SupportedCultures, TotalRecords) = await ListSupportedCulturesAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAddNewAsync(string ID)
        {
            if (string.IsNullOrWhiteSpace(ID))
            {
                TempData.Danger("Culture name can't be empty");
                return LocalRedirect(Url.Page("Index", new { area = "XLocalizerBS5" }));
            }

            var culture = new XDbCulture
            {
                ID = ID,
                EnglishName = CultureInfo.GetCultureInfo(ID).EnglishName,
                IsDefault = false,
                IsActive = false
            };

            if (await _culManager.GetCultureAsync(ID) != null)
                TempData.Warning("Culture already exists!");

            else if (await _culManager.AddCultureAsync(culture))
                TempData.Success("New culture added");
            else
                TempData.Danger("Culture not added!");

            return LocalRedirect(Url.Page("Index", new { area = "XLocalizerBS5" }));
        }

        public async Task<IActionResult> OnPostDeleteAsync(string ID)
        {
            if (string.IsNullOrWhiteSpace(ID))
            {
                TempData.Danger("Culture name can't be empty");
                return LocalRedirect(Url.Page("Index", new { area = "XLocalizerBS5" }));
            }

            var entity = await _culManager.GetCultureAsync(ID);
            if (entity == null)
            {
                TempData.Danger("Culture not found!");
                return LocalRedirect(Url.Page("Index", new { area = "XLocalizerBS5" }));
            }

            if (entity.IsDefault)
            {
                TempData.Warning("Default culture can't be deleted!");
                return LocalRedirect(Url.Page("Index", new { area = "XLocalizerBS5" }));
            }

            if (await _culManager.DeleteCultureAsync(entity.ID))
                TempData.Success("Deleted!");
            else
                TempData.Danger("Unknown error occord!");

            return LocalRedirect(Url.Page("Index", new { area = "XLocalizerBS5" }));
        }

        public async Task<IActionResult> OnPostActivateAsync(string ID)
        {
            if (string.IsNullOrWhiteSpace(ID))
            {
                TempData.Danger("Culture name can't be empty");
                return LocalRedirect(Url.Page("Index", new { area = "XLocalizerBS5" }));
            }

            var entity = await _culManager.GetCultureAsync(ID);
            if (entity == null)
            {
                TempData.Danger("Culture not found!");
                return LocalRedirect(Url.Page("Index", new { area = "XLocalizerBS5" }));
            }

            if (entity.IsDefault && entity.IsActive)
            {
                TempData.Warning("Default culture can't be disabled!");
                return LocalRedirect(Url.Page("Index", new { area = "XLocalizerBS5" }));
            }

            entity.IsActive = !entity.IsActive;

            if (await _culManager.UpdateCultureAsync(entity))
            {
                if (entity.IsActive)
                    TempData.Success("Culture enabled!");
                else
                    TempData.Success("Culture disabled!");
            }
            else
                TempData.Danger("Unknown error occord!");

            return LocalRedirect(Url.Page("Index", new { area = "XLocalizerBS5" }));
        }

        public async Task<IActionResult> OnPostSetDefaultAsync(string ID)
        {
            if (string.IsNullOrWhiteSpace(ID))
            {
                TempData.Danger("Culture name can't be empty");
                return LocalRedirect(Url.Page("Index", new { area = "XLocalizerBS5" }));
            }

            var entity = await _culManager.GetCultureAsync(ID);
            if (entity == null)
            {
                TempData.Danger("Culture not found!");
                return LocalRedirect(Url.Page("Index", new { area = "XLocalizerBS5" }));
            }

            if (await _culManager.SetAsDefaultAsync(entity.ID))
                TempData.Success("Culture enabled and set as default!");
            else
                TempData.Danger("Unknown error occord!");

            return LocalRedirect(Url.Page("Index", new { area = "XLocalizerBS5" }));
        }

        public async Task<(IEnumerable<XDbCulture> items, int total)> ListSupportedCulturesAsync()
        {
            var expList = new List<Expression<Func<XDbCulture, bool>>> { };
            if (!string.IsNullOrWhiteSpace(Q))
            {
                expList.Add(x => x.ID != null && x.ID.Contains(Q) || x.EnglishName != null && x.EnglishName.Contains(Q));
            }

            if (Def != null)
            {
                expList.Add(x => x.IsDefault == Def);
            }

            if (Act != null)
            {
                expList.Add(x => x.IsActive == Act);
            }

            return await _culManager.CulturesListAsync(P, S, expList);
        }

        /// <summary>
        /// Return a list of cultures
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public JsonResult OnGetSystemCultures(string search)
        {
            var keyWords = search.Split(' ');
#if NETCOREAPP2_0
            SystemCultures = CultureInfo.GetCultures(CultureTypes.AllCultures)
                .Where(x => keyWords.All(kw => x.Name.StartsWith(kw, StringComparison.OrdinalIgnoreCase)
                        || x.EnglishName.StartsWith(kw, StringComparison.OrdinalIgnoreCase)
                        || x.NativeName.StartsWith(kw, StringComparison.OrdinalIgnoreCase)
                        || x.DisplayName.StartsWith(kw, StringComparison.OrdinalIgnoreCase)))
                .Select(x => new CultureItemModel { ID = x.Name, Text = x.EnglishName }).ToList();
#else
            SystemCultures = CultureInfo.GetCultures(CultureTypes.AllCultures)
                            .Where(x => keyWords.All(kw => x.Name.StartsWith(kw, StringComparison.OrdinalIgnoreCase)
                                    || x.EnglishName.Contains(kw, StringComparison.OrdinalIgnoreCase)
                                    || x.NativeName.Contains(kw, StringComparison.OrdinalIgnoreCase)
                                    || x.DisplayName.Contains(kw, StringComparison.OrdinalIgnoreCase)))
                            .Select(x => new CultureItemModel { ID = x.Name, Text = x.EnglishName }).ToList();
#endif
            return new JsonResult(SystemCultures);
        }

        /// <summary>
        /// Generate resx file for the selected culture
        /// </summary>
        /// <param name="id">Culture id (two letter name)</param>
        /// <param name="approvedOnly">export all or only approved items</param>
        /// <param name="overwrite">overwrite existing items</param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostGenerateResxFile(string id, bool approvedOnly, bool overwrite)
        {
            var totalExported = await _exporter.ExportToResx<XDbResource>(id, approvedOnly, overwrite);

            if (totalExported > 0)
                TempData.Success($"Exporting to resource file finished successfully. Total new items {totalExported}.");
            else
                TempData.Danger("Error while creating resource file!");

            return RedirectToPage("Index");
        }
    }
}
