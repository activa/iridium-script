using System;
using System.Collections.Generic;
using System.Linq;

namespace Iridium.Script;

internal class ExpressionCompiler(ExpressionParser _parser, ExpressionToken[] _tokens)
{
    private int _currentIndex = -1;
    private ExpressionToken? _currentToken;

    public Expression Compile()
    {
        CurrentIndex = 0;

        return Compile(multiple: true);
    }

    private ExpressionToken? CurrentToken => _currentToken ??= (CurrentIndex < _tokens.Length ? _tokens[CurrentIndex] : null);

    private int CurrentIndex
    {
        get => _currentIndex;
        set
        {
            _currentIndex = value; 
            _currentToken = null;
        }
    }

    private bool MoveNext()
    {
        CurrentIndex++;

        return CurrentIndex < _tokens.Length;
    }

    private Expression CompileStatement(int lastToken = Int32.MaxValue)
    {
        RPNExpression rpn = new RPNExpression(_parser.FunctionEvaluator);

        rpn.Start();

        ExpressionToken? firstToken = CurrentToken;
        ExpressionToken? lastConsumedToken = null;

        while (CurrentToken != null && CurrentIndex <= lastToken)
        {
            if (CurrentToken.IsStatementSeperator)
            {
                MoveNext();
                break;
            }

            lastConsumedToken = CurrentToken;

            rpn.ApplyToken(CurrentToken);

            MoveNext();
        }

        rpn.Finish();

        Expression? expression = rpn.Compile();

        if (expression != null)
            SetSourceSpan(expression, firstToken, lastConsumedToken ?? firstToken);

        return expression;
    }

    /// <summary>
    /// Annotates an expression with the source region spanned by the given
    /// start and end tokens (unless it already carries a more specific span).
    /// </summary>
    private static void SetSourceSpan(Expression? expression, ExpressionToken? startToken, ExpressionToken endToken)
    {
        if (expression == null || startToken == null || expression.SourceSpan.IsKnown)
            return;

        expression.SourceSpan = SpanOf(startToken, endToken);
    }

    private static SourceSpan SpanOf(ExpressionToken startToken, ExpressionToken endToken)
    {
        if (startToken == null)
            return SourceSpan.Unknown;

        if (endToken == null)
            endToken = startToken;

        SourcePosition start = startToken.Position;
        SourcePosition endStart = endToken.Position;

        SourcePosition end = SourcePosition.Unknown;

        if (endStart.IsKnown)
        {
            int length = endToken.Text?.Length ?? 0;

            end = new SourcePosition(endStart.Index + length, endStart.Line, endStart.Column + length);
        }

        return new SourceSpan(start, end);
    }

    /// <summary>
    /// Builds a span starting at a keyword/statement token and extending to the
    /// end of a child expression (e.g. a loop or if body), so control-flow
    /// constructs report the full region they cover.
    /// </summary>
    private static SourceSpan SpanFromTokenToExpression(ExpressionToken startToken, Expression? endExpression)
    {
        SourceSpan keywordSpan = SpanOf(startToken, startToken);

        if (endExpression is { SourceSpan.IsKnown: true })
            return new SourceSpan(keywordSpan.Start, endExpression.SourceSpan.End);

        return keywordSpan;
    }

    private Expression CompileBracketed()
    {
        if (CurrentToken == null)
            throw new LexerException("Unexpected end of input", null, SourcePosition.Unknown);

        int level = 0;

        if (!CurrentToken.IsLeftParen)
            throw new LexerException("Expected (", CurrentToken.Text, CurrentToken.Position);

        SourcePosition openParenPosition = CurrentToken.Position;

        MoveNext();

        int start = CurrentIndex;

        while(CurrentToken != null)
        {
            if (CurrentToken.IsRightParen)
            {
                if (level > 0)
                {
                    level--;
                }
                else
                {
                    int idx = CurrentIndex-1;
                    CurrentIndex = start;

                    var expr = CompileStatement(idx);

                    MoveNext();

                    return expr;
                }
            }

            if (CurrentToken.IsLeftParen)
                level++;

            MoveNext();
        }

        throw new LexerException("Unterminated bracketed expression", null, openParenPosition);
    }

    private Expression Compile(bool multiple)
    {
        if (CurrentToken == null)
            throw new LexerException("Unexpected end of input", null, SourcePosition.Unknown);
        //return null;

        List<Expression> expressions = new List<Expression>();

        bool braced = CurrentToken.IsOpenBrace;

        if (braced)
        {
            MoveNext();
            multiple = true;
        }

        IfExpression? ifExpression = null;

        while (CurrentToken != null)
        {
            var token = CurrentToken;

            switch (token.TokenType)
            {
                case TokenType.CloseBrace:
                {
                    if (!braced)
                        throw new LexerException("Unexpected '" + token.Text + "'", token.Text, token.Position);

                    MoveNext();
                    multiple = false;

                    break;
                }
                            
                case TokenType.ForEach:
                {
                    MoveNext();

                    if (CompileBracketed() is not InExpression expression)
                        throw new LexerException("foreach syntax error", token.Text, token.Position);

                    ForEachExpression forEach = new ForEachExpression
                    {
                        Iterator = expression.Variable,
                        Expression = expression.Expression,
                        Body = Compile(false)
                    };

                    forEach.SourceSpan = SpanFromTokenToExpression(token, forEach.Body);

                    expressions.Add(forEach);

                    break;
                }

                case TokenType.While:
                {
                    MoveNext();

                    var conditionExpression = CompileBracketed();

                    var whileExpression = new WhileExpression
                    {
                        ConditionExpression = conditionExpression,
                        Body = Compile(false)
                    };

                    whileExpression.SourceSpan = SpanFromTokenToExpression(token, whileExpression.Body);

                    expressions.Add(whileExpression);

                    break;
                }


                case TokenType.If:
                {
                    MoveNext();

                    var expr = CompileBracketed();

                    ifExpression = new IfExpression(expr)
                    {
                        TrueExpression = Compile(false)
                    };

                    ifExpression.SourceSpan = SpanFromTokenToExpression(token, ifExpression.TrueExpression);

                    expressions.Add(ifExpression);

                    break;
                }

                case TokenType.Else:
                {
                    MoveNext();

                    if (ifExpression != null)
                    {
                        ifExpression.FalseExpression = Compile(false);
                        ifExpression = null;
                    }

                    break;
                }

                case TokenType.Return:
                {
                    MoveNext();

                    Expression expression = Compile(multiple: false);

                    ReturnExpression returnExpression = new ReturnExpression(expression);

                    returnExpression.SourceSpan = SpanFromTokenToExpression(token, expression);

                    expressions.Add(returnExpression);

                    break;
                }

                case TokenType.Break:
                {
                    MoveNext();

                    BreakLoopExpression breakExpression = new BreakLoopExpression();

                    breakExpression.SourceSpan = SpanOf(token, token);

                    expressions.Add(breakExpression);

                    break;

                }
                case TokenType.FunctionDefinition:
                {
                    MoveNext();

                    if (CurrentToken.TokenType != TokenType.Term)
                        throw new LexerException("function name expected", CurrentToken.Text, CurrentToken.Position);

                    var functionExpression = new FunctionDefinitionExpression
                    {
                        Name = CurrentToken.Text
                    };

                    MoveNext();

                    int level = 0;
                    int start = CurrentIndex-1;
                    int end = -1;

                    while (CurrentToken != null)
                    {
                        if (CurrentToken.IsLeftParen)
                            level++;
                        else if (CurrentToken.IsRightParen)
                        {
                            if (level > 0)
                            {
                                level--;

                                if (level == 0)
                                {
                                    end = CurrentIndex;
                                    break;
                                }
                            }
                        }

                        MoveNext();
                    }

                    CurrentIndex = start;
                                
                                
                    var parameters = (CallExpression) CompileStatement(end);

                    functionExpression.ParameterNames = (from p in parameters.Parameters select ((VariableExpression)p).VarName).ToArray();

                    functionExpression.Body = Compile(false);

                    functionExpression.SourceSpan = SpanFromTokenToExpression(token, functionExpression.Body);

                    expressions.Add(functionExpression);
                }
                    break;
                default:
                {
                    var exp = CompileStatement();

                    if (exp != null)
                        expressions.Add(exp);

                    break;
                }
            }

            if (!multiple)
                break;
        }

        if (expressions.Count > 1)
        {
            SequenceExpression sequence = new SequenceExpression(expressions.ToArray());

            SourceSpan first = expressions[0].SourceSpan;
            SourceSpan last = expressions[expressions.Count - 1].SourceSpan;

            if (first.IsKnown)
                sequence.SourceSpan = new SourceSpan(first.Start, last.IsKnown ? last.End : first.End);

            return sequence;
        }

        if (expressions.Count == 1)
            return expressions[0];

        throw new LexerException("Unexpected end of input", null, SourcePosition.Unknown);
    }

}