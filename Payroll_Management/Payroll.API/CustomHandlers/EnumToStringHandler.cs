namespace Payroll.API
{
    using Dapper;
    using System.Data;

    public class EnumToStringHandler<T> : SqlMapper.TypeHandler<T> where T : Enum
    {
        public override void SetValue(IDbDataParameter parameter, T? value)
        {
            parameter.Value = value?.ToString();
            parameter.DbType = DbType.String;
        }

        public override T Parse(object? value)
        {
            if (value == null || value == DBNull.Value)
                throw new DataException($"Cannot convert null to {typeof(T).Name}");

            return (T)Enum.Parse(typeof(T), value.ToString()!);
        }
    }
}
