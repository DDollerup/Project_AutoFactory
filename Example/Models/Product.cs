using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Example.Models
{
    public class Product
    {
        public int ID { get; set; }
        public string Token { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int CategoryID { get; set; }
        public int SubCategoryID { get; set; }
    }
}