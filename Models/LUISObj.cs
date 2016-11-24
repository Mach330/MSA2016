using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Bank_Bot.Models
{
    public class LUISObj
    {

        public class Resolution
        {
        }

        public class Value
        {
            public string entity { get; set; }
            public string type { get; set; }
            public Resolution resolution { get; set; }
        }

        public class Parameter
        {
            public string name { get; set; }
            public string type { get; set; }
            public bool required { get; set; }
            public List<Value> value { get; set; }
        }

        public class Action
        {
            public bool triggered { get; set; }
            public string name { get; set; }
            public List<Parameter> parameters { get; set; }
        }

        public class TopScoringIntent
        {
            public string intent { get; set; }
            public double score { get; set; }
            public List<Action> actions { get; set; }
        }

        public class Resolution2
        {
        }

        public class Value2
        {
            public string entity { get; set; }
            public string type { get; set; }
            public Resolution2 resolution { get; set; }
        }

        public class Parameter2
        {
            public string name { get; set; }
            public string type { get; set; }
            public bool required { get; set; }
            public List<Value2> value { get; set; }
        }

        public class Action2
        {
            public bool triggered { get; set; }
            public string name { get; set; }
            public List<Parameter2> parameters { get; set; }
        }

        public class Intent
        {
            public string intent { get; set; }
            public double score { get; set; }
            public List<Action2> actions { get; set; }
        }

        public class Resolution3
        {
        }

        public class Entity
        {
            public string entity { get; set; }
            public string type { get; set; }
            public int startIndex { get; set; }
            public int endIndex { get; set; }
            public double score { get; set; }
            public Resolution3 resolution { get; set; }
        }

        public class Dialog
        {
            public string contextId { get; set; }
            public string status { get; set; }
        }

        public class RootObject
        {
            public string query { get; set; }
            public TopScoringIntent topScoringIntent { get; set; }
            public List<Intent> intents { get; set; }
            public List<Entity> entities { get; set; }
            public Dialog dialog { get; set; }
        }
    }
}