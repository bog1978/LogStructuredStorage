using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Storage.Api.Internal
{
    /// <summary>
    /// Обработчик интерполированной строки. Делает 3 вещи:<br/>
    /// 1. собирает итоговую строку сообщения для исключения;<br/>
    /// 2. собирает шаблон для структурного лога;<br/>
    /// 3. собирает список аргументов для структурного лога.
    /// При этом имена плейсхолдеров в структурных логах будут
    /// совпадать с именами переменных, значения которых подставляются.
    /// </summary>
    [InterpolatedStringHandler]
    public readonly ref struct ActivityInterpolatedStringHandler(int literalLength, int formattedCount)
    {
        private readonly StringBuilder _template = new(literalLength + 10 * formattedCount);
        private readonly StringBuilder _message = new(literalLength + 20 * formattedCount);
        private readonly Dictionary<string, object?> _arguments = new(formattedCount);

        /// <summary>
        /// Список аргументов для структурированного лога. 
        /// </summary>
        internal IReadOnlyDictionary<string, object?> Arguments => _arguments;

        /// <summary>
        /// Формат сообщения для структурированного лога. 
        /// </summary>
        internal string Template => _template.ToString();

        /// <summary>
        /// Готовое отформатированное сообщение исключения.
        /// </summary>
        internal string Message => _message.ToString();
        
        /// <summary>
        /// Добавляет литеральную часть строки.
        /// </summary>
        public void AppendLiteral(string s)
        {
            _template.Append(s.Replace("{", "{{").Replace("}", "}}"));
            _message.Append(s);
        }

        /// <summary>
        /// Добавляет интерполированное значение.
        /// </summary>
        public void AppendFormatted<T>(T value, int? alignment = null, string? format = null, [CallerArgumentExpression(nameof(value))] string name = "")
        {
            var (n, f) = ParseFormat(format);
            
            var argName = n ?? GetArgName(name);
            var logFormat = CreateFormat(alignment, f, argName);
            var strFormat = CreateFormat(alignment, f, "0");

            _arguments.Add(argName, value);
            _template.Append(logFormat);
            _message.AppendFormat(CultureInfo.InvariantCulture, strFormat, value);
        }

        /// <summary>
        /// Создаёт строку формата для одного интерполированного значения.
        /// </summary>
        private static string CreateFormat(int? alignment, string? format, string fixedName)
        {
            var formatBuilder = new StringBuilder(32);
            formatBuilder.Append('{').Append(fixedName);
            if (alignment.HasValue)
                formatBuilder.Append(',').Append(alignment.Value);
            if (format != null)
                formatBuilder.Append(':').Append(format);
            formatBuilder.Append('}');
            return formatBuilder.ToString();
        }

        /// <summary>
        /// Преобразует имя параметра из lowerCamelCase в UpperCamelCase.
        /// </summary>
        private static string GetArgName(string name)
        {
            var chars = name
                .Split('.')
                .Last()
                .Trim('_')
                .ToCharArray();
            var char0 = chars[0];
            chars[0] = char.ToUpperInvariant(char0);
            return new(chars);
        }

        private static (string? name, string? format) ParseFormat(string? format)
        {
            if (format is not ['@', ..])
                return (null, format);
            
            var i = format.IndexOf(':');
            return i < 0
                ? (format[1..], null)
                : (format[1..i], format[(i + 1)..]);
        }
    }
}