﻿using Notebook.Entities.Entities;
using System;
using System.Collections.Generic;

namespace Notebook.Business.Managers.Abstract
{
    public interface IFolderNoteManager : IManager<FolderNote>
    {
        void Add(string NoteID, string FolderID);
    }
}
