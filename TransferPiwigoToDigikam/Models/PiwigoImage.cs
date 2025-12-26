using System;
using System.Collections.Generic;

namespace TransferPiwigoToDigikam.Models
{
    public class PiwigoImage
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string File { get; set; }
        public string ElementUrl { get; set; }
        public string Comment { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime DateAvailable { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public List<string> Categories { get; set; }
        public List<string> Tags { get; set; }

        public PiwigoImage()
        {
            Categories = new List<string>();
            Tags = new List<string>();
        }
    }

    public class PiwigoCategory
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string FullPath { get; set; }
        public int? ParentId { get; set; }
    }
}
