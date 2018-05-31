using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Example.Models
{
    public class CategoryRelation
    {
        public int ID { get; set; }
        public int CategoryID { get; set; }
        public string TokenKey { get; set; }
    }
}