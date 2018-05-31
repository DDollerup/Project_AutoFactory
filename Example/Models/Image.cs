using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Example.Models
{
    public class Image
    {
        public Image()
        {
            Alt = "";
        }
        public int ID { get; set; }
        public string Url { get; set; }
        public string Alt { get; set; }
        public string TokenKey { get; set; }
    }
}