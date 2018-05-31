using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BotSample.Model
{
    public class BookLUIS
    {
        public string query { get; set; }
        public Intent[] intents { get; set; }
        public Entity[] entities { get; set; }
    }
}