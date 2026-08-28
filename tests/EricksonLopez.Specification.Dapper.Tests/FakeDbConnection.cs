// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EricksonLopez.Specification.Dapper.Tests;

internal sealed class FakeDbConnection : DbConnection
{
    public FakeDbCommand? LastCommand { get; private set; }
    public List<Dictionary<string, object?>> DataReaderRows { get; set; } = [];
    public object? ScalarResult { get; set; }

    private ConnectionState _state = ConnectionState.Open;
    public override ConnectionState State => _state;
    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string ConnectionString { get; set; } = "FakeConnection";
    public override string Database => "FakeDb";
    public override string DataSource => "FakeSource";
    public override string ServerVersion => "1.0";

    protected override DbCommand CreateDbCommand()
    {
        LastCommand = new FakeDbCommand(this);
        return LastCommand;
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => new FakeDbTransaction(this);
    public override void ChangeDatabase(string databaseName) { }
    public override void Close() => _state = ConnectionState.Closed;
    public override void Open() => _state = ConnectionState.Open;
}

internal sealed class FakeDbTransaction : DbTransaction
{
    private readonly DbConnection _connection;
    public FakeDbTransaction(DbConnection connection) => _connection = connection;
    public override IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
    protected override DbConnection? DbConnection => _connection;
    public override void Commit() { }
    public override void Rollback() { }
}

internal sealed class FakeDbCommand : DbCommand
{
    private readonly FakeDbConnection _connection;
    private readonly FakeDbParameterCollection _parameters = new();

    public FakeDbCommand(FakeDbConnection connection) => _connection = connection;

    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string CommandText { get; set; } = string.Empty;
    public override int CommandTimeout { get; set; } = 30;
    public override CommandType CommandType { get; set; } = CommandType.Text;
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }
    protected override DbConnection? DbConnection { get => _connection; set { } }
    protected override DbParameterCollection DbParameterCollection => _parameters;
    protected override DbTransaction? DbTransaction { get; set; }

    public CancellationToken LastCancellationToken { get; private set; }

    public override void Cancel() { }
    protected override DbParameter CreateDbParameter() => new FakeDbParameter();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        => new FakeDbDataReader(_connection.DataReaderRows);

    protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
    {
        LastCancellationToken = cancellationToken;
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<DbDataReader>(cancellationToken);
        return Task.FromResult<DbDataReader>(new FakeDbDataReader(_connection.DataReaderRows));
    }

    public override int ExecuteNonQuery() => 0;
    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        LastCancellationToken = cancellationToken;
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<int>(cancellationToken);
        return Task.FromResult(0);
    }

    public override object? ExecuteScalar() => _connection.ScalarResult;
    public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
    {
        LastCancellationToken = cancellationToken;
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<object?>(cancellationToken);
        return Task.FromResult(_connection.ScalarResult);
    }

    public override void Prepare() { }
}

internal sealed class FakeDbParameter : DbParameter
{
    public override DbType DbType { get; set; }
    public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
    public override bool IsNullable { get; set; }
    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string ParameterName { get; set; } = string.Empty;
    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string SourceColumn { get; set; } = string.Empty;
    public override object? Value { get; set; }
    public override bool SourceColumnNullMapping { get; set; }
    public override int Size { get; set; }
    public override void ResetDbType() { }
}

internal sealed class FakeDbParameterCollection : DbParameterCollection
{
    private readonly List<DbParameter> _parameters = [];

    public override int Count => _parameters.Count;
    public override object SyncRoot => ((ICollection)_parameters).SyncRoot;

    public override int Add(object value)
    {
        _parameters.Add((DbParameter)value);
        return _parameters.Count - 1;
    }

    public override void AddRange(Array values)
    {
        foreach (var val in values)
            _parameters.Add((DbParameter)val);
    }

    public override void Clear() => _parameters.Clear();
    public override bool Contains(object value) => _parameters.Contains((DbParameter)value);
    public override bool Contains(string value) => _parameters.Any(p => p.ParameterName == value);
    public override void CopyTo(Array array, int index) => ((ICollection)_parameters).CopyTo(array, index);
    public override IEnumerator GetEnumerator() => _parameters.GetEnumerator();
    protected override DbParameter GetParameter(int index) => _parameters[index];
    protected override DbParameter GetParameter(string parameterName) => _parameters.First(p => p.ParameterName == parameterName);
    public override int IndexOf(object value) => _parameters.IndexOf((DbParameter)value);
    public override int IndexOf(string parameterName) => _parameters.FindIndex(p => p.ParameterName == parameterName);
    public override void Insert(int index, object value) => _parameters.Insert(index, (DbParameter)value);
    public override void Remove(object value) => _parameters.Remove((DbParameter)value);
    public override void RemoveAt(int index) => _parameters.RemoveAt(index);
    public override void RemoveAt(string parameterName) => _parameters.RemoveAll(p => p.ParameterName == parameterName);
    protected override void SetParameter(int index, DbParameter value) => _parameters[index] = value;
    protected override void SetParameter(string parameterName, DbParameter value)
    {
        var idx = IndexOf(parameterName);
        if (idx >= 0) _parameters[idx] = value;
        else _parameters.Add(value);
    }
}

internal sealed class FakeDbDataReader : DbDataReader
{
    private readonly List<Dictionary<string, object?>> _rows;
    private int _currentIndex = -1;
    private bool _isClosed;

    public FakeDbDataReader(List<Dictionary<string, object?>> rows) => _rows = rows;

    private Dictionary<string, object?> CurrentRow => _rows[_currentIndex];

    public override int FieldCount => _rows.Count > 0 ? _rows[0].Count : 0;
    public override bool HasRows => _rows.Count > 0;
    public override bool IsClosed => _isClosed;
    public override int RecordsAffected => 0;
    public override int Depth => 0;

    public override object this[int ordinal] => GetValue(ordinal);
    public override object this[string name] => CurrentRow[name] ?? DBNull.Value;

    public override bool Read()
    {
        if (_isClosed) return false;
        _currentIndex++;
        return _currentIndex < _rows.Count;
    }

    public override Task<bool> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(Read());

    public override bool NextResult() => false;
    public override Task<bool> NextResultAsync(CancellationToken cancellationToken) => Task.FromResult(false);

    public override void Close() => _isClosed = true;

    public override string GetName(int ordinal)
    {
        if (_rows.Count == 0) return string.Empty;
        return _rows[0].Keys.ElementAt(ordinal);
    }

    public override int GetOrdinal(string name)
    {
        if (_rows.Count == 0) return -1;
        return _rows[0].Keys.ToList().IndexOf(name);
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
    [UnconditionalSuppressMessage("Trimming", "IL2073", Justification = "Test fake returns runtime object type.")]
    public override Type GetFieldType(int ordinal)
    {
        if (_rows.Count > 0 && ordinal < _rows[0].Count)
        {
            var val = _rows[0].Values.ElementAt(ordinal);
            return val?.GetType() ?? typeof(object);
        }
        return typeof(object);
    }

    public override object GetValue(int ordinal)
    {
        if (_currentIndex >= 0 && _currentIndex < _rows.Count)
        {
            var name = GetName(ordinal);
            return CurrentRow[name] ?? DBNull.Value;
        }
        return DBNull.Value;
    }

    public override int GetValues(object[] values)
    {
        int count = Math.Min(values.Length, FieldCount);
        for (int i = 0; i < count; i++)
            values[i] = GetValue(i);
        return count;
    }

    public override bool IsDBNull(int ordinal) => GetValue(ordinal) is DBNull;
    public override bool GetBoolean(int ordinal) => Convert.ToBoolean(GetValue(ordinal));
    public override byte GetByte(int ordinal) => Convert.ToByte(GetValue(ordinal));
    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => 0;
    public override char GetChar(int ordinal) => Convert.ToChar(GetValue(ordinal));
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => 0;
    public override string GetDataTypeName(int ordinal) => GetFieldType(ordinal).Name;
    public override DateTime GetDateTime(int ordinal) => Convert.ToDateTime(GetValue(ordinal));
    public override decimal GetDecimal(int ordinal) => Convert.ToDecimal(GetValue(ordinal));
    public override double GetDouble(int ordinal) => Convert.ToDouble(GetValue(ordinal));
    public override IEnumerator GetEnumerator() => new DbEnumerator(this);
    public override float GetFloat(int ordinal) => Convert.ToSingle(GetValue(ordinal));
    public override Guid GetGuid(int ordinal) => (Guid)GetValue(ordinal);
    public override short GetInt16(int ordinal) => Convert.ToInt16(GetValue(ordinal));
    public override int GetInt32(int ordinal) => Convert.ToInt32(GetValue(ordinal));
    public override long GetInt64(int ordinal) => Convert.ToInt64(GetValue(ordinal));
    public override string GetString(int ordinal) => Convert.ToString(GetValue(ordinal)) ?? string.Empty;
}




