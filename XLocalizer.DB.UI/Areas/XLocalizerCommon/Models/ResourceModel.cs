using XLocalizer.DataAnnotations;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace XLocalizer.DB.UI.Areas.XLocalizerCommon.Models
{
    public class ResourceEditModel
    {
        [ExRequired]
        [Display(Name = "Key")]
        public string Key { get; set; }
        
        [Display(Name = "New Key")]
        public string NewKey { get; set; }
    }

    public class ResourceListItem
    {
        [Display(Name = "ID")]
        public int ID { get; set; }

        [Display(Name = "Key")]
        public string Key { get; set; }

        [Display(Name = "Comment")]
        public string Comment { get; set; }

        public ICollection<string> Cultures { get; set; }
    }

    public class ResourceSearchModel
    {
        public int? ID { get; set; }
        public string Query { get; set; }
        public string Culture { get; set; }
    }

    public class ResourceInputModel
    {
        [Display(Name ="ID")]
        public int ID { get; set; }

        /// <summary>
        /// The resource key
        /// </summary>
        [ExRequired]
        [Display(Name ="Text")]
        public string Key { get; set; }

        /// <summary>
        /// Comment if any...
        /// </summary>
        [Display(Name ="Comment")]
        public string Comment { get; set; }

        /// <summary>
        /// Localized Value
        /// </summary>
        [ExRequired]
        [Display(Name ="Localized value")]
        public string Value { get; set; }

        /// <summary>
        /// Culture Id, two letter ISO name
        /// </summary>
        [ExRequired]
        [Display(Name ="Culture name")]
        public string CultureID { get; set; }

        /// <summary>
        /// Enable, disable
        /// </summary>
        public bool IsActive { get; set; }
    }
}
