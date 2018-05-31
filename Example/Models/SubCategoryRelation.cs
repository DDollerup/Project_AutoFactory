using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Example.Models
{
    public class SubCategoryRelation
    {
        public int ID { get; set; }
        public int SubCategoryID { get; set; }
        public string TokenKey { get; set; }
    }
}