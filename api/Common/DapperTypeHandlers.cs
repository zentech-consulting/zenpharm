using System.Data;
using Dapper;

namespace Api.Common;

internal sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }

    public override DateOnly Parse(object value) => DateOnly.FromDateTime((DateTime)value);
}

internal sealed class NullableDateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly?>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly? value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.HasValue ? value.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
    }

    public override DateOnly? Parse(object value) =>
        value is DateTime dt ? DateOnly.FromDateTime(dt) : null;
}

internal sealed class TimeOnlyTypeHandler : SqlMapper.TypeHandler<TimeOnly>
{
    public override void SetValue(IDbDataParameter parameter, TimeOnly value)
    {
        parameter.DbType = DbType.Time;
        parameter.Value = value.ToTimeSpan();
    }

    public override TimeOnly Parse(object value) => TimeOnly.FromTimeSpan((TimeSpan)value);
}

internal sealed class NullableTimeOnlyTypeHandler : SqlMapper.TypeHandler<TimeOnly?>
{
    public override void SetValue(IDbDataParameter parameter, TimeOnly? value)
    {
        parameter.DbType = DbType.Time;
        parameter.Value = value.HasValue ? value.Value.ToTimeSpan() : DBNull.Value;
    }

    public override TimeOnly? Parse(object value) =>
        value is TimeSpan ts ? TimeOnly.FromTimeSpan(ts) : null;
}
