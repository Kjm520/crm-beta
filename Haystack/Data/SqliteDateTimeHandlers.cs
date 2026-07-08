using System.Data;
using System.Globalization;
using Dapper;

namespace Haystack.Data;

/// <summary>
/// Microsoft.Data.Sqlite stores dates as TEXT and, depending on the column
/// expression (notably aggregates like MAX(date)), returns them to Dapper as a
/// raw string rather than a DateTime. Dapper's default binder then fails with
/// "Unable to cast object of type 'System.String' to type 'DateTime'".
///
/// These handlers make the text -> DateTime conversion explicit and uniform for
/// every date column, so it no longer depends on SQLite column affinity.
/// Register both via Register() at startup.
/// </summary>
internal static class SqliteDateTimeHandlers
{
    public static void Register()
    {
        SqlMapper.AddTypeHandler(new DateTimeHandler());
        SqlMapper.AddTypeHandler(new NullableDateTimeHandler());
    }

    private static DateTime Parse(string s) =>
        DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.None);

    private sealed class DateTimeHandler : SqlMapper.TypeHandler<DateTime>
    {
        public override DateTime Parse(object value) =>
            value switch
            {
                DateTime dt => dt,
                string s    => SqliteDateTimeHandlers.Parse(s),
                _           => Convert.ToDateTime(value, CultureInfo.InvariantCulture)
            };

        public override void SetValue(IDbDataParameter parameter, DateTime value) =>
            parameter.Value = value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    private sealed class NullableDateTimeHandler : SqlMapper.TypeHandler<DateTime?>
    {
        public override DateTime? Parse(object value) =>
            value is null or DBNull
                ? null
                : value switch
                {
                    DateTime dt => dt,
                    string s    => SqliteDateTimeHandlers.Parse(s),
                    _           => Convert.ToDateTime(value, CultureInfo.InvariantCulture)
                };

        public override void SetValue(IDbDataParameter parameter, DateTime? value) =>
            parameter.Value = value.HasValue
                ? value.Value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                : DBNull.Value;
    }
}
