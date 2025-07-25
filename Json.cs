using System;
using System.Collections.Generic;
using System.Text;

// For some reason Unity's JsonUtility doesn't work with arrays, even though it is supposed to.
// Maybe it is some sort of version incompatibility, maybe something else.
// Anyway, I wrote my own JSON parser, which seems to fix it. It is limited though.
namespace BskyRoyale {
    public static class Json {
        public enum TokenKind {
            Int,
            String,
            LBrack,
            RBrack,
            LBrace,
            RBrace,
            Comma,
            Colon,
            True,
            False,
            Null,
            Eof,
        }

        public struct Token {
            public TokenKind Kind;
            public string Value;
            public int Length;
        }

        public class Tokenizer {
            private string json;
            private int pos = 0;

            public Tokenizer(string json) {
                this.json = json;
            }

            public Token Peek() {
                var i = pos;

                while (i < json.Length && Char.IsWhiteSpace(json[i])) {
                    ++i;
                }

                if (i >= json.Length) {
                    return new Token {
                        Kind = TokenKind.Eof,
                    };
                }

                TokenKind Kind;

                int start = i;
                switch (json[i++]) {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9': {
                        while (Char.IsDigit(json[i])) {
                            ++i;
                        }
                        Kind = TokenKind.Int;
                        // TODO: Floats? Not currently needed.
                        break;
                    }
                    case '"': {
                        var sb = new StringBuilder();

                        for (; ; )
                        {
                            var c = json[i++];
                            if (c == '\\') {
                                switch (json[i++]) {
                                    case '"': {
                                        sb.Append('"');
                                        break;
                                    }
                                    case '\\': {
                                        sb.Append('\\');
                                        break;
                                    }
                                    case '/': {
                                        sb.Append('/');
                                        break;
                                    }
                                    case 't': {
                                        sb.Append('\t');
                                        break;
                                    }
                                    case 'b': {
                                        sb.Append('\b');
                                        break;
                                    }
                                    case 'r': {
                                        sb.Append('\r');
                                        break;
                                    }
                                    case 'n': {
                                        sb.Append('\n');
                                        break;
                                    }
                                    case 'u': {
                                        sb.Append((char)Convert.ToUInt16(json.Substring(i, 4), 16));
                                        i += 4;
                                        break;
                                    }
                                    default: {
                                        throw new ArgumentException("Invalid escape");
                                    }
                                }
                            } else if (c == '"') {
                                break;
                            } else {
                                sb.Append(c);
                            }
                        }

                        return new Token {
                            Kind = TokenKind.String,
                            Value = sb.ToString(),
                            Length = i - pos,
                        };
                    }
                    case 't': {
                        if (json[i++] != 'r' || json[i++] != 'u' || json[i++] != 'e') {
                            throw new ArgumentException("Expected true");
                        }
                        Kind = TokenKind.True;
                        break;
                    }
                    case 'f': {
                        if (json[i++] != 'a' || json[i++] != 'l' || json[i++] != 's' || json[i++] != 'e') {
                            throw new ArgumentException("Expected false");
                        }
                        Kind = TokenKind.False;
                        break;
                    }
                    case 'n': {
                        if (json[i++] != 'u' || json[i++] != 'l' || json[i++] != 'l') {
                            throw new ArgumentException("Expected null");
                        }
                        Kind = TokenKind.Null;
                        break;
                    }
                    case ',': {
                        Kind = TokenKind.Comma;
                        break;
                    }
                    case ':': {
                        Kind = TokenKind.Colon;
                        break;
                    }
                    case '[': {
                        Kind = TokenKind.LBrack;
                        break;
                    }
                    case ']': {
                        Kind = TokenKind.RBrack;
                        break;
                    }
                    case '{': {
                        Kind = TokenKind.LBrace;
                        break;
                    }
                    case '}': {
                        Kind = TokenKind.RBrace;
                        break;
                    }
                    default: {
                        throw new ArgumentException("Invalid token");
                    }
                }

                return new Token {
                    Kind = Kind,
                    Value = json.Substring(start, i - start),
                    Length = i - pos,
                };
            }

            public Token Next() {
                var result = Peek();
                pos += result.Length;
                return result;
            }

            public bool Expect(TokenKind kind, TokenKind alt) {
                var token = Next();
                if (token.Kind == kind) {
                    return true;
                } else if (token.Kind == alt) {
                    return false;
                } else {
                    throw new ArgumentException($"Unexpected token");
                }
            }

            public Token Expect(TokenKind kind) {
                var token = Next();
                if (token.Kind == kind) {
                    return token;
                } else {
                    throw new ArgumentException("Unexpected token");
                }
            }

            public bool Follows(TokenKind kind) {
                var token = Peek();
                if (token.Kind == kind) {
                    pos += token.Length;
                    return true;
                } else {
                    return false;
                }
            }
        }

        public static void Skip(Tokenizer it) {
            int depth = 0;

            do {
                switch (it.Next().Kind) {
                    case TokenKind.Eof: {
                        throw new ArgumentException("Expected token");
                    }
                    case TokenKind.LBrack:
                    case TokenKind.LBrace: {
                        ++depth;
                        break;
                    }
                    case TokenKind.RBrack:
                    case TokenKind.RBrace: {
                        if (--depth < 0) {
                            throw new ArgumentException("Unexpected R-brack/brace");
                        }

                        break;
                    }
                }
            } while (depth > 0);
        }

        public static object Parse(Tokenizer it, Type type) {
            if (type == typeof(string)) {
                return it.Expect(TokenKind.String).Value;
            } else if (type == typeof(bool)) {
                var token = it.Next();
                if (token.Kind == TokenKind.True) {
                    return true;
                } else if (token.Kind == TokenKind.False) {
                    return false;
                } else {
                    throw new ArgumentException("Expected boolean");
                }
            } else if (type.IsPrimitive) {
                return Convert.ChangeType(it.Expect(TokenKind.Int).Value, type);
            } else if (type.IsArray) {
                it.Expect(TokenKind.LBrack);

                var elementType = type.GetElementType();
                var list = new List<object>();

                if (!it.Follows(TokenKind.RBrack)) {
                    do {
                        list.Add(Parse(it, elementType));
                    } while (it.Expect(TokenKind.Comma, TokenKind.RBrack));
                }

                var result = Array.CreateInstance(elementType, list.Count);
                for (int i = 0; i < result.Length; ++i) {
                    result.SetValue(list[i], i);
                }

                return result;
            } else {
                it.Expect(TokenKind.LBrace);

                var result = type.GetConstructor(Array.Empty<Type>()).Invoke(Array.Empty<object>());

                if (!it.Follows(TokenKind.RBrace)) {
                    do {
                        var key = it.Expect(TokenKind.String).Value;
                        it.Expect(TokenKind.Colon);
                        var field = type.GetField(key);
                        if (field == null) {
                            Skip(it);
                        } else {
                            field.SetValue(result, Parse(it, field.FieldType));
                        }
                    } while (it.Expect(TokenKind.Comma, TokenKind.RBrace));
                }

                return result;
            }
        }

        public static object Parse(string json, Type type) {
            var it = new Tokenizer(json);

            var result = Parse(it, type);

            it.Expect(TokenKind.Eof);

            return result;
        }

        public static T Parse<T>(Tokenizer it) {
            return (T)Parse(it, typeof(T));
        }

        public static T Parse<T>(string json) {
            return (T)Parse(json, typeof(T));
        }
    }
}
