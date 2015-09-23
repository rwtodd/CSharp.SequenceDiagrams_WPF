using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyDiagrams
{
    enum TokenType {  
                      TOK_TITLE,      // makes the title of the diagram...
                      TOK_NOTE,       // makes a note on a line
                      TOK_IDENTIFIER, // any non-numeric group of characters not otherwise classified
                      TOK_TO,       // indicates an arrow
                      TOK_SELF,     // indicates a self-liine
                      TOK_DASHED,   // indicates dashed arrow
                      TOK_STRING,   // Everything after the colon ":"
                      TOK_EOL       // The end of a statement/line
    }


    // basically an all-public data-holding class. No encapsulation here.
    class Token
    {
        public TokenType type;
        public string data;

        public Token(TokenType tt, string d) { type = tt; data = d; }

        public override string ToString()
        {
            return String.Format("Token: type <{0}>, string <{1}>", type, data);
        }
    }



    class Tokenizer
    {
        private string src;
        private int index;

        public Tokenizer(string text)
        {
            src = text;
            index = 0;
        }

        public Token NextToken()
        {
            // if we had a token waiting, just return it
            Token ans = null;

            // otherwise... process some text.
            skipWS();
            if (!OutOfChars)
            {
                char c = nextChar();
                switch (c)
                {
                    case '#':   // comment
                        getToEOL();
                        ans = new Token(TokenType.TOK_EOL, null);
                        break;
                    case '\n':  // end of line/statement
                        ans = new Token(TokenType.TOK_EOL, null);
                        break;
                    case ':':   // start of free-form string arg
                        ans = new Token(TokenType.TOK_STRING, getToEOL());
                        break;
                    default:    // must be a keyword or identifier...
                        ans = processIdentifier(c);
                        break;
                }
            }
            return ans;
        }

        public bool OutOfChars { get { return index > src.Length; } }


        // process characters and figure out if it's a keyword or not.
        private Token processIdentifier(char first)
        {
            Token ans = null;
            var sb = new StringBuilder();
            sb.Append(first);

            char c = nextChar();
            while (c != ':' && !Char.IsWhiteSpace(c))
            {
                sb.Append(c);
                c = nextChar();
            }
            ungetChar(); // push back whatever char we stopped on.

            // now we have an identifier... we need to see if it's a keyword...
            string ident = sb.ToString();
            switch (ident.ToUpper())
            {
                case "TO":
                    ans = new Token(TokenType.TOK_TO, ident);
                    break;
                case "SELF":
                    ans = new Token(TokenType.TOK_SELF, ident);
                    break;
                case "DASHED":
                    ans = new Token(TokenType.TOK_DASHED, ident);
                    break;
                case "TITLE":
                    ans = new Token(TokenType.TOK_TITLE, ident);
                    break;
                case "NOTE":
                    ans = new Token(TokenType.TOK_NOTE, ident);
                    break;
                default:
                    ans = new Token(TokenType.TOK_IDENTIFIER, ident);
                    break;
            }
            return ans;
        }

        // get a string up to the EOL
        private string getToEOL()
        {
            var sb = new StringBuilder();
            char c = nextChar();
            while ((c != '\n') && (c != '#'))
            {
                sb.Append(c);
                c = nextChar();
            }
            ungetChar(); // push back the '\n'
            return sb.ToString().TrimEnd();
        }

        private void ungetChar() { index--; }
        private char nextChar()
        {
            char ans = '\n';
            if (index <  src.Length) { ans = src[index]; }
            index++;
            return ans;
        }

        private void skipWS() {
            while(!OutOfChars) {
                char c = nextChar();
                if(c == '\n' || !Char.IsWhiteSpace(c)) break;
            }
            ungetChar(); // put back whatever stopped us...
        }
       
    }
}
