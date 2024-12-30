using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

class Base26Calculator
{
    // Convertit un nombre en base 26 en BigInteger
    public static BigInteger Base26ToBigInteger(string base26)
    {
        if (string.IsNullOrEmpty(base26))
            throw new ArgumentException("Le nombre en base 26 ne peut pas être vide.");

        BigInteger result = 0;
        foreach (char c in base26)
        {
            if (c >= '0' && c <= '9')
            {
                result = (result * 26) + (c - '0');
            }
            else if (c >= 'A' && c <= 'Z')
            {
                result = (result * 26) + (c - 'A' + 10);
            }
            else
            {
                throw new ArgumentException($"Caractère invalide dans le nombre en base 26 : {c}");
            }
        }
        return result;
    }

    // Convertit un BigInteger en nombre en base 26
    public static string BigIntegerToBase26(BigInteger number)
    {
        if (number < 0)
            throw new ArgumentException("Le nombre ne peut pas être négatif.");

        if (number == 0)
            return "0";

        var result = new StringBuilder();
        while (number > 0)
        {
            int remainder = (int)(number % 26);
            char digit = (remainder < 10) ? (char)('0' + remainder) : (char)('A' + remainder - 10);
            result.Insert(0, digit);
            number /= 26;
        }
        return result.ToString();
    }

    // Convertit un BigInteger en une base donnée (2, 10, 16, etc.)
    public static string ConvertToBase(BigInteger number, int baseValue)
    {
        if (baseValue < 2 || baseValue > 36)
            throw new ArgumentException("La base doit être comprise entre 2 et 36.");

        if (number == 0)
            return "0";

        var result = new StringBuilder();
        while (number > 0)
        {
            int remainder = (int)(number % baseValue);
            char digit = (remainder < 10) ? (char)('0' + remainder) : (char)('A' + remainder - 10);
            result.Insert(0, digit);
            number /= baseValue;
        }
        return result.ToString();
    }

    // Nœud de l'arbre d'expression
    public abstract class ExpressionNode
    {
        public abstract BigInteger Evaluate();
        public abstract void PrintTree(string indent = "", bool last = true);
    }

    // Nœud pour les valeurs en base 26
    private class ValueNode : ExpressionNode
    {
        private readonly BigInteger _value;
        private readonly string _representation;

        public ValueNode(string value)
        {
            _representation = value;
            _value = Base26ToBigInteger(value);
        }

        public override BigInteger Evaluate()
        {
            return _value;
        }

        public override void PrintTree(string indent = "", bool last = true)
        {
            Console.WriteLine(indent + "+-- " + _representation);
        }
    }

    // Nœud pour les opérations binaires
    private class BinaryOperationNode : ExpressionNode
    {
        private readonly char _operator;
        private readonly ExpressionNode _left;
        private readonly ExpressionNode _right;

        public BinaryOperationNode(char op, ExpressionNode left, ExpressionNode right)
        {
            _operator = op;
            _left = left;
            _right = right;
        }

        public override BigInteger Evaluate()
        {
            BigInteger leftValue = _left.Evaluate();
            BigInteger rightValue = _right.Evaluate();

            switch (_operator)
            {
                case '+': return leftValue + rightValue;
                case '-': return leftValue - rightValue;
                case '*': return leftValue * rightValue;
                case '/': return rightValue == 0 ? throw new DivideByZeroException("Division par zéro.") : leftValue / rightValue;
                case '^': return BigInteger.Pow(leftValue, (int)rightValue);
                case '%': return leftValue % rightValue;
                default: throw new InvalidOperationException($"Opérateur inconnu : {_operator}");
            }
        }

        public override void PrintTree(string indent = "", bool last = true)
        {
            Console.WriteLine(indent + "+-- " + _operator);
            _left.PrintTree(indent + (last ? "    " : "|   "), false);
            _right.PrintTree(indent + (last ? "    " : "|   "), true);
        }
    }

    // Parse une expression et construit l'arbre des calculs
    public static ExpressionNode ParseExpression(string expression)
    {
        var tokens = Tokenize(expression);
        var postfix = ConvertToPostfix(tokens);
        return BuildExpressionTree(postfix);
    }

    // Tokenize l'expression en nombres et opérateurs
    private static List<string> Tokenize(string expression)
    {
        var tokens = new List<string>();
        var currentToken = new StringBuilder();

        foreach (char c in expression)
        {
            if (c == ' ')
                continue;

            if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z'))
            {
                currentToken.Append(c);
            }
            else
            {
                if (currentToken.Length > 0)
                {
                    tokens.Add(currentToken.ToString());
                    currentToken.Clear();
                }
                tokens.Add(c.ToString());
            }
        }

        if (currentToken.Length > 0)
        {
            tokens.Add(currentToken.ToString());
        }

        return tokens;
    }

    // Convertit les tokens en notation postfixée (RPN)
    private static List<string> ConvertToPostfix(List<string> tokens)
    {
        var output = new List<string>();
        var operators = new Stack<string>();

        foreach (var token in tokens)
        {
            if (token.All(c => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z')))
            {
                output.Add(token);
            }
            else if (token == "(")
            {
                operators.Push(token);
            }
            else if (token == ")")
            {
                while (operators.Count > 0 && operators.Peek() != "(")
                {
                    output.Add(operators.Pop());
                }
                if (operators.Count == 0 || operators.Peek() != "(")
                    throw new ArgumentException("Parenthèses mal équilibrées.");
                operators.Pop();
            }
            else
            {
                while (operators.Count > 0 && Precedence(operators.Peek()) >= Precedence(token))
                {
                    output.Add(operators.Pop());
                }
                operators.Push(token);
            }
        }

        while (operators.Count > 0)
        {
            if (operators.Peek() == "(" || operators.Peek() == ")")
                throw new ArgumentException("Parenthèses mal équilibrées.");
            output.Add(operators.Pop());
        }

        return output;
    }

    // Construit l'arbre d'expression à partir de la notation postfixée
    private static ExpressionNode BuildExpressionTree(List<string> postfix)
    {
        var stack = new Stack<ExpressionNode>();

        foreach (var token in postfix)
        {
            if (token.All(c => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z')))
            {
                stack.Push(new ValueNode(token));
            }
            else
            {
                if (stack.Count < 2)
                    throw new ArgumentException("Expression invalide : opérateur sans opérandes suffisants.");

                var right = stack.Pop();
                var left = stack.Pop();
                stack.Push(new BinaryOperationNode(token[0], left, right));
            }
        }

        if (stack.Count != 1)
            throw new ArgumentException("Expression invalide : trop d'opérandes ou d'opérateurs.");

        return stack.Pop();
    }

    // Retourne la précédence des opérateurs
    private static int Precedence(string op)
    {
        switch (op)
        {
            case "+":
            case "-":
                return 1;
            case "*":
            case "/":
            case "%":
                return 2;
            case "^":
                return 3;
            default:
                return 0;
        }
    }

    // Point d'entrée du programme
    static void Main(string[] args)
    {
        Console.WriteLine("Calculatrice en base 26");
        Console.WriteLine("Opérations supportées : +, -, *, /, ^, %");
        Console.WriteLine("Exemple d'expression : 145B + 1524 * 154FE / 24GF - 4245EAC * 14DC - 19735ABCDEF * BC - AB");

        while (true)
        {
            Console.WriteLine("\nEntrez une expression en base 26 ou 'exit' pour quitter :");
            string expression = Console.ReadLine()?.ToUpper();

            if (expression == "EXIT")
                break;

            try
            {
                var expressionTree = ParseExpression(expression);
                Console.WriteLine("\nArbre des calculs :");
                expressionTree.PrintTree();

                BigInteger result = expressionTree.Evaluate();
                Console.WriteLine($"\nLe résultat en base 26 est: {BigIntegerToBase26(result)}");
                Console.WriteLine($"Le résultat en base 10 est: {result}");
                Console.WriteLine($"Le résultat en binaire est: {ConvertToBase(result, 2)}");
                Console.WriteLine($"Le résultat en hexadécimal est: {ConvertToBase(result, 16)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur: {ex.Message}");
            }
        }
    }
}