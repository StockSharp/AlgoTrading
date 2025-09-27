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

using StockSharp.Algo.Candles;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy converted from the Exp_VolatilityPivot MQL expert advisor.
/// Tracks volatility-based pivot lines to follow or fade trend reversals.
/// </summary>
public class VolatilityPivotStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _smoothingPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _deltaPrice;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _enableBuyEntries;
	private readonly StrategyParam<bool> _enableSellEntries;
	private readonly StrategyParam<bool> _allowLongExits;
	private readonly StrategyParam<bool> _allowShortExits;
	private readonly StrategyParam<VolatilityPivotDirections> _tradeDirection;
	private readonly StrategyParam<VolatilityPivotModes> _pivotMode;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;

	private AverageTrueRange _atr = null!;
	private ExponentialMovingAverage _atrSmoother = null!;
	private readonly Queue<PivotResult> _results = new();
	private decimal? _previousStop;
	private decimal? _previousClose;
	private bool _previousUpTrendActive;
	private bool _previousDownTrendActive;
	private decimal? _entryPrice;
	private Sides? _entrySide;

	/// <summary>
	/// Directional mode for interpreting pivot signals.
	/// </summary>
	public enum VolatilityPivotDirections
	{
		/// <summary>
		/// Trade in the same direction as the pivot breakout.
		/// </summary>
		WithTrend,

		/// <summary>
		/// Trade against the pivot breakout direction (counter-trend).
		/// </summary>
		CounterTrend
	}

	/// <summary>
	/// Calculation mode for the pivot distance.
	/// </summary>
	public enum VolatilityPivotModes
	{
		/// <summary>
		/// Use ATR multiplied by a smoothing EMA.
		/// </summary>
		Atr,

		/// <summary>
		/// Use a fixed price deviation.
		/// </summary>
		PriceDeviation
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// EMA smoothing period applied to ATR values.
	/// </summary>
	public int SmoothingPeriod
	{
		get => _smoothingPeriod.Value;
		set => _smoothingPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the smoothed ATR.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Fixed price deviation for the price-based mode.
	/// </summary>
	public decimal DeltaPrice
	{
		get => _deltaPrice.Value;
		set => _deltaPrice.Value = value;
	}

	/// <summary>
	/// Number of completed bars to delay signal execution.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Enables long entries when true.
	/// </summary>
	public bool EnableBuyEntries
	{
		get => _enableBuyEntries.Value;
		set => _enableBuyEntries.Value = value;
	}

	/// <summary>
	/// Enables short entries when true.
	/// </summary>
	public bool EnableSellEntries
	{
		get => _enableSellEntries.Value;
		set => _enableSellEntries.Value = value;
	}

	/// <summary>
	/// Allows closing of existing long positions.
	/// </summary>
	public bool AllowLongExits
	{
		get => _allowLongExits.Value;
		set => _allowLongExits.Value = value;
	}

	/// <summary>
	/// Allows closing of existing short positions.
	/// </summary>
	public bool AllowShortExits
	{
		get => _allowShortExits.Value;
		set => _allowShortExits.Value = value;
	}

	/// <summary>
	/// Directional handling of pivot signals.
	/// </summary>
	public VolatilityPivotDirections TradeDirection
	{
		get => _tradeDirection.Value;
		set => _tradeDirection.Value = value;
	}

	/// <summary>
	/// Pivot calculation mode.
	/// </summary>
	public VolatilityPivotModes PivotMode
	{
		get => _pivotMode.Value;
		set => _pivotMode.Value = value;
	}

	/// <summary>
	/// Absolute stop-loss distance in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Absolute take-profit distance in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="VolatilityPivotStrategy"/> class.
	/// </summary>
	public VolatilityPivotStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for the pivot calculation", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 100)
			.SetRange(10, 400)
			.SetDisplay("ATR Period", "ATR length used in the pivot distance", "Indicator")
			.SetCanOptimize(true);

		_smoothingPeriod = Param(nameof(SmoothingPeriod), 10)
			.SetRange(1, 100)
			.SetDisplay("ATR Smoothing", "EMA length applied to ATR values", "Indicator")
			.SetCanOptimize(true);

		_atrMultiplier = Param(nameof(AtrMultiplier), 3m)
			.SetRange(0.5m, 6m)
			.SetDisplay("ATR Multiplier", "Multiplier applied to the smoothed ATR", "Indicator")
			.SetCanOptimize(true);

		_deltaPrice = Param(nameof(DeltaPrice), 0.002m)
			.SetRange(0.0001m, 1m)
			.SetDisplay("Price Deviation", "Fixed distance used in price mode", "Indicator")
			.SetCanOptimize(true);

		_signalBar = Param(nameof(SignalBar), 1)
			.SetRange(0, 5)
			.SetDisplay("Signal Bar", "Delay in bars before executing a signal", "Trading Logic");

		_enableBuyEntries = Param(nameof(EnableBuyEntries), true)
			.SetDisplay("Enable Long Entries", "Allow opening long positions", "Trading Logic");

		_enableSellEntries = Param(nameof(EnableSellEntries), true)
			.SetDisplay("Enable Short Entries", "Allow opening short positions", "Trading Logic");

		_allowLongExits = Param(nameof(AllowLongExits), true)
			.SetDisplay("Allow Long Exits", "Permit closing existing long positions", "Trading Logic");

		_allowShortExits = Param(nameof(AllowShortExits), true)
			.SetDisplay("Allow Short Exits", "Permit closing existing short positions", "Trading Logic");

		_tradeDirection = Param(nameof(TradeDirection), VolatilityPivotDirections.WithTrend)
			.SetDisplay("Trade Direction", "Follow or fade the pivot breakout", "Trading Logic");

		_pivotMode = Param(nameof(PivotMode), VolatilityPivotModes.Atr)
			.SetDisplay("Pivot Mode", "Choose ATR based or fixed deviation mode", "Indicator");

		_stopLoss = Param(nameof(StopLoss), 0m)
			.SetRange(0m, 10m)
			.SetDisplay("Stop Loss", "Absolute stop-loss distance", "Risk Management");

		_takeProfit = Param(nameof(TakeProfit), 0m)
			.SetRange(0m, 10m)
			.SetDisplay("Take Profit", "Absolute take-profit distance", "Risk Management");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_results.Clear();
		_previousStop = null;
		_previousClose = null;
		_previousUpTrendActive = false;
		_previousDownTrendActive = false;
		_entryPrice = null;
		_entrySide = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange { Length = AtrPeriod };
		_atrSmoother = new ExponentialMovingAverage { Length = SmoothingPeriod };

		_previousStop = null;
		_previousClose = null;
		_previousUpTrendActive = false;
		_previousDownTrendActive = false;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var previousClose = _previousClose ?? candle.ClosePrice;
		var deltaStop = CalculateDeltaStop(atrValue);

		if (_previousStop == null)
			_previousStop = previousClose;

		CheckProtectiveLevels(candle);

		if (deltaStop == null)
		{
			_previousClose = candle.ClosePrice;
			return;
		}

		var result = ComputePivot(candle, deltaStop.Value, previousClose);
		EnqueueResult(result);

		var canTrade = IsFormedAndOnlineAndAllowTrading();
		if (canTrade && TryGetShiftedResult(out var shifted))
		{
			ExecuteTrading(shifted, candle);
		}

		_previousClose = candle.ClosePrice;
	}

	private decimal? CalculateDeltaStop(IIndicatorValue atrValue)
	{
		if (PivotMode == VolatilityPivotModes.PriceDeviation)
			return DeltaPrice;

		if (!atrValue.IsFinal)
			return null;

		var smoothed = _atrSmoother.Process(atrValue);
		if (!smoothed.IsFinal)
			return null;

		var atr = smoothed.GetValue<decimal>();
		return atr * AtrMultiplier;
	}

	private PivotResult ComputePivot(ICandleMessage candle, decimal deltaStop, decimal previousClose)
	{
		var previousStop = _previousStop ?? previousClose;
		var currentClose = candle.ClosePrice;
		var stop = CalculateStop(currentClose, previousClose, previousStop, deltaStop);

		var upTrendActive = currentClose > stop;
		var downTrendActive = currentClose < stop;
		var upSignal = upTrendActive && !_previousUpTrendActive;
		var downSignal = downTrendActive && !_previousDownTrendActive;

		_previousUpTrendActive = upTrendActive;
		_previousDownTrendActive = downTrendActive;
		_previousStop = stop;

		return new PivotResult(
			candle.CloseTime,
			upSignal,
			downSignal,
			upTrendActive,
			downTrendActive,
			upTrendActive ? stop : (decimal?)null,
			downTrendActive ? stop : (decimal?)null);
	}

	private void EnqueueResult(PivotResult result)
	{
		_results.Enqueue(result);

		var maxSize = Math.Max(SignalBar + 2, 8);
		while (_results.Count > maxSize)
			_results.Dequeue();
	}

	private bool TryGetShiftedResult(out PivotResult result)
	{
		result = default;

		if (_results.Count == 0)
			return false;

		if (_results.Count <= SignalBar)
			return false;

		var targetIndex = _results.Count - 1 - SignalBar;
		var index = 0;
		foreach (var item in _results)
		{
			if (index == targetIndex)
			{
				result = item;
				return true;
			}

			index++;
		}

		return false;
	}

	private void ExecuteTrading(PivotResult pivot, ICandleMessage candle)
	{
		var upSignal = pivot.UpSignal;
		var downSignal = pivot.DownSignal;
		var upTrendActive = pivot.UpTrendActive;
		var downTrendActive = pivot.DownTrendActive;
		var upTrendPrice = pivot.UpTrendPrice;
		var downTrendPrice = pivot.DownTrendPrice;

		if (TradeDirection == VolatilityPivotDirections.CounterTrend)
		{
			upSignal = pivot.DownSignal;
			downSignal = pivot.UpSignal;
			upTrendActive = pivot.DownTrendActive;
			downTrendActive = pivot.UpTrendActive;
			upTrendPrice = pivot.DownTrendPrice;
			downTrendPrice = pivot.UpTrendPrice;
		}

		var openLong = false;
		var openShort = false;
		var closeLong = false;
		var closeShort = false;

		if (upSignal)
		{
			if (EnableBuyEntries)
				openLong = true;
			if (AllowShortExits)
				closeShort = true;
		}
		else if (upTrendActive)
		{
			if (AllowShortExits)
				closeShort = true;
		}

		if (downSignal)
		{
			if (EnableSellEntries)
				openShort = true;
			if (AllowLongExits)
				closeLong = true;
		}
		else if (downTrendActive)
		{
			if (AllowLongExits)
				closeLong = true;
		}

		var targetPosition = Position;

		if (openLong)
			targetPosition = Volume;
		else if (openShort)
			targetPosition = -Volume;
		else
		{
			if (closeLong && Position > 0)
				targetPosition = 0m;
			if (closeShort && Position < 0)
				targetPosition = 0m;
		}

		if (targetPosition == Position)
			return;

		var reason = BuildReason(openLong, openShort, closeLong, closeShort, upSignal, downSignal, upTrendActive, downTrendActive);
		AdjustPosition(targetPosition, candle.ClosePrice, reason);

		if (targetPosition > 0m)
		{
			_entrySide = Sides.Buy;
			_entryPrice = candle.ClosePrice;
		}
		else if (targetPosition < 0m)
		{
			_entrySide = Sides.Sell;
			_entryPrice = candle.ClosePrice;
		}
		else
		{
			_entrySide = null;
			_entryPrice = null;
		}
	}

	private void AdjustPosition(decimal targetPosition, decimal price, string reason)
	{
		var delta = targetPosition - Position;
		if (delta == 0m)
			return;

		if (delta > 0m)
		{
			BuyMarket(delta);
			LogInfo($"{reason}: buying {delta} at {price:F4} to reach {targetPosition}.");
		}
		else
		{
			var volume = Math.Abs(delta);
			SellMarket(volume);
			LogInfo($"{reason}: selling {volume} at {price:F4} to reach {targetPosition}.");
		}
	}

	private void CheckProtectiveLevels(ICandleMessage candle)
	{
		if (_entrySide == null || _entryPrice == null || Position == 0m)
			return;

		if (StopLoss <= 0m && TakeProfit <= 0m)
			return;

		if (_entrySide == Sides.Buy)
		{
			var stopPrice = StopLoss > 0m ? _entryPrice.Value - StopLoss : (decimal?)null;
			var takePrice = TakeProfit > 0m ? _entryPrice.Value + TakeProfit : (decimal?)null;

			if (stopPrice != null && candle.LowPrice <= stopPrice.Value)
			{
				AdjustPosition(0m, stopPrice.Value, "Long stop-loss triggered");
				_entrySide = null;
				_entryPrice = null;
				return;
			}

			if (takePrice != null && candle.HighPrice >= takePrice.Value)
			{
				AdjustPosition(0m, takePrice.Value, "Long take-profit triggered");
				_entrySide = null;
				_entryPrice = null;
				return;
			}
		}
		else if (_entrySide == Sides.Sell)
		{
			var stopPrice = StopLoss > 0m ? _entryPrice.Value + StopLoss : (decimal?)null;
			var takePrice = TakeProfit > 0m ? _entryPrice.Value - TakeProfit : (decimal?)null;

			if (stopPrice != null && candle.HighPrice >= stopPrice.Value)
			{
				AdjustPosition(0m, stopPrice.Value, "Short stop-loss triggered");
				_entrySide = null;
				_entryPrice = null;
				return;
			}

			if (takePrice != null && candle.LowPrice <= takePrice.Value)
			{
				AdjustPosition(0m, takePrice.Value, "Short take-profit triggered");
				_entrySide = null;
				_entryPrice = null;
				return;
			}
		}
	}

	private static decimal CalculateStop(decimal currentClose, decimal previousClose, decimal previousStop, decimal deltaStop)
	{
		if (currentClose == previousStop)
			return previousStop;

		if (previousClose < previousStop && currentClose < previousStop)
			return Math.Min(previousStop, currentClose + deltaStop);

		if (previousClose > previousStop && currentClose > previousStop)
			return Math.Max(previousStop, currentClose - deltaStop);

		return currentClose > previousStop
			? currentClose - deltaStop
			: currentClose + deltaStop;
	}

	private static string BuildReason(bool openLong, bool openShort, bool closeLong, bool closeShort, bool upSignal, bool downSignal, bool upTrendActive, bool downTrendActive)
	{
		if (openLong && upSignal)
			return "Long entry after bullish pivot";
		if (openShort && downSignal)
			return "Short entry after bearish pivot";
		if (closeLong && downSignal)
			return "Closing long due to bearish pivot";
		if (closeShort && upSignal)
			return "Closing short due to bullish pivot";
		if (closeLong && downTrendActive)
			return "Closing long while bearish trail is active";
		if (closeShort && upTrendActive)
			return "Closing short while bullish trail is active";
		return "Adjusting position";
	}

	private readonly struct PivotResult
	{
		public PivotResult(DateTimeOffset time, bool upSignal, bool downSignal, bool upTrendActive, bool downTrendActive, decimal? upTrendPrice, decimal? downTrendPrice)
		{
			Time = time;
			UpSignal = upSignal;
			DownSignal = downSignal;
			UpTrendActive = upTrendActive;
			DownTrendActive = downTrendActive;
			UpTrendPrice = upTrendPrice;
			DownTrendPrice = downTrendPrice;
		}

		public DateTimeOffset Time { get; }
		public bool UpSignal { get; }
		public bool DownSignal { get; }
		public bool UpTrendActive { get; }
		public bool DownTrendActive { get; }
		public decimal? UpTrendPrice { get; }
		public decimal? DownTrendPrice { get; }
	}
}

