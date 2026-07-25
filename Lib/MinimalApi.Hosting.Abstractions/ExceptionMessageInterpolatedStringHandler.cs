using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace MinimalApi.Hosting
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
    public readonly ref struct ExceptionMessageInterpolatedStringHandler(int literalLength, int formattedCount)
    {
        private readonly StringBuilder _template = new(literalLength + 10 * formattedCount);
        private readonly StringBuilder _message = new(literalLength + 20 * formattedCount);
        private readonly List<object?> _arguments = new(formattedCount);

        /// <summary>
        /// Список аргументов для структурированного лога. 
        /// </summary>
        internal IReadOnlyList<object?> Arguments => _arguments;

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
            var argName = ToUpperCamelCase(name);
            var logFormat = CreateFormat(alignment, format, argName);
            var strFormat = CreateFormat(alignment, format, "0");

            _arguments.Add(value);
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
        private static string ToUpperCamelCase(string name)
        {
            var chars = name.ToCharArray();
            var char0 = chars[0];
            chars[0] = char.ToUpperInvariant(char0);
            return new(chars);
        }
    }
}