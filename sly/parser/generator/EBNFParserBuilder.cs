using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using sly.buildresult;
using sly.i18n;
using sly.lexer.fsm;
using sly.parser.generator.visitor;
using sly.parser.llparser;
using sly.parser.syntax.grammar;

namespace sly.parser.generator
{
    /// <summary>
    ///     this class provides API to build parser
    /// </summary>
    internal class EBNFParserBuilder<IN, OUT> : ParserBuilder<IN, OUT> where IN : struct
    {
        
        public EBNFParserBuilder(string i18n = null) : base(i18n)
        {
        }
        
        public override BuildResult<Parser<IN, OUT>> BuildParser(object parserInstance, ParserType parserType,
            string rootRule, BuildExtension<IN> extensionBuilder = null)
        {
            var ruleparser = new RuleParser<IN,OUT>();
            var builder = new ParserBuilder<EbnfTokenGeneric, GrammarNode<IN,OUT>>(I18n);

            var grammarParser = builder.BuildParser(ruleparser, ParserType.LL_RECURSIVE_DESCENT, "rule").Result;


            var result = new BuildResult<Parser<IN, OUT>>();

            ParserConfiguration<IN, OUT> configuration = null;

            try
            {
                configuration = ExtractEbnfParserConfiguration(parserInstance, grammarParser);
                configuration.ParserInstance = parserInstance;
                LeftRecursionChecker<IN,OUT> recursionChecker = new LeftRecursionChecker<IN,OUT>();
                var (foundRecursion, recursions) = LeftRecursionChecker<IN,OUT>.CheckLeftRecursion(configuration);
                if (foundRecursion)
                {
                    var recs = string.Join("\n", recursions.Select(x => string.Join(" > ",x)));
                    result.AddError(new ParserInitializationError(ErrorLevel.FATAL,
                        I18N.Instance.GetText(I18n,Message.LeftRecursion,recs),
                        ErrorCodes.PARSER_LEFT_RECURSIVE));
                    return result;

                }
                
                configuration.StartingRule = rootRule;
            }
            catch (Exception e)
            {
                result.AddError(new ParserInitializationError(ErrorLevel.ERROR, e.Message,ErrorCodes.PARSER_UNKNOWN_ERROR));
                return result;
            }

            var syntaxParser = BuildSyntaxParser(configuration, parserType, rootRule);

            SyntaxTreeVisitor<IN, OUT> visitor = null;
            if (parserType == ParserType.LL_RECURSIVE_DESCENT)
                new SyntaxTreeVisitor<IN, OUT>(configuration, parserInstance);
            else if (parserType == ParserType.EBNF_LL_RECURSIVE_DESCENT)
                visitor = new EBNFSyntaxTreeVisitor<IN, OUT>(configuration, parserInstance);
            var parser = new Parser<IN, OUT>(I18n,syntaxParser, visitor);
            parser.Configuration = configuration;
            var lexerResult = BuildLexer(extensionBuilder);
            if (lexerResult.IsError)
            {
                foreach (var lexerResultError in lexerResult.Errors)
                {
                    result.AddError(lexerResultError);
                }
                return result;
            }
            else
                parser.Lexer = lexerResult.Result;
            parser.Instance = parserInstance;
            result.Result = parser;
            return result;
        }


        protected override ISyntaxParser<IN, OUT> BuildSyntaxParser(ParserConfiguration<IN, OUT> conf,
            ParserType parserType,
            string rootRule)
        {
            ISyntaxParser<IN, OUT> parser = null;
            switch (parserType)
            {
                case ParserType.LL_RECURSIVE_DESCENT:
                {
                    parser = new RecursiveDescentSyntaxParser<IN, OUT>(conf, rootRule,I18n);
                    break;
                }
                case ParserType.EBNF_LL_RECURSIVE_DESCENT:
                {
                    parser = new EBNFRecursiveDescentSyntaxParser<IN, OUT>(conf, rootRule,I18n);
                    break;
                }
                default:
                {
                    parser = null;
                    break;
                }
            }

            return parser;
        }


        #region configuration

        protected virtual ParserConfiguration<IN, OUT> ExtractEbnfParserConfiguration(object parserInstance,
            Parser<EbnfTokenGeneric, GrammarNode<IN,OUT>> grammarParser)
        {
            var parserClass = parserInstance.GetType();
            var conf = new ParserConfiguration<IN, OUT>();
            conf.ParserInstance = parserInstance;
            var nonTerminals = new Dictionary<string, NonTerminal<IN,OUT>>();
            var methods = parserClass.GetMethods().ToList();
            methods = methods.Where(m =>
            {
                var attributes = m.GetCustomAttributes().ToList();
                var attr = attributes.Find(a => a.GetType() == typeof(ProductionAttribute));
                return attr != null;
            }).ToList();

            methods.ForEach(m =>
            {
                var attributes =
                    (ProductionAttribute[]) m.GetCustomAttributes(typeof(ProductionAttribute), true);

                foreach (var attr in attributes)
                {
                    var ruleString = attr.RuleString;
                    var parseResult = grammarParser.Parse(ruleString);
                    if (!parseResult.IsError)
                    {
                        var rule = (Rule<IN,OUT>) parseResult.Result;
                        rule.RuleString = ruleString;
                        rule.SetVisitor(m,conf.ParserInstance);
                        NonTerminal<IN,OUT> nonT = null;
                        if (!nonTerminals.ContainsKey(rule.NonTerminalName))
                            nonT = new NonTerminal<IN,OUT>(rule.NonTerminalName, new List<Rule<IN,OUT>>());
                        else
                            nonT = nonTerminals[rule.NonTerminalName];
                        nonT.Rules.Add(rule);
                        nonTerminals[rule.NonTerminalName] = nonT;
                    }
                    else
                    {
                        var message = parseResult
                            .Errors
                            .Select(e => e.ErrorMessage)
                            .Aggregate((e1, e2) => e1 + "\n" + e2);
                        message = $"rule error [{ruleString}] : {message}";
                        throw new ParserConfigurationException(message);
                    }
                }
            });

            conf.NonTerminals = nonTerminals;

            return conf;
        }

        #endregion
    }
}