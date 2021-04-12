﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using sly.i18n;
using sly.lexer;

namespace sly.parser
{
    public class UnexpectedTokenSyntaxError<T> : ParseError, IComparable
    {
        public UnexpectedTokenSyntaxError(Token<T> unexpectedToken, params T[] expectedTokens)
        {
            ErrorType = unexpectedToken.IsEOS ? ErrorType.UnexpectedEOS : ErrorType.UnexpectedToken;
            
            UnexpectedToken = unexpectedToken;
            if (expectedTokens != null)
            {
                ExpectedTokens = new List<T>();
                ExpectedTokens.AddRange(expectedTokens);
            }
            else
            {
                ExpectedTokens = null;
            }
        }


        public Token<T> UnexpectedToken { get; set; }

        public List<T> ExpectedTokens { get; set; }

        public override int Line
        {
            get
            {
                var l = UnexpectedToken?.Position?.Line;
                return l.HasValue ? l.Value : 1;
            }
        }

        public override int Column
        {
            get
            {
                var c = UnexpectedToken?.Position?.Column;
                return c.HasValue ? c.Value : 1;
            }
        }

        public override string ErrorMessage
        {
            get
            {
                
                Message message = Message.UnexpectedToken;
                if (UnexpectedToken.IsEOS)
                {
                    message = Message.UnexpectedEos;
                    if (ExpectedTokens != null && ExpectedTokens.Any())
                    {
                        message = Message.UnexpectedEosExpecting;
                    }
                }
                else
                {
                    message = Message.UnexpectedToken;
                    if (ExpectedTokens != null && ExpectedTokens.Any())
                    {
                        message = Message.UnexpectedTokenExpecting;
                    }
                }
                
                
                var expecting = new StringBuilder();
                
                if (ExpectedTokens != null && ExpectedTokens.Any())
                {
                    

                    foreach (var t in ExpectedTokens)
                    {
                        expecting.Append(t);
                        expecting.Append(", ");
                    }
                }

                return I18N.Instance.GetText(message, UnexpectedToken.Value, UnexpectedToken.TokenID.ToString(), expecting.ToString());
            }
        }

        public override string ToString()
        {
            return ErrorMessage;
        }
    }
}