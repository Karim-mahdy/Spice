using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Spice.Models
{
    public class MenuItem
    {
        [Key]
        
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [AllowHtml]
        public string Descrition { get; set; }
        public string Spicyness { get; set; }

        public enum Espicy { NA=0,Notspicy=1,Spicy=2,VerySpicy=3}

        public string Image { get; set; }


        //[Display(Name="Category")]
        //public int CategoryId { get; set; }
        //[ForeignKey("CategoryId")]
        //public virtual Category Category { get; set; }

        [Display(Name = "SubCategory")]
        public int SubCategoryId { get; set; }
        [ForeignKey("SubCategoryId")]
        public virtual SubCategory SubCategory { get; set; }

        [Range(1,int.MaxValue ,ErrorMessage ="prise should be greater than ${1}")]
        public double Price { get; set; }
    }
}
