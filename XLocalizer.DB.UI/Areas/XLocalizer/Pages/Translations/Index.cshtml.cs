using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using XLocalizer.DB.Models;
using LazZiya.TagHelpers.Alerts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net;
using Microsoft.Extensions.Logging;
using XLocalizer.Translate;

namespace XLocalizer.DB.UI.Areas.XLocalizer.Pages.Translations
{
    [ValidateAntiForgeryToken]
    public class IndexModel : PageModel
    {
        private readonly IDbResourceManager _resManager;
        private readonly IDbCultureManager _culManager;
        private readonly IStringTranslatorFactory _translatorFactory;
        private readonly ILogger _logger;

        public readonly List<SelectListItem> TranslationProviders;

        public IndexModel(IDbResourceManager manager, IDbCultureManager cManager, IStringTranslatorFactory translatorFactory, ILogger<IndexModel> log)
        {
            _logger = log;
            _resManager = manager;
            _culManager = cManager;
            _translatorFactory = translatorFactory;

            // get all registered Resource services names
            TranslationProviders = new List<SelectListItem>();

            foreach (var ts in translatorFactory.ServiceNames())
            {
                TranslationProviders.Add(new SelectListItem { Text = ts, Value = ts });
            }
        }

        [BindProperty(SupportsGet = true)]
        public string DefaultCulture { get; set; }

        /// <summary>
        /// optionally set target culture 
        /// to navigate directly to  the culture edit box
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string TargetCulture { get; set; }

        /// <summary>
        /// just in case coming from different pages (Resources or Cultures...)
        /// to return to the desired page.
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public string ReturnUrl { get; set; }

        /// <summary>
        /// Resource ID
        /// </summary>
        [BindProperty(SupportsGet = true)]
        public int ResourceID { get; set; }

        /// <summary>
        /// Resource entity including related translations
        /// </summary>
        public IEnumerable<XDbResource> Resources { get; set; }

        [BindProperty(SupportsGet = true)]
        public XDbResource Resource { get; set; }

        /// <summary>
        /// Available cultures list
        /// </summary>
        public IEnumerable<XDbCulture> Cultures { get; set; }

        private class CRUDAjaxReponse
        {
            public HttpStatusCode StatusCode { get; set; }
            /// <summary>
            /// Target culture
            /// </summary>
            public string Target { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (ResourceID == 0)
            {
                TempData.Warning("Resource ID can't be zero!");
                return RedirectToPage("Resources");
            }

            var res = await _resManager.GetResourceAsync<XDbResource>(x => x.ID == ResourceID);

            var exp = new List<Expression<Func<XDbResource, bool>>> { x => x.Key == res.Key };

            int _;
            (Resources, _) = await _resManager.ResourcesSetListAsync(1, 848, exp);

            if (Resources == null)
            {
                TempData.Danger("Resource not found!");
                return RedirectToPage("Index");
            }

            var culturesExp = new List<Expression<Func<XDbCulture, bool>>> { };
            if (!string.IsNullOrWhiteSpace(TargetCulture))
            {
                culturesExp.Add(x => x.ID == TargetCulture);
            }

            _ = 0;
            (Cultures, _) = await _culManager.CulturesListAsync(1, 848, null);

            DefaultCulture = Cultures?.SingleOrDefault(c => c.IsDefault == true)?.ID;

            return Page();
        }

        public async Task<JsonResult> OnPostSaveTranslationAsync()
        {
            if (!ModelState.IsValid)
            {
                return new JsonResult(new CRUDAjaxReponse { StatusCode = HttpStatusCode.BadRequest, Target = Resource.CultureID });
            }

            // get Resource from DB
            var entity = await _resManager.GetResourceAsync<XDbResource>(x => x.ID == Resource.ID && x.CultureID == Resource.CultureID);

            bool success;

            if (entity == null)
            {
                // create a new Resource if no record exist for this resource and culture 
                success = await _resManager.AddResourceAsync<XDbResource>(Resource);
            }
            else
            {
                _logger.LogInformation($"UPDATE Resource: {entity.IsActive}  {Resource.IsActive}");
                // Update existing Resource if record is exist for this resource and culture 
                entity.Value = Resource.Value;
                entity.IsActive = Resource.IsActive;
                success = await _resManager.UpdateResourceAsync<XDbResource>(entity);
            }

            _logger.LogInformation($"Save new translation: {Resource.ID}, {Resource.Key}, {Resource.Value}, {Resource.Comment}, {Resource.IsActive}");
            return success
                ? new JsonResult(new CRUDAjaxReponse { StatusCode = HttpStatusCode.OK, Target = Resource.CultureID })
                : new JsonResult(new CRUDAjaxReponse { StatusCode = HttpStatusCode.InternalServerError, Target = Resource.CultureID });
        }

        public async Task<JsonResult> OnPostDeleteTranslationAsync()
        {
            if (Resource.ID == 0)
            {
                return new JsonResult(new CRUDAjaxReponse { StatusCode = HttpStatusCode.BadRequest, Target = Resource.CultureID });
            }

            if (string.IsNullOrWhiteSpace(Resource.CultureID))
            {
                return new JsonResult(new CRUDAjaxReponse { StatusCode = HttpStatusCode.BadRequest, Target = Resource.CultureID });
            }

            var entity = await _resManager.GetResourceAsync<XDbResource>(x => x.ID == Resource.ID && x.CultureID == Resource.CultureID);

            if (entity == null)
            {
                return new JsonResult(new CRUDAjaxReponse { StatusCode = HttpStatusCode.NotFound, Target = Resource.CultureID });
            }

            return await _resManager.DeleteResourceAsync<XDbResource>(x => x.ID == entity.ID)
                ? new JsonResult(new CRUDAjaxReponse { StatusCode = HttpStatusCode.OK, Target = Resource.CultureID })
                : new JsonResult(new CRUDAjaxReponse { StatusCode = HttpStatusCode.InternalServerError, Target = Resource.CultureID });
        }

        public async Task<JsonResult> OnPostOnlineTranslateAsync(string provider, string text, string source, string target, string format)
        {
            var service = _translatorFactory.Create(provider);
            var result = await service.TranslateAsync(source, target, text, format);
            return new JsonResult(result);
        }
    }
}
