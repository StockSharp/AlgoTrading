using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Manual practice strategy that recreates the MT4 PracticeMod helper using command files instead of chart objects.
/// </summary>
public class PracticeModStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<string> _commandDirectory;
	private readonly StrategyParam<string> _entryFileName;
	private readonly StrategyParam<string> _modifyFileName;
	private readonly StrategyParam<string> _closeFileName;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _point;
	private decimal? _pendingEntryPrice;
	private bool _closeRequested;
	private bool _longExitRequested;
	private bool _shortExitRequested;
	private decimal? _lastLongEntry;
	private decimal? _lastShortEntry;
	private decimal? _longStop;
	private decimal? _longTake;
	private decimal? _shortStop;
	private decimal? _shortTake;

	/// <summary>
	/// Order volume for market operations.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Initial take-profit distance expressed in points.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Initial stop-loss distance expressed in points.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in points. Set to zero to disable trailing.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Directory that stores entry, modification and close command files.
	/// </summary>
	public string CommandDirectory
	{
		get => _commandDirectory.Value;
		set => _commandDirectory.Value = value;
	}

	/// <summary>
	/// File name that contains pending entry commands.
	/// </summary>
	public string EntryFileName
	{
		get => _entryFileName.Value;
		set => _entryFileName.Value = value;
	}

	/// <summary>
	/// File name that contains modification commands.
	/// </summary>
	public string ModifyFileName
	{
		get => _modifyFileName.Value;
		set => _modifyFileName.Value = value;
	}

	/// <summary>
	/// File name that triggers position liquidation.
	/// </summary>
	public string CloseFileName
	{
		get => _closeFileName.Value;
		set => _closeFileName.Value = value;
	}

	/// <summary>
	/// Candle type used for periodic command polling.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="PracticeModStrategy"/>.
	/// </summary>
	public PracticeModStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Market order volume", "Trading");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (points)", "Initial take-profit distance in points", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 30m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (points)", "Initial stop-loss distance in points", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 0m)
			.SetDisplay("Trailing Stop (points)", "Trailing stop distance in points", "Risk");

		_commandDirectory = Param(nameof(CommandDirectory), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PracticeMod"))
			.SetDisplay("Command Directory", "Folder that stores entry/exit command files", "General");

		_entryFileName = Param(nameof(EntryFileName), "entry.txt")
			.SetDisplay("Entry File", "File name that contains the next entry price", "General");

		_modifyFileName = Param(nameof(ModifyFileName), "modify.txt")
			.SetDisplay("Modify File", "File name with price levels that adjust stops or targets", "General");

		_closeFileName = Param(nameof(CloseFileName), "close.txt")
			.SetDisplay("Close File", "File name that forces the current position to close", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for periodic polling", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[]
		{
			(Security, CandleType),
			(Security, DataType.Level1)
		};
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pendingEntryPrice = null;
		_closeRequested = false;
		_longExitRequested = false;
		_shortExitRequested = false;
		_lastLongEntry = null;
		_lastShortEntry = null;
		_longStop = null;
		_longTake = null;
		_shortStop = null;
		_shortTake = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_point = Security?.PriceStep ?? 1m;
		if (_point <= 0m)
		{
			_point = 1m;
		}

		try
		{
			Directory.CreateDirectory(CommandDirectory);
		}
		catch (Exception ex)
		{
			LogError($"Failed to create command directory '{CommandDirectory}': {ex.Message}");
		}

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		{
			return;
		}

		var canTrade = IsFormedAndOnlineAndAllowTrading();
		ProcessCommands(canTrade);
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		var canTrade = IsFormedAndOnlineAndAllowTrading();
		ProcessCommands(canTrade);

		if (!canTrade)
		{
			return;
		}

		ProcessRiskManagement();
	}

	private void ProcessCommands(bool canTrade)
	{
		LoadEntryCommand();

		if (canTrade)
		{
			ExecuteEntry();
		}

		if (Position != 0m)
		{
			LoadModifyCommands();
		}

		LoadCloseCommand();

		if (canTrade)
		{
			ExecuteClose();
		}
	}

	private void LoadEntryCommand()
	{
		if (_pendingEntryPrice.HasValue)
		{
			return;
		}

		var path = Path.Combine(CommandDirectory, EntryFileName);
		if (!File.Exists(path))
		{
			return;
		}

		decimal? price = null;

		try
		{
			var content = File.ReadAllText(path);
			var tokens = content.Split(new[] { '\r', '\n', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);

			foreach (var token in tokens)
			{
				if (decimal.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
				{
					price = NormalizePrice(parsed);
					break;
				}
			}

			if (price is null)
			{
				LogWarning($"No valid price found in entry command '{path}'.");
			}
		}
		catch (Exception ex)
		{
			LogError($"Failed to read entry command '{path}': {ex.Message}");
		}
		finally
		{
			TryDeleteFile(path);
		}

		_pendingEntryPrice = price;
	}

	private void ExecuteEntry()
	{
		if (!_pendingEntryPrice.HasValue)
		{
			return;
		}

		if (Position != 0m || Volume <= 0m || HasActiveOrders())
		{
			LogWarning("Entry ignored because the strategy is busy or volume is not positive.");
			_pendingEntryPrice = null;
			return;
		}

		var bid = GetBidPrice();
		if (bid is null)
		{
			return;
		}

		var entryPrice = _pendingEntryPrice.Value;

		if (entryPrice > bid.Value)
		{
			BuyMarket(Volume);
			_longExitRequested = false;
		}
		else if (entryPrice < bid.Value)
		{
			SellMarket(Volume);
			_shortExitRequested = false;
		}

		_pendingEntryPrice = null;
	}

	private void LoadModifyCommands()
	{
		var path = Path.Combine(CommandDirectory, ModifyFileName);
		if (!File.Exists(path))
		{
			return;
		}

		string[] lines;

		try
		{
			lines = File.ReadAllLines(path);
		}
		catch (Exception ex)
		{
			LogError($"Failed to read modify command '{path}': {ex.Message}");
			return;
		}
		finally
		{
			TryDeleteFile(path);
		}

		foreach (var line in lines)
		{
			var trimmed = line.Trim();
			if (string.IsNullOrEmpty(trimmed))
			{
				continue;
			}

			if (!decimal.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var price))
			{
				LogWarning($"Cannot parse price '{trimmed}' in modify command.");
				continue;
			}

			ApplyModification(NormalizePrice(price));
		}
	}

	private void ApplyModification(decimal price)
	{
		var bid = GetBidPrice();
		if (bid is null)
		{
			return;
		}

		if (Position > 0m)
		{
			if (price > bid.Value)
			{
				_longTake = price;
			}
			else if (price < bid.Value)
			{
				_longStop = price;
			}
		}
		else if (Position < 0m)
		{
			if (price > bid.Value)
			{
				_shortStop = price;
			}
			else if (price < bid.Value)
			{
				_shortTake = price;
			}
		}
	}

	private void LoadCloseCommand()
	{
		if (_closeRequested)
		{
			return;
		}

		var path = Path.Combine(CommandDirectory, CloseFileName);
		if (!File.Exists(path))
		{
			return;
		}

		_closeRequested = true;
		TryDeleteFile(path);
	}

	private void ExecuteClose()
	{
		if (!_closeRequested)
		{
			return;
		}

		if (Position > 0m && !_longExitRequested)
		{
			SellMarket(Position);
			_longExitRequested = true;
		}
		else if (Position < 0m && !_shortExitRequested)
		{
			BuyMarket(Math.Abs(Position));
			_shortExitRequested = true;
		}

		if (Position == 0m)
		{
			_closeRequested = false;
		}
	}

	private void ProcessRiskManagement()
	{
		if (Position > 0m)
		{
			if (_longExitRequested)
			{
				return;
			}

			var bid = GetBidPrice();
			if (bid is null)
			{
				return;
			}

			var price = bid.Value;

			if (_longTake.HasValue && price >= _longTake.Value)
			{
				SellMarket(Position);
				_longExitRequested = true;
				return;
			}

			if (_longStop.HasValue && price <= _longStop.Value)
			{
				SellMarket(Position);
				_longExitRequested = true;
				return;
			}

			ApplyLongTrailing(price);
		}
		else if (Position < 0m)
		{
			if (_shortExitRequested)
			{
				return;
			}

			var ask = GetAskPrice();
			if (ask is null)
			{
				return;
			}

			var price = ask.Value;

			if (_shortTake.HasValue && price <= _shortTake.Value)
			{
				BuyMarket(Math.Abs(Position));
				_shortExitRequested = true;
				return;
			}

			if (_shortStop.HasValue && price >= _shortStop.Value)
			{
				BuyMarket(Math.Abs(Position));
				_shortExitRequested = true;
				return;
			}

			ApplyShortTrailing(price);
		}
	}

	private void ApplyLongTrailing(decimal bidPrice)
	{
		if (TrailingStopPips <= 0m || !_lastLongEntry.HasValue)
		{
			return;
		}

		var offset = TrailingStopPips * _point;
		if (offset <= 0m)
		{
			return;
		}

		if (bidPrice - _lastLongEntry.Value <= offset)
		{
			return;
		}

		var newStop = NormalizePrice(bidPrice - offset);

		if (!_longStop.HasValue || _longStop.Value < newStop)
		{
			_longStop = newStop;
		}
	}

	private void ApplyShortTrailing(decimal askPrice)
	{
		if (TrailingStopPips <= 0m || !_lastShortEntry.HasValue)
		{
			return;
		}

		var offset = TrailingStopPips * _point;
		if (offset <= 0m)
		{
			return;
		}

		if (_lastShortEntry.Value - askPrice <= offset)
		{
			return;
		}

		var newStop = NormalizePrice(askPrice + offset);

		if (!_shortStop.HasValue || _shortStop.Value > newStop)
		{
			_shortStop = newStop;
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		var previous = Position - delta;

		if (Position > 0m)
		{
			if (previous <= 0m)
			{
				_lastLongEntry = PositionPrice != 0m ? PositionPrice : GetBidPrice();
				_longTake = TakeProfitPips > 0m && _lastLongEntry.HasValue
				? NormalizePrice(_lastLongEntry.Value + TakeProfitPips * _point)
				: null;
				_longStop = StopLossPips > 0m && _lastLongEntry.HasValue
				? NormalizePrice(_lastLongEntry.Value - StopLossPips * _point)
				: null;
			}

			_shortStop = null;
			_shortTake = null;
			_lastShortEntry = null;
			_shortExitRequested = false;
		}
		else if (Position < 0m)
		{
			if (previous >= 0m)
			{
				_lastShortEntry = PositionPrice != 0m ? PositionPrice : GetAskPrice();
				_shortTake = TakeProfitPips > 0m && _lastShortEntry.HasValue
				? NormalizePrice(_lastShortEntry.Value - TakeProfitPips * _point)
				: null;
				_shortStop = StopLossPips > 0m && _lastShortEntry.HasValue
				? NormalizePrice(_lastShortEntry.Value + StopLossPips * _point)
				: null;
			}

			_longStop = null;
			_longTake = null;
			_lastLongEntry = null;
			_longExitRequested = false;
		}
		else
		{
			_lastLongEntry = null;
			_lastShortEntry = null;
			_longStop = null;
			_longTake = null;
			_shortStop = null;
			_shortTake = null;
			_closeRequested = false;
			_longExitRequested = false;
			_shortExitRequested = false;
		}
	}

	private decimal? GetBidPrice()
	{
		if (Security?.BestBid?.Price is decimal bid && bid > 0m)
		{
			return bid;
		}

		if (Security?.LastTrade?.Price is decimal last && last > 0m)
		{
			return last;
		}

		return null;
	}

	private decimal? GetAskPrice()
	{
		if (Security?.BestAsk?.Price is decimal ask && ask > 0m)
		{
			return ask;
		}

		if (Security?.LastTrade?.Price is decimal last && last > 0m)
		{
			return last;
		}

		return null;
	}

	private void TryDeleteFile(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch (Exception ex)
		{
			LogWarning($"Failed to delete command file '{path}': {ex.Message}");
		}
	}

	private bool HasActiveOrders()
	{
		foreach (var order in Orders)
		{
			if (order.State == OrderStates.Active)
			{
				return true;
			}
		}

		return false;
	}

	private decimal NormalizePrice(decimal price)
	{
		var security = Security;
		if (security == null)
		{
			return price;
		}

		var normalized = security.ShrinkPrice(price);
		return normalized > 0m ? normalized : price;
	}
}
