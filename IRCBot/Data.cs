using System;
using Iesi.Collections.Generic;
using System.Data.SqlTypes;

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

        public virtual void addCharacter(Character character)
        {
            if (this.characters == null)
            {
                this.characters = new Iesi.Collections.Generic.HashedSet<Character>();
            }
            character.user_id = this;
            this.characters.Add(character);
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
        public virtual UserApi api { get; set; }

        public Character()
        {
        }

        public Character(string name, int charId) {
            this.characterName = name;
            this.apiCharacterId = charId;
        }

    }

    public class InvType
    {
        public virtual int typeID { get; set; }
        public virtual int groupID { get; set; }
        public virtual string typeName { get; set; }
        public virtual string description { get; set; }
        public virtual float mass { get; set; }
        public virtual float volume { get; set; }
        public virtual float capacity { get; set; }
        public virtual int portionSize {get;set;}
        public virtual int raceID { get; set; }
        public virtual double basePrice { get; set; }
        public virtual bool published { get; set; }
        public virtual int marketGroupID { get; set; }
        public virtual float chanceOfDuplicating { get; set; }

        public InvType()
        {
        }


    }

    public class SolarSystem
    {
        public virtual Region region { get; set; }
        public virtual Constellation constellation { get; set; }
        public virtual int solarSystemID { get; set; }
        public virtual string solarSystemName { get; set; }
        public virtual float security { get; set; }
    }
    public class Constellation
    {
        public virtual Region region {get;set;}
        public virtual int constellationID { get; set; }
        public virtual string constellationName { get; set; }


    }
    public class Region
    {
        public virtual int regionID { get; set; }
        public virtual string regionName { get; set; }
    }

}