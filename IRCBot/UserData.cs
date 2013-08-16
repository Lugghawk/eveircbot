﻿using System;
using Iesi.Collections.Generic;

namespace Ircbot.Database
{

    public class User
    {
        public virtual Guid Id { get; protected set; }
        public virtual string userName { get; set; }
        public virtual int defaultChar { get; set; }
        public virtual ISet<Character> characters { get; set; }
        public virtual ISet<UserApi> apis { get; set;}

        public User()
        {
        }

        public User(string inputUserName)
        {
            userName = inputUserName;
        }

        public virtual void addApi(UserApi api)
        {
            if (this.apis == null)
            {
                this.apis = new Iesi.Collections.Generic.HashedSet<UserApi>();
            }
            api.user_id = this;
            this.apis.Add(api);

        }
    }

    public class NickUsers 
    {
        public virtual Guid Id { get; protected set; }
        public virtual string ircNick { get; set; }
        public virtual Guid user_id { get; set; }
        public virtual ISet<User> Users { get; set; }
    }

    public class UserApi 
    {
        public virtual Guid Id { get; protected set; }
        public virtual User user_id { get; set; }
        public virtual int apiUserId { get; set; }
        public virtual string apiKeyId { get; set; }

        public UserApi()
        {
        }

        public UserApi(int user_id, string key_id)
        {
            this.apiKeyId = key_id;
            this.apiUserId = user_id;
        }
    }

    public class Character
    {
        public virtual Guid Id { get; protected set; }
        public virtual User user_id { get; set; }
        public virtual string characterName { get; set; }
        public virtual int apiCharacterId { get; set; }

    }

}