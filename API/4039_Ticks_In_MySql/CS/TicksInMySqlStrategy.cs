using System;
using System.Collections.Generic;

using MySql.Data.MySqlClient;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that records level 1 quotes and account metrics into a MySQL table.
/// Mirrors the legacy MQL TicksInMySQL script by storing margin, free margin, symbol, and equity per tick.
/// </summary>
public class TicksInMySqlStrategy : Strategy
{
	private readonly StrategyParam<string> _server;
	private readonly StrategyParam<int> _port;
	private readonly StrategyParam<string> _database;
	private readonly StrategyParam<string> _user;
	private readonly StrategyParam<string> _password;
	private readonly StrategyParam<string> _tableName;
	private readonly StrategyParam<bool> _autoCreateTable;
	private readonly StrategyParam<int> _pricePrecision;

	private readonly object _syncRoot = new();

	private MySqlConnection? _connection;
	private MySqlCommand? _insertCommand;
	private decimal? _lastBid;
	private decimal? _lastAsk;

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public TicksInMySqlStrategy()
	{
		_server = Param(nameof(Server), "localhost")
			.SetDisplay("Server", "MySQL server host", "Connection");

		_port = Param(nameof(Port), 3306)
			.SetGreaterThanZero()
			.SetDisplay("Port", "MySQL server port", "Connection");

		_database = Param(nameof(Database), "mt4")
			.SetDisplay("Database", "Database name", "Connection");

		_user = Param(nameof(User), "user")
			.SetDisplay("User", "MySQL login", "Connection");

		_password = Param(nameof(Password), "pwd")
			.SetDisplay("Password", "MySQL password", "Connection");

		_tableName = Param(nameof(TableName), "ticks")
			.SetDisplay("Table", "Destination table name", "Connection");

		_autoCreateTable = Param(nameof(AutoCreateTable), true)
			.SetDisplay("Auto Create Table", "Create table when it does not exist", "Connection");

		_pricePrecision = Param(nameof(PricePrecision), 4)
			.SetNotNegative()
			.SetDisplay("Price Precision", "Number of decimal places for bid/ask", "Formatting");
	}

	/// <summary>
	/// MySQL server host.
	/// </summary>
	public string Server
	{
		get => _server.Value;
		set => _server.Value = value;
	}

	/// <summary>
	/// MySQL server port.
	/// </summary>
	public int Port
	{
		get => _port.Value;
		set => _port.Value = value;
	}

	/// <summary>
	/// Database name that will store tick rows.
	/// </summary>
	public string Database
	{
		get => _database.Value;
		set => _database.Value = value;
	}

	/// <summary>
	/// User name used for authentication.
	/// </summary>
	public string User
	{
		get => _user.Value;
		set => _user.Value = value;
	}

	/// <summary>
	/// Password used for authentication.
	/// </summary>
	public string Password
	{
		get => _password.Value;
		set => _password.Value = value;
	}

	/// <summary>
	/// Destination table name inside the database.
	/// </summary>
	public string TableName
	{
		get => _tableName.Value;
		set => _tableName.Value = value;
	}

	/// <summary>
	/// Automatically create the table if it is missing.
	/// </summary>
	public bool AutoCreateTable
	{
		get => _autoCreateTable.Value;
		set => _autoCreateTable.Value = value;
	}

	/// <summary>
	/// Number of decimal places for bid and ask values.
	/// </summary>
	public int PricePrecision
	{
		get => _pricePrecision.Value;
		set => _pricePrecision.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, DataType.Level1);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		CloseConnection();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		try
		{
			OpenConnection();
		}
		catch (Exception ex)
		{
			LogError($"Failed to open MySQL connection: {ex.Message}");
			Stop();
			return;
		}

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnStopped(DateTimeOffset time)
	{
		CloseConnection();
		base.OnStopped(time);
	}

	private void OpenConnection()
	{
		var builder = new MySqlConnectionStringBuilder
		{
			Server = Server,
			Port = (uint)Port,
			UserID = User,
			Password = Password,
			Database = Database,
			SslMode = MySqlSslMode.None
		};

		_connection = new MySqlConnection(builder.ConnectionString);
		_connection.Open();

		LogInfo($"Connected to MySQL server {Server}:{Port}, database {Database}.");

		if (AutoCreateTable)
			CreateTable();

		PrepareInsertCommand();
	}

	private void CreateTable()
	{
		if (_connection == null)
			throw new InvalidOperationException("Connection is not initialized.");

		var sql = $@"CREATE TABLE IF NOT EXISTS `{TableName}` (
			`id` BIGINT NOT NULL AUTO_INCREMENT,
			`margin` DECIMAL(19,6) NOT NULL,
			`freemargin` DECIMAL(19,6) NOT NULL,
			`date` DATETIME NOT NULL,
			`ask` DECIMAL(19,6) NOT NULL,
			`bid` DECIMAL(19,6) NOT NULL,
			`symbol` VARCHAR(64) NOT NULL,
			`equity` DECIMAL(19,6) NOT NULL,
			PRIMARY KEY (`id`)
		) ENGINE=InnoDB;";

		using var command = new MySqlCommand(sql, _connection);
		command.ExecuteNonQuery();
	}

	private void PrepareInsertCommand()
	{
		if (_connection == null)
			throw new InvalidOperationException("Connection is not initialized.");

		_insertCommand?.Dispose();

		var sql = $"INSERT INTO `{TableName}` (margin, freemargin, date, ask, bid, symbol, equity) VALUES (@margin, @freeMargin, @date, @ask, @bid, @symbol, @equity);";
		_insertCommand = new MySqlCommand(sql, _connection);
		_insertCommand.Parameters.Add("@margin", MySqlDbType.Decimal);
		_insertCommand.Parameters.Add("@freeMargin", MySqlDbType.Decimal);
		_insertCommand.Parameters.Add("@date", MySqlDbType.DateTime);
		_insertCommand.Parameters.Add("@ask", MySqlDbType.Decimal);
		_insertCommand.Parameters.Add("@bid", MySqlDbType.Decimal);
		_insertCommand.Parameters.Add("@symbol", MySqlDbType.VarChar, 128);
		_insertCommand.Parameters.Add("@equity", MySqlDbType.Decimal);
	}

	private void CloseConnection()
	{
		lock (_syncRoot)
		{
			_insertCommand?.Dispose();
			_insertCommand = null;

			_connection?.Dispose();
			_connection = null;
		}

		_lastBid = null;
		_lastAsk = null;
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue))
			_lastBid = (decimal)bidValue;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue))
			_lastAsk = (decimal)askValue;

		if (_insertCommand == null || _connection == null)
			return;

		if (_lastBid == null || _lastAsk == null)
			return;

		var margin = Portfolio?.BlockedValue ?? 0m;
		var equity = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		var freeMargin = equity - margin;
		var symbol = Security?.Id ?? string.Empty;

		var timestamp = level1.ServerTime != default ? level1.ServerTime.UtcDateTime : CurrentTime.UtcDateTime;

		lock (_syncRoot)
		{
			if (_insertCommand == null)
				return;

			_insertCommand.Parameters["@margin"].Value = margin;
			_insertCommand.Parameters["@freeMargin"].Value = freeMargin;
			_insertCommand.Parameters["@date"].Value = timestamp;
			_insertCommand.Parameters["@ask"].Value = Math.Round(_lastAsk.Value, PricePrecision, MidpointRounding.AwayFromZero);
			_insertCommand.Parameters["@bid"].Value = Math.Round(_lastBid.Value, PricePrecision, MidpointRounding.AwayFromZero);
			_insertCommand.Parameters["@symbol"].Value = symbol;
			_insertCommand.Parameters["@equity"].Value = equity;

			try
			{
				_insertCommand.ExecuteNonQuery();
			}
			catch (Exception ex)
			{
				LogError($"Failed to insert tick into MySQL: {ex.Message}");
			}
		}
	}
}
