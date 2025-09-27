using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// NRTR reversal strategy converted from MetaTrader 4.
/// The strategy follows the NRTR trailing line calculated from ATR and reverses when price breaks it.
/// </summary>
public class NrtrReversalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _lotSize;
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<int> _reverseDistancePoints;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<CandleSnapshot> _history = new();

	private AverageTrueRange _atr;
	private bool _isTrendUp;
	private decimal _currentLine;
	private decimal _priceStep;

	/// <summary>
	/// Initializes a new instance of the <see cref="NrtrReversalStrategy"/> class.
	/// </summary>
	public NrtrReversalStrategy()
	{
		_takeProfitPoints = Param(nameof(TakeProfitPoints), 4000m)
		.SetDisplay("Take Profit (points)", "Target distance in price steps", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(500m, 6000m, 500m);

		_stopLossPoints = Param(nameof(StopLossPoints), 4000m)
		.SetDisplay("Stop Loss (points)", "Stop distance in price steps", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(500m, 6000m, 500m);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 0m)
		.SetDisplay("Trailing Stop (points)", "Reserved for manual trailing stop modules", "Risk Management");

		_lotSize = Param(nameof(TradeVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Trade Volume", "Base order volume that mirrors MetaTrader lots", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 1m, 0.1m);

		_period = Param(nameof(Period), 3)
		.SetGreaterThanZero()
		.SetDisplay("Period", "Number of candles used to build the NRTR pivot", "NRTR")
		.SetCanOptimize(true)
		.SetOptimize(3, 20, 1);

		_reverseDistancePoints = Param(nameof(ReverseDistancePoints), 100)
		.SetGreaterThanZero()
		.SetDisplay("Reverse Distance (points)", "Minimum breakout distance to confirm a reversal", "NRTR")
		.SetCanOptimize(true)
		.SetOptimize(50, 500, 50);

		_atrMultiplier = Param(nameof(AtrMultiplier), 3m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Multiplier", "Multiplier that converts ATR into the NRTR offset", "NRTR")
		.SetCanOptimize(true)
		.SetOptimize(1m, 6m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Time frame used for calculations", "General");
	}

	/// <summary>
	/// Take-profit distance expressed in instrument steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in instrument steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in instrument steps.
	/// Reserved for external trailing modules.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Default trade volume.
	/// </summary>
	public decimal TradeVolume
	{
		get => _lotSize.Value;
		set => _lotSize.Value = value;
	}

	/// <summary>
	/// Number of candles used to calculate NRTR pivots.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Breakout distance in points required to reverse the trend.
	/// </summary>
	public int ReverseDistancePoints
	{
		get => _reverseDistancePoints.Value;
		set => _reverseDistancePoints.Value = value;
	}

	/// <summary>
	/// Multiplier applied to ATR before building the NRTR line.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Candle data type used for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_history.Clear();
		_isTrendUp = true;
		_currentLine = 0m;
		_priceStep = 0m;
		Volume = TradeVolume;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;
		_isTrendUp = true;
		_priceStep = Security?.PriceStep ?? 0m;
		if (_priceStep <= 0m)
		_priceStep = 1m;

		_atr = new AverageTrueRange { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_atr, ProcessCandle)
		.Start();

		var takeProfitUnit = TakeProfitPoints > 0m ? new Unit(TakeProfitPoints, UnitTypes.Step) : null;
		var stopLossUnit = StopLossPoints > 0m ? new Unit(StopLossPoints, UnitTypes.Step) : null;

		if (takeProfitUnit != null || stopLossUnit != null)
		{
			StartProtection(takeProfit: takeProfitUnit, stopLoss: stopLossUnit, useMarketOrders: true);
		}
		else
		{
			StartProtection();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var atr = atrValue.IsFinal ? atrValue.ToDecimal() : atrValue.GetValue<decimal>();

		_history.Add(new CandleSnapshot(candle.HighPrice, candle.LowPrice, candle.ClosePrice));
		TrimHistory();

		if (_atr == null || !_atr.IsFormed)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_history.Count < Math.Max(Period, 2))
		return;

		var point = _priceStep > 0m ? _priceStep : 1m;
		if (point <= 0m)
		return;

		if (atr <= 0m)
		return;

		var atrPoints = atr / point;
		var offsetPoints = Math.Round(AtrMultiplier * atrPoints / 10m, MidpointRounding.AwayFromZero);
		if (offsetPoints <= 0m)
		offsetPoints = 1m;

		var offsetPrice = offsetPoints * point;
		var breakLevel = offsetPoints * point;
		var reverseOffset = ReverseDistancePoints * point;

		var latest = _history[^1];

		var halfLength = Math.Max(1, (int)Math.Round(Period / 2m, MidpointRounding.AwayFromZero));

		if (_isTrendUp)
		{
			var lowest = GetLowestLow(Period);
			var recentLowest = GetLowestLow(halfLength);

			_currentLine = lowest - offsetPrice;

			var breakByClose = _currentLine - latest.Close > breakLevel;
			var breakByReverse = recentLowest - _currentLine >= reverseOffset;

			if (breakByClose || breakByReverse)
			{
				_isTrendUp = false;
				LogInfo($"Switching to short. Line={_currentLine}, Close={latest.Close}, BreakByClose={breakByClose}, BreakByReverse={breakByReverse}");
				SellMarket(Volume + Math.Abs(Position));
			}
		}
		else
		{
			var highest = GetHighestHigh(Period);
			var recentHighest = GetHighestHigh(halfLength);

			_currentLine = highest + offsetPrice;

			var breakByClose = latest.Close - _currentLine > breakLevel;
			var breakByReverse = _currentLine - recentHighest >= reverseOffset;

			if (breakByClose || breakByReverse)
			{
				_isTrendUp = true;
				LogInfo($"Switching to long. Line={_currentLine}, Close={latest.Close}, BreakByClose={breakByClose}, BreakByReverse={breakByReverse}");
				BuyMarket(Volume + Math.Abs(Position));
			}
		}
	}

	private decimal GetLowestLow(int length)
	{
		var result = decimal.MaxValue;
		var count = 0;

		for (var i = _history.Count - 1; i >= 0 && count < length; i--, count++)
		{
			var low = _history[i].Low;
			if (low < result)
			result = low;
		}

		return result;
	}

	private decimal GetHighestHigh(int length)
	{
		var result = decimal.MinValue;
		var count = 0;

		for (var i = _history.Count - 1; i >= 0 && count < length; i--, count++)
		{
			var high = _history[i].High;
			if (high > result)
			result = high;
		}

		return result;
	}

	private void TrimHistory()
	{
		var maxLength = Math.Max(Period * 3, 20);
		if (_history.Count <= maxLength)
		return;

		var removeCount = _history.Count - maxLength;
		_history.RemoveRange(0, removeCount);
	}

	private readonly struct CandleSnapshot
	{
		public CandleSnapshot(decimal high, decimal low, decimal close)
		{
			High = high;
			Low = low;
			Close = close;
		}

		public decimal High { get; }
		public decimal Low { get; }
		public decimal Close { get; }
	}
}

