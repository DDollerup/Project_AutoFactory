using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Example.Models.ViewModels
{
    public class ProductVM
    {
        public Product Product { get; set; }
        public List<Category> Categories { get; set; }
        public List<SubCategory> Subcategories { get; set; }
        public List<Image> Images { get; set; }
    }
}