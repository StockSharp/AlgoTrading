using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that mirrors aggregate positions from a master account into a slave account.
/// The original MQL script copies two specific symbols by writing position snapshots into a shared binary file.
/// The StockSharp version keeps the same two-pair mapping idea and synchronizes the slave exposure with the master exposure.
/// </summary>
public class AllanmaugTradeCopierStrategy : Strategy
{
	private readonly StrategyParam<TradeCopierMode> _mode;
	private readonly StrategyParam<string> _masterSymbol1;
	private readonly StrategyParam<string> _slaveSymbol1;
	private readonly StrategyParam<string> _masterSymbol2;
	private readonly StrategyParam<string> _slaveSymbol2;
	private readonly StrategyParam<string> _fileName;
	private readonly StrategyParam<TimeSpan> _interval;
	private readonly StrategyParam<decimal> _volumeTolerance;

	private readonly Dictionary<string, Security?> _securityCache = new(StringComparer.OrdinalIgnoreCase);
	private Timer? _timer;
	private string _filePath = string.Empty;

	/// <summary>
	/// Initializes a new instance of the <see cref="AllanmaugTradeCopierStrategy"/> class.
	/// </summary>
	public AllanmaugTradeCopierStrategy()
	{
		_mode = Param(nameof(Mode), TradeCopierMode.Slave)
		.SetDisplay("Mode", "Master writes positions, slave synchronizes exposure", "General");

		_masterSymbol1 = Param(nameof(MasterSymbol1), "XAUUSD.ecn")
		.SetDisplay("Master Symbol 1", "Identifier of the first master symbol", "Symbols");

		_slaveSymbol1 = Param(nameof(SlaveSymbol1), "GOLD")
		.SetDisplay("Slave Symbol 1", "Identifier of the first slave symbol", "Symbols");

		_masterSymbol2 = Param(nameof(MasterSymbol2), "USDJPY.ecn")
		.SetDisplay("Master Symbol 2", "Identifier of the second master symbol", "Symbols");

		_slaveSymbol2 = Param(nameof(SlaveSymbol2), "USDJPY")
		.SetDisplay("Slave Symbol 2", "Identifier of the second slave symbol", "Symbols");

		_fileName = Param(nameof(FileName), "allanmaug_tradecopier.bin")
		.SetDisplay("File Name", "Binary file that shares snapshots between master and slave", "Files");

		_interval = Param(nameof(Interval), TimeSpan.FromMilliseconds(200))
		.SetDisplay("Interval", "How often the strategy reads or writes the file", "Timing");

		_volumeTolerance = Param(nameof(VolumeTolerance), 0.0001m)
		.SetNotNegative()
		.SetDisplay("Volume Tolerance", "Minimum difference required before rebalancing", "Trading");
	}

	/// <summary>
	/// Working mode of the trade copier.
	/// </summary>
	public TradeCopierMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Identifier of the first master symbol.
	/// </summary>
	public string MasterSymbol1
	{
		get => _masterSymbol1.Value;
		set => _masterSymbol1.Value = value;
	}

	/// <summary>
	/// Identifier of the first slave symbol.
	/// </summary>
	public string SlaveSymbol1
	{
		get => _slaveSymbol1.Value;
		set => _slaveSymbol1.Value = value;
	}

	/// <summary>
	/// Identifier of the second master symbol.
	/// </summary>
	public string MasterSymbol2
	{
		get => _masterSymbol2.Value;
		set => _masterSymbol2.Value = value;
	}

	/// <summary>
	/// Identifier of the second slave symbol.
	/// </summary>
	public string SlaveSymbol2
	{
		get => _slaveSymbol2.Value;
		set => _slaveSymbol2.Value = value;
	}

	/// <summary>
	/// Binary file used to exchange position data.
	/// </summary>
	public string FileName
	{
		get => _fileName.Value;
		set => _fileName.Value = value;
	}

	/// <summary>
	/// Timer interval used to poll the file.
	/// </summary>
	public TimeSpan Interval
	{
		get => _interval.Value;
		set => _interval.Value = value;
	}

	/// <summary>
	/// Minimum volume difference before sending synchronizing orders.
	/// </summary>
	public decimal VolumeTolerance
	{
		get => _volumeTolerance.Value;
		set => _volumeTolerance.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_filePath = Path.Combine(Directory.GetCurrentDirectory(), FileName);
		_timer = new Timer(_ => ProcessCopyCycle(), null, TimeSpan.Zero, Interval);
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		var timer = _timer;
		if (timer != null)
		{
			_timer = null;
			timer.Dispose();
		}

		base.OnStopped();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_securityCache.Clear();
	}

	private void ProcessCopyCycle()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		try
		{
			if (Mode == TradeCopierMode.Master)
			ProcessMaster();
			else
			ProcessSlave();
		}
		catch (Exception ex)
		{
			this.LogWarning($"Trade copier cycle failed: {ex.Message}");
		}
	}

	private void ProcessMaster()
	{
		var snapshot = new List<CopierEntry>();

		foreach (var mapping in EnumerateMappings())
		{
			if (string.IsNullOrWhiteSpace(mapping.Master) || string.IsNullOrWhiteSpace(mapping.Slave))
			continue;

			var (volume, averagePrice) = GetAggregatePosition(mapping.Master);
			snapshot.Add(new CopierEntry
			{
				MasterSymbol = mapping.Master,
				SlaveSymbol = mapping.Slave,
				Volume = volume,
				AveragePrice = averagePrice
			});
		}

		WriteSnapshot(snapshot);
	}

	private void ProcessSlave()
	{
		var snapshot = ReadSnapshot();
		if (snapshot.Count == 0)
		return;

		foreach (var entry in snapshot)
		{
			if (string.IsNullOrWhiteSpace(entry.SlaveSymbol))
			continue;

			var slaveSecurity = LookupSecurity(entry.SlaveSymbol);
			if (slaveSecurity == null)
			{
				this.LogWarning($"Unknown slave security '{entry.SlaveSymbol}'.");
				continue;
			}

			var (currentVolume, _) = GetAggregatePosition(entry.SlaveSymbol);
			var targetVolume = entry.Volume;
			var difference = targetVolume - currentVolume;

			if (Math.Abs(difference) <= VolumeTolerance)
			continue;

			if (difference > 0m)
			{
				BuyMarket(difference, slaveSecurity);
				LogInfo($"Increase {slaveSecurity.Id} by {difference:0.####} lots to follow master {entry.MasterSymbol}.");
			}
			else
			{
				SellMarket(-difference, slaveSecurity);
				LogInfo($"Decrease {slaveSecurity.Id} by {-difference:0.####} lots to follow master {entry.MasterSymbol}.");
			}
		}
	}

	private void WriteSnapshot(List<CopierEntry> entries)
	{
		try
		{
			using var stream = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
			using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: false);
			writer.Write(entries.Count);
			foreach (var entry in entries)
			{
				writer.Write(entry.MasterSymbol ?? string.Empty);
				writer.Write(entry.SlaveSymbol ?? string.Empty);
				writer.Write(entry.Volume);
				writer.Write(entry.AveragePrice);
			}
		}
		catch (Exception ex)
		{
			this.LogWarning($"Failed to write snapshot: {ex.Message}");
		}
	}

	private List<CopierEntry> ReadSnapshot()
	{
		var result = new List<CopierEntry>();

		if (!File.Exists(_filePath))
		return result;

		try
		{
			using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false);
			var count = reader.ReadInt32();
			for (var i = 0; i < count; i++)
			{
				var entry = new CopierEntry
				{
					MasterSymbol = reader.ReadString(),
					SlaveSymbol = reader.ReadString(),
					Volume = reader.ReadDecimal(),
					AveragePrice = reader.ReadDecimal()
				};
				result.Add(entry);
			}
		}
		catch (Exception ex)
		{
			this.LogWarning($"Failed to read snapshot: {ex.Message}");
		}

		return result;
	}

	private (decimal volume, decimal averagePrice) GetAggregatePosition(string symbol)
	{
		decimal volume = 0m;
		decimal averagePrice = 0m;

		void Accumulate(IEnumerable<Position> positions)
		{
			foreach (var position in positions)
			{
				var security = position.Security;
				if (security == null)
				continue;

				if (!string.Equals(security.Id, symbol, StringComparison.OrdinalIgnoreCase))
				continue;

				var current = position.CurrentValue ?? 0m;
				if (current == 0m)
				continue;

				volume += current;
				var price = position.AveragePrice ?? 0m;
				if (price > 0m)
				averagePrice = price;
			}
		}

		var portfolio = Portfolio;
		if (portfolio != null)
		Accumulate(portfolio.Positions);

		Accumulate(Positions);

		return (volume, averagePrice);
	}

	private Security LookupSecurity(string symbol)
	{
		if (string.IsNullOrWhiteSpace(symbol))
		return null;

		if (_securityCache.TryGetValue(symbol, out var cached))
		return cached;

		var provider = (ISecurityProvider?)SecurityProvider ?? Connector;
		var security = provider?.LookupById(symbol);
		_securityCache[symbol] = security;
		return security;
	}

	private IEnumerable<(string Master, string Slave)> EnumerateMappings()
	{
		yield return (MasterSymbol1, SlaveSymbol1);
		yield return (MasterSymbol2, SlaveSymbol2);
	}

	private sealed class CopierEntry
	{
		public string MasterSymbol { get; set; }
		public string SlaveSymbol { get; set; }
		public decimal Volume { get; set; }
		public decimal AveragePrice { get; set; }
	}
}

/// <summary>
/// Mode of the trade copier.
/// </summary>
public enum TradeCopierMode
{
	/// <summary>
	/// Write master positions to the shared file.
	/// </summary>
	Master,

	/// <summary>
	/// Read the shared file and mirror the exposure.
	/// </summary>
	Slave
}
