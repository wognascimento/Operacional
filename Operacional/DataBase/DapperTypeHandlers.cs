using Dapper;
using System;
using System.Data;

namespace Operacional.DataBase;

public static class DapperTypeHandlers
{
    private static bool _registered;

    public static void Register()
    {
        if (_registered)
            return;

        SqlMapper.AddTypeHandler(new DateTimeHandler());
        SqlMapper.AddTypeHandler(new NullableDateTimeHandler());
        SqlMapper.AddTypeHandler(new TimeSpanHandler());
        SqlMapper.AddTypeHandler(new NullableTimeSpanHandler());
        _registered = true;
    }

    private sealed class DateTimeHandler : SqlMapper.TypeHandler<DateTime>
    {
        public override DateTime Parse(object value) => value switch
        {
            DateOnly date => date.ToDateTime(TimeOnly.MinValue),
            DateTime dateTime => dateTime,
            _ => Convert.ToDateTime(value)
        };

        public override void SetValue(IDbDataParameter parameter, DateTime value)
        {
            parameter.Value = value;
        }
    }

    private sealed class NullableDateTimeHandler : SqlMapper.TypeHandler<DateTime?>
    {
        public override DateTime? Parse(object value) => value switch
        {
            null => null,
            DBNull => null,
            DateOnly date => date.ToDateTime(TimeOnly.MinValue),
            DateTime dateTime => dateTime,
            _ => Convert.ToDateTime(value)
        };

        public override void SetValue(IDbDataParameter parameter, DateTime? value)
        {
            parameter.Value = value ?? (object)DBNull.Value;
        }
    }

    private sealed class TimeSpanHandler : SqlMapper.TypeHandler<TimeSpan>
    {
        public override TimeSpan Parse(object value) => value switch
        {
            TimeOnly time => time.ToTimeSpan(),
            TimeSpan timeSpan => timeSpan,
            DateTime dateTime => dateTime.TimeOfDay,
            _ => TimeSpan.Parse(value.ToString() ?? "00:00:00")
        };

        public override void SetValue(IDbDataParameter parameter, TimeSpan value)
        {
            parameter.Value = value;
        }
    }

    private sealed class NullableTimeSpanHandler : SqlMapper.TypeHandler<TimeSpan?>
    {
        public override TimeSpan? Parse(object value) => value switch
        {
            null => null,
            DBNull => null,
            TimeOnly time => time.ToTimeSpan(),
            TimeSpan timeSpan => timeSpan,
            DateTime dateTime => dateTime.TimeOfDay,
            _ => TimeSpan.Parse(value.ToString() ?? "00:00:00")
        };

        public override void SetValue(IDbDataParameter parameter, TimeSpan? value)
        {
            parameter.Value = value ?? (object)DBNull.Value;
        }
    }
}
