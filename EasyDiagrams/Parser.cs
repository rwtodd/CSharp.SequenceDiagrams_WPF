using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDiagrams
{

    class Parser
    {
        
        public static Diagram Parse(Tokenizer t)
        {
            var ans = new Diagram();
         
            while( !(ans.HasErrors || t.OutOfChars) )
            {
                Token next = t.NextToken();
                switch (next.type)
                {
                    case TokenType.TOK_EOL:  // empty lines are ok
                        break;   
                    case TokenType.TOK_TITLE:  // we have a title
                        getTitle(t,ans);
                        break;
                    case TokenType.TOK_NOTE:  // we have a note
                        getNote(t, ans);
                        break;
                    case TokenType.TOK_IDENTIFIER:
                        parseIdentifier(t, ans, next);
                        break;
                    default:  // anything else is a problem!
                        ans.HasErrors = true;
                        break;
                }
            }

            return ans;
        }

        private static void getNote(Tokenizer t, Diagram d)
        {
            // a NOTE takes the form:  NOTE Actor: the note is here....
            Token id = t.NextToken();
            if (id.type != TokenType.TOK_IDENTIFIER) { d.HasErrors = true; return; }
            var actor = d.MaybeNewActor(id.data);

            Token str = t.NextToken();
            string desc = null;
            switch(str.type) {
                case TokenType.TOK_STRING:
                    desc = str.data;
                    break;
                case TokenType.TOK_EOL:
                    desc = "";
                    break;
                default:
                    d.HasErrors = true;
                    return;
            }

            var ln = d.AddLine(actor, actor);
            ln.Desc = desc;
            ln.Note = true;
        }

        private static void parseIdentifier(Tokenizer t, Diagram d, Token first)
        {
            // OK, we got an identifier... let's make sure the Diagram knows about it...
            Actor left = d.MaybeNewActor(first.data);
            
            //  Valid continuations are:
            //   EOL          ... just define the actor so we have a good order
            //   STRING EOL   ... define the actor with a display name
            //   TO ID [DASHED] [STRING] EOL  ... define an arrow
            Token second = t.NextToken();
            switch (second.type)
            {
                case TokenType.TOK_EOL:  // no problem here...
                    break;
                case TokenType.TOK_STRING: // giving a display name...
                    left.DisplayName = second.data;
                    break;
                case TokenType.TOK_TO:  // defining an arrow...
                    parseArrow(t, d, left);
                    break;
                default:   // something went wrong here...
                    d.HasErrors = true;
                    break;
            }
        }

        private static void parseArrow(Tokenizer t, Diagram d, Actor left)
        {
            Token rightID = t.NextToken();
            
            // this should be an identifier... or SELF...
            Actor right = null;

            switch (rightID.type)
            {
                case TokenType.TOK_IDENTIFIER:
                    right = d.MaybeNewActor(rightID.data);
                    break;
                case TokenType.TOK_SELF:
                    right = left;
                    break;
                default:
                    d.HasErrors = true;
                    return;
            }

            // we definitely have a line now...
            var line = d.AddLine(left, right);

            // OK, now we have a possible DASHED and STRING...
            Token rest = t.NextToken();
            while (rest.type != TokenType.TOK_EOL)
            {
                switch (rest.type)
                {
                    case TokenType.TOK_DASHED:
                        line.Dashed = true;
                        break;
                    case TokenType.TOK_STRING:
                        line.Desc = rest.data;
                        break;
                    default:
                        d.HasErrors = true;
                        return;
                }
                rest = t.NextToken();
            }
        }

        private static void getTitle(Tokenizer t, Diagram d) {
            Token str = t.NextToken();
            if(str.type == TokenType.TOK_STRING) {
               d.Title = str.data;
            }
            else
            {
                d.HasErrors = true;
            }
        }
    }
}
