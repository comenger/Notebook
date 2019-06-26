﻿using Notebook.Entities.Entities;
using Notebook.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Notebook.Web.Models
{
    public class FolderDetailModel
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Explanation { get; set; }
        public DateTime CreateDate { get; set; }
        public Visible Visible { get; set; }
        public Member MemberType { get; set; }
        public int NoteCount { get; set; }
        public int UserCount { get; set; }
        public string List { get; set; }

        public Group Group { get; set; }
    }
}
