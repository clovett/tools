using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsBuildImportGraph
{
    class Variable : Expression
    {
        public string Name;
    }

    class Namespace : Expression
    {
        public string Name;
    }

    class MethodCall : Expression
    {
        public string Namespace;
        public string Name;
        public List<Expression> Arguments = new List<Expression>();
    }

    class Literal : Expression
    {
        public string Value;
    }

    class Expression
    {
        // Expr ::= (Method | Literal | Variable)*
        // Method ::= [ Namespace :: ] Literal '(' Arguments ')'
        // Arguments ::= Expr ',' Expr
        // Aguments ::= Expr
        // Variable ::= '$(' Expr ')'
        // Literal ::= Literal

        private Expression Parse(string expr)
        {
            Initialize(expr);

            // todo: this is not complete...
            StringBuilder sb = new StringBuilder();
            for (Token token = GetNextToken(); token.Type != TokenType.EOF; token = GetNextToken())
            {
                switch (token.Type)
                {
                    case TokenType.Dollar:
                        token = GetNextToken();
                        if (token.Type == TokenType.OpenParen)
                        {
                            Variable var = ParseVariableName();
                        }
                        else
                        {
                            throw new Exception("Expecting '('");
                        }
                        break;
                    case TokenType.OpenSquare:
                        Namespace ns = ParseNamespace();
                        break;
                    case TokenType.CloseSquare:
                        break;
                    case TokenType.NamespaceSeparator:
                        break;
                    case TokenType.Literal:
                        break;
                    case TokenType.OpenParen:
                        break;
                    case TokenType.CloseParen:
                        // done!
                        break;
                    case TokenType.Comma:
                        break;
                }
            }
            return null;
        }

        private Variable ParseVariableName()
        {
            StringBuilder sb = new StringBuilder();
            Variable var = new Variable();
            bool done = false;
            for (Token token = GetNextToken(); !done && token.Type != TokenType.EOF; token = GetNextToken())
            {
                switch (token.Type)
                {
                    case TokenType.Dollar:
                        token = GetNextToken();
                        if (token.Type == TokenType.OpenParen)
                        {
                            Variable nested = ParseVariableName();
                            // todo: resolve this variable and append it to the string builder...
                        }
                        else
                        {
                            throw new Exception("Expecting '('");
                        }
                        break;
                    case TokenType.OpenSquare:
                        MethodCall call = ParseQualifiedMethodCall();
                        // todo: resolve this method call and append result to our string builder...
                        break;
                    case TokenType.Literal:
                        // could be the name of a method if the next token is an open paren.
                        token = GetNextToken();
                        if (token.Type == TokenType.OpenParen)
                        {
                            PushToken();
                            PushToken();
                            MethodCall method = ParseMethodCall();
                        }
                        break;
                    case TokenType.CloseParen:
                        done = true;
                        break;
                    case TokenType.NamespaceSeparator:
                    case TokenType.CloseSquare:
                    case TokenType.Comma:
                    case TokenType.OpenParen:
                        throw new Exception("Unexpected token in namespace qualifier: '" + token.Literal + "'");
                }
            }

            return var;
        }

        private MethodCall ParseMethodCall()
        {
            // name '(' args ')'
            MethodCall result = null;
            return result;
        }

        private MethodCall ParseQualifiedMethodCall()
        {
            Namespace ns = ParseNamespace();

            MethodCall result = ParseMethodCall();
            if (result.Namespace == null)
            {
                result.Namespace = ns.Name;
            }
            else
            {

            }

            return result;
        }

        private Namespace ParseNamespace()
        {
            StringBuilder sb = new StringBuilder();
            Namespace ns = new Namespace();
            bool done = false;
            for (Token token = GetNextToken(); !done && token.Type != TokenType.EOF; token = GetNextToken())
            {
                switch (token.Type)
                {
                    case TokenType.CloseSquare:
                        // done!
                        done = true;
                        break;
                    case TokenType.Literal:
                        sb.Append(token.Literal);
                        break;
                    case TokenType.Dollar:
                    case TokenType.NamespaceSeparator:
                    case TokenType.OpenParen:
                    case TokenType.CloseParen:
                    case TokenType.OpenSquare:
                    case TokenType.Comma:
                        throw new Exception("Unexpected token in namespace qualifier: '" + token.Literal + "'");
                }
                if (done)
                {
                    break;
                }
            }
            ns.Name = sb.ToString();
            return ns;
        }

        List<Token> tokens;
        int tokenPos;

        private void Initialize(string expr)
        {
            tokens = new List<Token>(Tokenize(expr).ToArray());
            tokenPos = 0;
        }

        Token GetNextToken()
        {
            if (tokenPos < tokens.Count)
            {
                return tokens[tokenPos++];
            }
            return eof;
        }

        Token eof = new Token() { Type = TokenType.EOF };

        void PushToken()
        {
            if (tokenPos > 0)
            {
                tokenPos--;
            }
        }


        public enum TokenType
        {
            None,
            Dollar,
            OpenSquare,
            CloseSquare,
            NamespaceSeparator,
            Literal,
            OpenParen,
            CloseParen,
            Comma,
            EOF
        }

        public class Token
        {
            public TokenType Type;
            public string Literal;
        }

        char[] lexicalSymbols = new char[] { '$', '(', ')', '[', ']', ':', ',' };

        private IEnumerable<Token> Tokenize(string expr)
        {
            for (int i = 0, n = expr.Length; i < n; i++)
            {
                char ch = expr[i];
                switch (ch)
                {
                    case '$':
                        yield return new Token() { Type = TokenType.Dollar, Literal = "$" };
                        break;
                    case '(':
                        yield return new Token() { Type = TokenType.OpenParen, Literal = "(" };
                        break;
                    case ')':
                        yield return new Token() { Type = TokenType.CloseParen, Literal = ")" };
                        break;
                    case '[':
                        yield return new Token() { Type = TokenType.OpenSquare, Literal = "[" };
                        break;
                    case ']':
                        yield return new Token() { Type = TokenType.CloseSquare, Literal = "]" };
                        break;
                    case ':':
                        if (i + 1 < n && expr[i + 1] == ':')
                        {
                            i++;
                            yield return new Token() { Type = TokenType.NamespaceSeparator, Literal = "::" };
                        }
                        else
                        {
                            throw new Exception("Expecting another ':' to make a namespace separator");
                        }
                        break;
                    case ',':
                        yield return new Token() { Type = TokenType.Comma, Literal = "," };
                        break;
                    case ' ':
                    case '\t':
                    case '\r':
                    case '\n':
                        // strip whitespace.
                        break;
                    default:
                        int j = expr.IndexOfAny(lexicalSymbols, i);
                        if (j < 0)
                        {
                            j = expr.Length;
                        }
                        if (j > i)
                        {
                            yield return new Token() { Type = TokenType.Literal, Literal = expr.Substring(i, j - i) };
                        }
                        else
                        {
                            // are we stuck ???
                        }
                        break;
                }
            }
        }
    }
}
