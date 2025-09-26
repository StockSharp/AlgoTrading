using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that keeps a single market position open and manages exits using fixed take-profit and stop-loss distances.
/// </summary>
public class CsvExampleExpertStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _takePoints;
	private readonly StrategyParam<decimal> _stopPoints;
	private readonly StrategyParam<Sides> _tradeDirection;
	private readonly StrategyParam<bool> _writeCloseData;
	private readonly StrategyParam<string> _fileName;

	private decimal _pointSize;
	private decimal _lastRealizedPnL;
	private decimal? _lastBid;
	private decimal? _lastAsk;
	private decimal? _positionEntryPrice;
	private string _resolvedFilePath;
	private decimal? _lastMyTradePrice;
	private DateTimeOffset? _lastMyTradeTime;

	/// <summary>
	/// Volume for every market order.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in MetaTrader points.
	/// </summary>
	public decimal TakePoints
	{
		get => _takePoints.Value;
		set => _takePoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in MetaTrader points.
	/// </summary>
	public decimal StopPoints
	{
		get => _stopPoints.Value;
		set => _stopPoints.Value = value;
	}

	/// <summary>
	/// Direction of the position that must always stay open.
	/// </summary>
	public Sides TradeDirection
	{
		get => _tradeDirection.Value;
		set => _tradeDirection.Value = value;
	}

	/// <summary>
	/// Enable writing closed trade information to CSV file.
	/// </summary>
	public bool WriteCloseData
	{
		get => _writeCloseData.Value;
		set => _writeCloseData.Value = value;
	}

	/// <summary>
	/// Relative or absolute path to the CSV file.
	/// </summary>
	public string FileName
	{
		get => _fileName.Value;
		set => _fileName.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="CsvExampleExpertStrategy"/>.
	/// </summary>
	public CsvExampleExpertStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetDisplay("Trade Volume", "Volume submitted with every market order", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1m, 0.1m);

		_takePoints = Param(nameof(TakePoints), 300m)
			.SetDisplay("Take Profit Points", "Distance to the take-profit in MetaTrader points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(50m, 1000m, 50m);

		_stopPoints = Param(nameof(StopPoints), 300m)
			.SetDisplay("Stop Loss Points", "Distance to the stop-loss in MetaTrader points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(50m, 1000m, 50m);

		_tradeDirection = Param(nameof(TradeDirection), Sides.Sell)
			.SetDisplay("Trade Direction", "Side of the market to keep open (Buy or Sell)", "Trading");

		_writeCloseData = Param(nameof(WriteCloseData), false)
			.SetDisplay("Write Close Data", "Write closed trade information into CSV file", "Logging");

		_fileName = Param(nameof(FileName), Path.Combine("CSVexpert", "CSVexample.csv"))
			.SetDisplay("File Name", "Path to CSV log file for closed trades", "Logging");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pointSize = 0m;
		_lastRealizedPnL = 0m;
		_lastBid = null;
		_lastAsk = null;
		_positionEntryPrice = null;
		_resolvedFilePath = null;
		_lastMyTradePrice = null;
		_lastMyTradeTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_pointSize = GetPointSize();
		_lastRealizedPnL = PnL;

		PrepareLogFile();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade is null)
			return;

		var execution = trade.Trade;

		if (execution != null)
		{
			_lastMyTradePrice = execution.Price;
			_lastMyTradeTime = execution.Time;
		}
		else
		{
			_lastMyTradePrice = trade.Order.Price ?? _lastMyTradePrice;
			_lastMyTradeTime = trade.Order.LastChangeTime ?? _lastMyTradeTime ?? CurrentTime;
		}

	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (delta == 0m)
			return;

		if (Position != 0m)
		{
			_positionEntryPrice = PositionPrice;
			return;
		}

		var realizedPnL = PnL;
		var tradePnL = realizedPnL - _lastRealizedPnL;
		_lastRealizedPnL = realizedPnL;

		if (WriteCloseData && _positionEntryPrice is decimal entryPrice && _lastMyTradePrice is decimal exitPrice)
		{
			var closeTime = _lastMyTradeTime ?? CurrentTime;
			var volume = Math.Abs(delta);
			var direction = delta < 0m ? "LONG" : "SHORT";

			LogClosedTrade(direction, tradePnL, exitPrice, closeTime, volume);
		}

		_positionEntryPrice = null;
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1 is null)
			return;

		var changes = level1.Changes;

		if (changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj) && bidObj is decimal bid)
			_lastBid = bid;

		if (changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj) && askObj is decimal ask)
			_lastAsk = ask;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position == 0m)
		{
			if (!HasActiveOrders())
				TryOpenPosition();

			return;
		}

		CheckExitConditions();
	}

	private void TryOpenPosition()
	{
		var volume = TradeVolume;

		if (volume <= 0m)
		{
			LogWarning("Trade volume must be positive to place orders.");
			return;
		}

		if (HasActiveOrders())
			return;

		if (TradeDirection == Sides.Buy)
		{
			BuyMarket(volume);
		}
		else if (TradeDirection == Sides.Sell)
		{
			SellMarket(volume);
		}
	}

	private void CheckExitConditions()
	{
		if (_positionEntryPrice is null)
			return;

		var stopDistance = GetDistance(StopPoints);
		var takeDistance = GetDistance(TakePoints);

		if (Position > 0m)
		{
			if (_lastAsk is not decimal ask)
			return;

			var entry = _positionEntryPrice.Value;

			if (!HasActiveOrders() && stopDistance > 0m && entry - ask >= stopDistance)
			{
				SellMarket(Position);
				return;
			}

			if (!HasActiveOrders() && takeDistance > 0m && ask - entry >= takeDistance)
			{
				SellMarket(Position);
			}
		}
		else if (Position < 0m)
		{
			if (_lastBid is not decimal bid)
			return;

			var entry = _positionEntryPrice.Value;
			var volume = Math.Abs(Position);

			if (!HasActiveOrders() && takeDistance > 0m && entry - bid >= takeDistance)
			{
				BuyMarket(volume);
				return;
			}

			if (!HasActiveOrders() && stopDistance > 0m && bid - entry >= stopDistance)
			{
				BuyMarket(volume);
			}
		}
	}

	private bool HasActiveOrders()
	{
		foreach (var order in Orders)
		{
			if (order != null && order.State.IsActive())
				return true;
		}

		return false;
	}

	private decimal GetDistance(decimal points)
	{
		var distance = points * _pointSize;
		return distance < 0m ? 0m : distance;
	}

	private decimal GetPointSize()
	{
		var step = Security?.PriceStep ?? 0m;

		if (step <= 0m)
			step = Security?.MinPriceStep ?? 0m;

		if (step <= 0m)
			step = 0.0001m;

		return step;
	}

	private void PrepareLogFile()
	{
		if (!WriteCloseData)
		{
			_resolvedFilePath = null;
			return;
		}

		var name = FileName;

		if (string.IsNullOrWhiteSpace(name))
		{
			LogWarning("FileName parameter is empty. Closed trade logging is disabled.");
			_resolvedFilePath = null;
			return;
		}

		var filePath = name;

		if (!Path.IsPathRooted(filePath))
			filePath = Path.Combine(Environment.CurrentDirectory, filePath);

		try
		{
			var directory = Path.GetDirectoryName(filePath);

			if (!string.IsNullOrEmpty(directory))
				Directory.CreateDirectory(directory);

			var header = string.Join(",", "OPType", "Gain/Loss", "ClosePrice", "CloseTime", "Symbol", "Lots") + Environment.NewLine;
			File.WriteAllText(filePath, header);
			_resolvedFilePath = filePath;
		}
		catch (Exception error)
		{
			LogError("Failed to prepare CSV file {0}. Error: {1}", filePath, error.Message);
			_resolvedFilePath = null;
		}
	}

	private void LogClosedTrade(string direction, decimal gain, decimal closePrice, DateTimeOffset closeTime, decimal volume)
	{
		if (!WriteCloseData || string.IsNullOrEmpty(_resolvedFilePath))
			return;

		try
		{
			var symbol = Security?.Id ?? string.Empty;
			var line = string.Join(",",
				direction,
				gain.ToString(CultureInfo.InvariantCulture),
				closePrice.ToString(CultureInfo.InvariantCulture),
				closeTime.ToString("O", CultureInfo.InvariantCulture),
				symbol,
				volume.ToString(CultureInfo.InvariantCulture)) + Environment.NewLine;

			File.AppendAllText(_resolvedFilePath, line);
		}
		catch (Exception error)
		{
			LogError("Failed to write trade information to {0}. Error: {1}", _resolvedFilePath, error.Message);
		}
	}
}
