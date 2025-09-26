using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Translation of the MetaTrader "PROphet" expert advisor.
/// The strategy keeps the original range-weighted entry trigger and trailing stop behaviour.
/// </summary>
public class ProphetStrategy : Strategy
{
	private readonly StrategyParam<bool> _allowBuy;
	private readonly StrategyParam<bool> _allowSell;
	private readonly StrategyParam<int> _x1;
	private readonly StrategyParam<int> _x2;
	private readonly StrategyParam<int> _x3;
	private readonly StrategyParam<int> _x4;
	private readonly StrategyParam<int> _buyStopLossPoints;
	private readonly StrategyParam<int> _y1;
	private readonly StrategyParam<int> _y2;
	private readonly StrategyParam<int> _y3;
	private readonly StrategyParam<int> _y4;
	private readonly StrategyParam<int> _sellStopLossPoints;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _tradeStartHour;
	private readonly StrategyParam<int> _tradeEndHour;
	private readonly StrategyParam<int> _exitHour;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _lastCandle;
	private ICandleMessage _previousCandle;
	private ICandleMessage _olderCandle;
	private decimal? _longStopPrice;
	private decimal? _shortStopPrice;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal _pointSize;
	private decimal? _lastBid;
	private decimal? _lastAsk;

	/// <summary>
	/// Enables or disables long entries.
	/// </summary>
	public bool AllowBuy
	{
		get => _allowBuy.Value;
		set => _allowBuy.Value = value;
	}

	/// <summary>
	/// Enables or disables short entries.
	/// </summary>
	public bool AllowSell
	{
		get => _allowSell.Value;
		set => _allowSell.Value = value;
	}

	/// <summary>
	/// Weight for |High[1] - Low[2]| inside the long signal formula.
	/// </summary>
	public int X1
	{
		get => _x1.Value;
		set => _x1.Value = value;
	}

	/// <summary>
	/// Weight for |High[3] - Low[2]| inside the long signal formula.
	/// </summary>
	public int X2
	{
		get => _x2.Value;
		set => _x2.Value = value;
	}

	/// <summary>
	/// Weight for |High[2] - Low[1]| inside the long signal formula.
	/// </summary>
	public int X3
	{
		get => _x3.Value;
		set => _x3.Value = value;
	}

	/// <summary>
	/// Weight for |High[2] - Low[3]| inside the long signal formula.
	/// </summary>
	public int X4
	{
		get => _x4.Value;
		set => _x4.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for long trades expressed in MetaTrader points.
	/// </summary>
	public int BuyStopLossPoints
	{
		get => _buyStopLossPoints.Value;
		set => _buyStopLossPoints.Value = value;
	}

	/// <summary>
	/// Weight for |High[1] - Low[2]| inside the short signal formula.
	/// </summary>
	public int Y1
	{
		get => _y1.Value;
		set => _y1.Value = value;
	}

	/// <summary>
	/// Weight for |High[3] - Low[2]| inside the short signal formula.
	/// </summary>
	public int Y2
	{
		get => _y2.Value;
		set => _y2.Value = value;
	}

	/// <summary>
	/// Weight for |High[2] - Low[1]| inside the short signal formula.
	/// </summary>
	public int Y3
	{
		get => _y3.Value;
		set => _y3.Value = value;
	}

	/// <summary>
	/// Weight for |High[2] - Low[3]| inside the short signal formula.
	/// </summary>
	public int Y4
	{
		get => _y4.Value;
		set => _y4.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for short trades expressed in MetaTrader points.
	/// </summary>
	public int SellStopLossPoints
	{
		get => _sellStopLossPoints.Value;
		set => _sellStopLossPoints.Value = value;
	}

	/// <summary>
	/// Base trade size used for entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// First hour (inclusive) when new entries are allowed.
	/// </summary>
	public int TradeStartHour
	{
		get => _tradeStartHour.Value;
		set => _tradeStartHour.Value = value;
	}

	/// <summary>
	/// Last hour (inclusive) when new entries are allowed.
	/// </summary>
	public int TradeEndHour
	{
		get => _tradeEndHour.Value;
		set => _tradeEndHour.Value = value;
	}

	/// <summary>
	/// Hour after which the strategy forces position exits.
	/// </summary>
	public int ExitHour
	{
		get => _exitHour.Value;
		set => _exitHour.Value = value;
	}

	/// <summary>
	/// Candle type used for price analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ProphetStrategy"/>.
	/// </summary>
	public ProphetStrategy()
	{
		_allowBuy = Param(nameof(AllowBuy), true)
			.SetDisplay("Allow Long", "Enable buy-side trades", "Trading");

		_allowSell = Param(nameof(AllowSell), true)
			.SetDisplay("Allow Short", "Enable sell-side trades", "Trading");

		_x1 = Param(nameof(X1), 9)
			.SetDisplay("X1", "Weight applied to |High[1] - Low[2]|", "Long Signal");

		_x2 = Param(nameof(X2), 29)
			.SetDisplay("X2", "Weight applied to |High[3] - Low[2]|", "Long Signal");

		_x3 = Param(nameof(X3), 94)
			.SetDisplay("X3", "Weight applied to |High[2] - Low[1]|", "Long Signal");

		_x4 = Param(nameof(X4), 125)
			.SetDisplay("X4", "Weight applied to |High[2] - Low[3]|", "Long Signal");

		_buyStopLossPoints = Param(nameof(BuyStopLossPoints), 68)
			.SetDisplay("Buy SL (pts)", "Stop distance for long trades in points", "Risk");

		_y1 = Param(nameof(Y1), 61)
			.SetDisplay("Y1", "Weight applied to |High[1] - Low[2]|", "Short Signal");

		_y2 = Param(nameof(Y2), 100)
			.SetDisplay("Y2", "Weight applied to |High[3] - Low[2]|", "Short Signal");

		_y3 = Param(nameof(Y3), 117)
			.SetDisplay("Y3", "Weight applied to |High[2] - Low[1]|", "Short Signal");

		_y4 = Param(nameof(Y4), 31)
			.SetDisplay("Y4", "Weight applied to |High[2] - Low[3]|", "Short Signal");

		_sellStopLossPoints = Param(nameof(SellStopLossPoints), 72)
			.SetDisplay("Sell SL (pts)", "Stop distance for short trades in points", "Risk");

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Base trade volume", "Trading");

		_tradeStartHour = Param(nameof(TradeStartHour), 10)
			.SetRange(0, 23)
			.SetDisplay("Start Hour", "Hour (0-23) when entries become valid", "Session");

		_tradeEndHour = Param(nameof(TradeEndHour), 18)
			.SetRange(0, 23)
			.SetDisplay("End Hour", "Hour (0-23) after which no new trades open", "Session");

		_exitHour = Param(nameof(ExitHour), 18)
			.SetRange(0, 23)
			.SetDisplay("Exit Hour", "Hour (0-23) used to flatten positions", "Session");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for calculations", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_lastCandle = null;
		_previousCandle = null;
		_olderCandle = null;
		_longStopPrice = null;
		_shortStopPrice = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_lastBid = null;
		_lastAsk = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_pointSize = Security?.PriceStep ?? 0m;
		if (_pointSize <= 0m)
			_pointSize = 0.0001m;

		var candleSubscription = SubscribeCandles(CandleType);
		candleSubscription
			.Bind(ProcessCandle)
			.Start();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, candleSubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		var changes = level1.Changes;

		if (changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj))
			_lastBid = (decimal)bidObj;

		if (changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj))
			_lastAsk = (decimal)askObj;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position > 0m)
		{
			_longEntryPrice = PositionPrice;

			var distance = GetStopDistance(BuyStopLossPoints);
			_longStopPrice = distance > 0m ? PositionPrice - distance : null;

			_shortEntryPrice = null;
			_shortStopPrice = null;
		}
		else if (Position < 0m)
		{
			_shortEntryPrice = PositionPrice;

			var distance = GetStopDistance(SellStopLossPoints);
			_shortStopPrice = distance > 0m ? PositionPrice + distance : null;

			_longEntryPrice = null;
			_longStopPrice = null;
		}
		else
		{
			_longEntryPrice = null;
			_shortEntryPrice = null;
			_longStopPrice = null;
			_shortStopPrice = null;
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateHistory(candle);

		var hour = candle.OpenTime.Hour;
		var exitTriggered = ManageOpenPositions(candle, hour);

		if (exitTriggered || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (_olderCandle == null)
			return;

		TryEnterLong(hour);
		TryEnterShort(hour);
	}

	private void UpdateHistory(ICandleMessage candle)
	{
		// Shift candle references to emulate the MQL4 indexing scheme.
		_olderCandle = _previousCandle;
		_previousCandle = _lastCandle;
		_lastCandle = candle;
	}

	private bool ManageOpenPositions(ICandleMessage candle, int hour)
	{
		var closed = false;

		if (Position > 0m)
		{
			// Close long trades after the configured session.
			if (hour > ExitHour)
			{
				SellMarket(Position);
				closed = true;
			}
			else if (UpdateLongTrailing(candle))
			{
				closed = true;
			}
		}
		else if (Position < 0m)
		{
			// Close short trades after the configured session.
			if (hour > ExitHour)
			{
				BuyMarket(-Position);
				closed = true;
			}
			else if (UpdateShortTrailing(candle))
			{
				closed = true;
			}
		}
		else
		{
			_longStopPrice = null;
			_shortStopPrice = null;
		}

		return closed;
	}

	private bool UpdateLongTrailing(ICandleMessage candle)
	{
		var stopDistance = GetStopDistance(BuyStopLossPoints);
		if (stopDistance <= 0m)
			return false;

		if (_longStopPrice == null && _longEntryPrice.HasValue)
			_longStopPrice = _longEntryPrice.Value - stopDistance;

		if (_longStopPrice == null)
			return false;

		var bid = _lastBid ?? candle.ClosePrice;
		var spread = GetCurrentSpread();
		var trigger = _longStopPrice.Value + spread + 2m * stopDistance;

		// Advance the stop once price has moved by spread + 2*SL points.
		if (bid > trigger)
		{
			var candidate = bid - stopDistance;
			if (candidate > _longStopPrice.Value)
				_longStopPrice = candidate;
		}

		// Exit if the candle hits the trailing stop.
		if (candle.LowPrice <= _longStopPrice.Value)
		{
			SellMarket(Position);
			return true;
		}

		return false;
	}

	private bool UpdateShortTrailing(ICandleMessage candle)
	{
		var stopDistance = GetStopDistance(SellStopLossPoints);
		if (stopDistance <= 0m)
			return false;

		if (_shortStopPrice == null && _shortEntryPrice.HasValue)
			_shortStopPrice = _shortEntryPrice.Value + stopDistance;

		if (_shortStopPrice == null)
			return false;

		var ask = _lastAsk ?? candle.ClosePrice;
		var spread = GetCurrentSpread();
		var trigger = _shortStopPrice.Value - spread - 2m * stopDistance;

		// Advance the stop once price has moved by spread + 2*SL points.
		if (ask < trigger)
		{
			var candidate = ask + stopDistance;
			if (candidate < _shortStopPrice.Value)
				_shortStopPrice = candidate;
		}

		// Exit if the candle hits the trailing stop.
		if (candle.HighPrice >= _shortStopPrice.Value)
		{
			BuyMarket(-Position);
			return true;
		}

		return false;
	}

	private void TryEnterLong(int hour)
	{
		if (!AllowBuy)
			return;

		if (hour < TradeStartHour || hour > TradeEndHour)
			return;

		var volume = TradeVolume + Math.Max(-Position, 0m);
		if (volume <= 0m)
			return;

		var signal = CalculateQu(X1, X2, X3, X4);
		if (signal <= 0m)
			return;

		// Enter long; the order size neutralises existing shorts before opening the new position.
		BuyMarket(volume);
	}

	private void TryEnterShort(int hour)
	{
		if (!AllowSell)
			return;

		if (hour < TradeStartHour || hour > TradeEndHour)
			return;

		var volume = TradeVolume + Math.Max(Position, 0m);
		if (volume <= 0m)
			return;

		var signal = CalculateQu(Y1, Y2, Y3, Y4);
		if (signal <= 0m)
			return;

		// Enter short; the order size neutralises existing longs before opening the new position.
		SellMarket(volume);
	}

	private decimal CalculateQu(int q1, int q2, int q3, int q4)
	{
		if (_lastCandle == null || _previousCandle == null || _olderCandle == null)
			return 0m;

		var term1 = Math.Abs(_lastCandle.HighPrice - _previousCandle.LowPrice);
		var term2 = Math.Abs(_olderCandle.HighPrice - _previousCandle.LowPrice);
		var term3 = Math.Abs(_previousCandle.HighPrice - _lastCandle.LowPrice);
		var term4 = Math.Abs(_previousCandle.HighPrice - _olderCandle.LowPrice);

		return (q1 - 100) * term1 + (q2 - 100) * term2 + (q3 - 100) * term3 + (q4 - 100) * term4;
	}

	private decimal GetStopDistance(int points)
	{
		if (points <= 0 || _pointSize <= 0m)
			return 0m;

		return points * _pointSize;
	}

	private decimal GetCurrentSpread()
	{
		if (_lastBid.HasValue && _lastAsk.HasValue)
		{
			var spread = _lastAsk.Value - _lastBid.Value;
			if (spread > 0m)
				return spread;
		}

		return _pointSize > 0m ? _pointSize : 0m;
	}
}
