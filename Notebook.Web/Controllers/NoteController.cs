﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Notebook.Business.Managers.Abstract;
using Notebook.Entities.Entities;
using Notebook.Entities.Enums;
using Notebook.Web.Filters;
using Notebook.Web.Models;
using Notebook.Web.Models.Datatable;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Notebook.Web.Controllers
{
    public class NoteController : Controller
    {
        public string _lockIcon = "<i class='fa fa-lock'></i> ";
        public string _noteIcon = "<i class='fa fa-newspaper-o'></i> ";

        private IStringLocalizer<NoteController> _localizer;
        private INoteManager _noteManager;
        private IUserNoteManager _userNoteManager;
        private IGroupManager _groupManager;
        private IFolderManager _folderManager;
        public NoteController(IStringLocalizer<NoteController> localizer, INoteManager noteManager, IGroupManager groupManager, IFolderManager folderManager, IUserNoteManager userNoteManager)
        {
            _localizer = localizer;
            _noteManager = noteManager;
            _userNoteManager = userNoteManager;
            _groupManager = groupManager;
            _folderManager = folderManager;
        }

        #region CRUD

        [HttpGet]
        [Route("~/{ID}/note-detail/{title?}")]
        public IActionResult Detail(string ID = "")
        {
            var _user = HttpContext.Session.GetSession<User>("User");

            NoteDetailModel detail = null;

            var _note = _noteManager.getMany(a => a.ID == ID).Include(a => a.Users).FirstOrDefault();
            if (_note != null)
            {
                detail = new NoteDetailModel();
                detail.ID = _note.ID;
                detail.Title = _note.Title;
                detail.Explanation = _note.Explanation;
                detail.Content = _note.Content;
                detail.CreateDate = _note.CreateDate;
                detail.UpdateDate = _note.UpdateDate;
                detail.Visible = _note.Visible;
                detail.OwnerID = _note.UserID;
                detail.ReadCount = _note.ReadCount;
                detail.UserCount = _userNoteManager.getMany(a => a.NoteID == _note.ID).Count();

                _noteManager.UpdateNoteReadCount(_note);
            }

            return View(detail);
        }

        [TypeFilter(typeof(AccountFilterAttribute))]
        [HttpGet]
        [Route("~/add-note")]
        [Route("~/{id?}/edit-note")]
        public IActionResult Form(string id = "",string groupId = "", string folderId = "")
        {
            var _sessionUser = HttpContext.Session.GetSession<User>("User");

            var _note = _noteManager.getOne(a => a.ID == id && a.UserID == _sessionUser.ID);

            return PartialView(_note ?? new Note { Visible = Visible.Public, UserID = _sessionUser.ID, GroupID = groupId, FolderID = folderId });
        }

        [TypeFilter(typeof(AccountFilterAttribute))]
        [HttpPost]
        [Route("~/addNote")]
        [ValidateAntiForgeryToken]
        public IActionResult Add(Note _note)
        {
            var _user = HttpContext.Session.GetSession<User>("User");

            _noteManager.Add(_note);
            _userNoteManager.Add(new UserNote{ Note = _note, User = _user, CreateDate = DateTime.Now, Status = Status.Owner });

            TempData["message"] = HelperMethods.JsonConvertString(new TempDataModel { type = "success", message = _localizer["Transaction successful"] });

            return Redirect(TempData["BeforeUrl"].ToString());
        }

        [TypeFilter(typeof(AccountFilterAttribute))]
        [HttpPost]
        [Route("~/editNote")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Note note)
        {
            _noteManager.Update(note);

            TempData["message"] = HelperMethods.JsonConvertString(new TempDataModel { type = "success", message = _localizer["Transaction successful"] });

            return Redirect(string.Format("/note/{0}/{1}",note.ID,note.Title.ClearHtmlTagAndCharacter()));
        }

        [TypeFilter(typeof(AccountFilterAttribute))]
        [HttpPost]
        [Route("~/{ID?}/delete-note")]
        public JsonResult Delete(string ID = "")
        {
            var _user = HttpContext.Session.GetSession<User>("User");

            _noteManager.Delete(ID, _user.ID);

            TempData["message"] = HelperMethods.JsonConvertString(new TempDataModel { type = "success", message = _localizer["Transaction successful"] });

            return Json("");
        }

        #endregion

    }
}
