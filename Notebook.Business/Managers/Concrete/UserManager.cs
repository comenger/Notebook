﻿using Microsoft.EntityFrameworkCore;
using Notebook.Business.Managers.Abstract;
using Notebook.Business.Tools.Logging;
using Notebook.Business.Tools.Validation.FluentValidation;
using Notebook.Core.Aspects.SimpleProxy.Caching;
using Notebook.Core.Aspects.SimpleProxy.Logging;
using Notebook.Core.Aspects.SimpleProxy.Validation;
using Notebook.Core.CrossCuttingConcerns.Caching.MemoryCache;
using Notebook.Core.CrossCuttingConcerns.Logging;
using Notebook.DataAccess.DataAccess.Abstract;
using Notebook.Entities.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Notebook.Business.Managers.Concrete
{
    public class UserManager : Manager<User>, IUserManager
    {
        private IUserDal servisDal;
        public UserManager(IUserDal _servisDal) : base(_servisDal)
        {
            servisDal = _servisDal;
        }

        [Validate(typeof(User),typeof(UserFluentValidation))]
        public override void Add(User model)
        {
            EmailControl(model.Email);
            UsernameControl(model.Username);

            model.Password = model.Password.SHA256Encrypt();
            model.CreateDate = DateTime.Now;
            model.LastActiveDate = DateTime.Now;

            base.Add(model);
        }


        private void EmailControl(string email)
        {
            if (servisDal.getOne(a => a.Email == email) != null)
            {
                throw new Exception("This email address is not available");
            }
        }

        private void UsernameControl(string username)
        {
            if (!string.IsNullOrEmpty(username) && servisDal.getOne(a => a.Username == username) != null)
            {
                throw new Exception("This username is not available");
            }
        }

        //[Cache(typeof(MemoryCacheManager), 20)]
        //[Log(typeof(FileLogger),LogType.Info)]
        public override List<User> getAll(int Skip, int Take)
        {
            return base.getAll(Skip, Take);
        }

        //[Cache(typeof(MemoryCacheManager), 20)]
        public override IQueryable<User> getAll()
        {
            return base.getAll();
        }

        public void LastActiveDateUpdate(User user)
        {
            user.LastActiveDate = DateTime.Now;

            servisDal.Update(user);
        }

        public User Login(User user)
        {
            var _user = servisDal.getMany(a => (a.Username == user.Email || a.Email == user.Email) && a.Password == user.Password.SHA256Encrypt()).Include(a => a.Role).FirstOrDefault();
            if (_user != null)
            {
                if (_user.Approve)
                {
                    LastActiveDateUpdate(_user);

                    return _user;
                }
                else
                {
                    throw new Exception("Your account is not active");
                }
            }
            else
            {
                throw new Exception("Username or password is wrong");
            }
        }

        public User Cookie(string key)
        {
            User user = null;

            if (!string.IsNullOrEmpty(key))
            {
                user = servisDal.getMany(a => a.Email == key).Include(a => a.Role).FirstOrDefault();
                if (user != null)
                {
                    LastActiveDateUpdate(user);
                }
            }

            return user;
        }
    }
}
