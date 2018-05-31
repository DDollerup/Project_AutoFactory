using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Example.Models
{
    public class SubCategory
    {
        public SubCategory()
        {
            Description = "";
        }
        public int ID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }
}