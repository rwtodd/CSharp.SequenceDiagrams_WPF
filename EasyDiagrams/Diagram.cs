using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDiagrams
{
    class Actor {
        public string Name { get ; set; }
        public string DisplayName { get; set; }

        public Actor(string name, string disp) { Name = name; DisplayName = disp; }
    }

    class ActorLine
    {
        public Actor From { get; set; }
        public Actor To { get; set; }
        public bool Dashed { get; set; }
        public bool Note { get; set; }
        public string Desc { get; set; }

        public ActorLine(Actor f, Actor t)
        {
            From = f;
            To = t;
            Note = false;
            Desc = "";
        }
    }

    // this is a very public data container... no encapsulation.
    class Diagram
    {
        public string Title { get; set; }
        public bool HasErrors { get; set; }
        public List<Actor> Actors { get; set; }
        public List<ActorLine> Lines { get; set; }

        public Diagram()
        {
            HasErrors = false;
            Title = "Untitled";
            Actors = new List<Actor>();
            Lines = new List<ActorLine>();
        }

        public Actor MaybeNewActor(string name)
        {
            var searchFor = name.ToUpper();
            var ans = Actors.Find(a => a.Name.Equals(searchFor));
            if (ans == null)
            {
                ans = new Actor(searchFor,name);
                Actors.Add(ans);
            }
            return ans;
        }

        public ActorLine AddLine(Actor from, Actor to)
        {
            var ans = new ActorLine(from, to);
            Lines.Add(ans);
            return ans;
        }
    }
}
