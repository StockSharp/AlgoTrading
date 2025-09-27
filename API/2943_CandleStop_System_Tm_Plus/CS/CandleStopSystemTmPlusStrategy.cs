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
using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;



/// <summary>
/// CandleStop breakout system with trailing high/low channels and optional time-based exits.
/// </summary>
public class CandleStopSystemTmPlusStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<bool> _enableLongEntry;
	private readonly StrategyParam<bool> _enableShortEntry;
	private readonly StrategyParam<bool> _closeLongOnBearishBreak;
	private readonly StrategyParam<bool> _closeShortOnBullishBreak;
	private readonly StrategyParam<bool> _enableTimeExit;
	private readonly StrategyParam<int> _maxPositionMinutes;
	private readonly StrategyParam<int> _upTrailPeriods;
	private readonly StrategyParam<int> _upTrailShift;
	private readonly StrategyParam<int> _downTrailPeriods;
	private readonly StrategyParam<int> _downTrailShift;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _upHighest = null!;
	private Lowest _downLowest = null!;
	private Shift? _upShiftIndicator;
	private Shift? _downShiftIndicator;
	private readonly List<int> _colorHistory = new();
	private int _historyCapacity;
	private DateTimeOffset? _positionOpenTime;

	/// <summary>
	/// Volume for each market order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Enable opening long positions.
	/// </summary>
	public bool EnableLongEntry
	{
		get => _enableLongEntry.Value;
		set => _enableLongEntry.Value = value;
	}

	/// <summary>
	/// Enable opening short positions.
	/// </summary>
	public bool EnableShortEntry
	{
		get => _enableShortEntry.Value;
		set => _enableShortEntry.Value = value;
	}

	/// <summary>
	/// Close existing longs on a bearish CandleStop breakout.
	/// </summary>
	public bool CloseLongOnBearishBreak
	{
		get => _closeLongOnBearishBreak.Value;
		set => _closeLongOnBearishBreak.Value = value;
	}

	/// <summary>
	/// Close existing shorts on a bullish CandleStop breakout.
	/// </summary>
	public bool CloseShortOnBullishBreak
	{
		get => _closeShortOnBullishBreak.Value;
		set => _closeShortOnBullishBreak.Value = value;
	}

	/// <summary>
	/// Enable time-based exits.
	/// </summary>
	public bool EnableTimeExit
	{
		get => _enableTimeExit.Value;
		set => _enableTimeExit.Value = value;
	}

	/// <summary>
	/// Maximum holding time in minutes.
	/// </summary>
	public int MaxPositionMinutes
	{
		get => _maxPositionMinutes.Value;
		set => _maxPositionMinutes.Value = value;
	}

	/// <summary>
	/// Lookback for the bullish CandleStop channel.
	/// </summary>
	public int UpTrailPeriods
	{
		get => _upTrailPeriods.Value;
		set => _upTrailPeriods.Value = value;
	}

	/// <summary>
	/// Shift applied to the bullish channel.
	/// </summary>
	public int UpTrailShift
	{
		get => _upTrailShift.Value;
		set => _upTrailShift.Value = value;
	}

	/// <summary>
	/// Lookback for the bearish CandleStop channel.
	/// </summary>
	public int DownTrailPeriods
	{
		get => _downTrailPeriods.Value;
		set => _downTrailPeriods.Value = value;
	}

	/// <summary>
	/// Shift applied to the bearish channel.
	/// </summary>
	public int DownTrailShift
	{
		get => _downTrailShift.Value;
		set => _downTrailShift.Value = value;
	}

	/// <summary>
	/// Bar index used for signal confirmation.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="CandleStopSystemTmPlusStrategy"/>.
	/// </summary>
	public CandleStopSystemTmPlusStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Size of each executed order", "General")
		.SetCanOptimize(true);

		_enableLongEntry = Param(nameof(EnableLongEntry), true)
		.SetDisplay("Enable Long", "Allow long entries", "Signals");

		_enableShortEntry = Param(nameof(EnableShortEntry), true)
		.SetDisplay("Enable Short", "Allow short entries", "Signals");

		_closeLongOnBearishBreak = Param(nameof(CloseLongOnBearishBreak), true)
		.SetDisplay("Close Long On Bearish Break", "Exit longs on bearish CandleStop breakouts", "Signals");

		_closeShortOnBullishBreak = Param(nameof(CloseShortOnBullishBreak), true)
		.SetDisplay("Close Short On Bullish Break", "Exit shorts on bullish CandleStop breakouts", "Signals");

		_enableTimeExit = Param(nameof(EnableTimeExit), true)
		.SetDisplay("Enable Time Exit", "Use maximum position lifetime filter", "Risk");

		_maxPositionMinutes = Param(nameof(MaxPositionMinutes), 1920)
		.SetNotNegative()
		.SetDisplay("Max Position Minutes", "Maximum holding time in minutes", "Risk")
		.SetCanOptimize(true);

		_upTrailPeriods = Param(nameof(UpTrailPeriods), 5)
		.SetGreaterThanZero()
		.SetDisplay("Upper Lookback", "Bars for upper CandleStop channel", "Channels")
		.SetCanOptimize(true);

		_upTrailShift = Param(nameof(UpTrailShift), 5)
		.SetNotNegative()
		.SetDisplay("Upper Shift", "Offset for upper channel evaluation", "Channels")
		.SetCanOptimize(true);

		_downTrailPeriods = Param(nameof(DownTrailPeriods), 5)
		.SetGreaterThanZero()
		.SetDisplay("Lower Lookback", "Bars for lower CandleStop channel", "Channels")
		.SetCanOptimize(true);

		_downTrailShift = Param(nameof(DownTrailShift), 5)
		.SetNotNegative()
		.SetDisplay("Lower Shift", "Offset for lower channel evaluation", "Channels")
		.SetCanOptimize(true);

		_signalBar = Param(nameof(SignalBar), 1)
		.SetNotNegative()
		.SetDisplay("Signal Bar", "Index of the bar used for entries", "Signals")
		.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
		.SetNotNegative()
		.SetDisplay("Stop Loss Points", "Stop loss distance in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
		.SetNotNegative()
		.SetDisplay("Take Profit Points", "Take profit distance in price steps", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe", "General");
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

		_colorHistory.Clear();
		_historyCapacity = Math.Max(SignalBar + 2, 2);
		_positionOpenTime = null;
		_upShiftIndicator = null;
		_downShiftIndicator = null;
		Volume = OrderVolume;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_upHighest = new Highest { Length = UpTrailPeriods };
		_downLowest = new Lowest { Length = DownTrailPeriods };
		_upShiftIndicator = UpTrailShift > 0 ? new Shift { Length = UpTrailShift } : null;
		_downShiftIndicator = DownTrailShift > 0 ? new Shift { Length = DownTrailShift } : null;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(
			new Unit(TakeProfitPoints, UnitTypes.Step),
			new Unit(StopLossPoints, UnitTypes.Step),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var upperRaw = _upHighest.Process(candle).ToDecimal();
		var lowerRaw = _downLowest.Process(candle).ToDecimal();

		if (!_upHighest.IsFormed || !_downLowest.IsFormed)
			return;

		if (_upShiftIndicator != null)
		{
			var shifted = _upShiftIndicator.Process(upperRaw, candle.OpenTime, true);
			if (!_upShiftIndicator.IsFormed)
				return;

			upperRaw = shifted.GetValue<decimal>();
		}

		if (_downShiftIndicator != null)
		{
			var shifted = _downShiftIndicator.Process(lowerRaw, candle.OpenTime, true);
			if (!_downShiftIndicator.IsFormed)
				return;

			lowerRaw = shifted.GetValue<decimal>();
		}

		var upperLevel = upperRaw;
		var lowerLevel = lowerRaw;

		var color = 4;

		if (candle.ClosePrice > upperLevel)
		{
			color = candle.ClosePrice >= candle.OpenPrice ? 3 : 2;
		}
		else if (candle.ClosePrice < lowerLevel)
		{
			color = candle.ClosePrice <= candle.OpenPrice ? 0 : 1;
		}

		UpdateColorHistory(color);

		if (_colorHistory.Count <= SignalBar + 1)
			return;

		var signalColor = _colorHistory[SignalBar];
		var confirmColor = _colorHistory[SignalBar + 1];

		var bullishBreakout = confirmColor == 2 || confirmColor == 3;
		var bearishBreakout = confirmColor == 0 || confirmColor == 1;

		var longEntrySignal = EnableLongEntry && bullishBreakout && signalColor != 2 && signalColor != 3;
		var shortEntrySignal = EnableShortEntry && bearishBreakout && signalColor != 0 && signalColor != 1;
		var longExitSignal = CloseLongOnBearishBreak && bearishBreakout;
		var shortExitSignal = CloseShortOnBullishBreak && bullishBreakout;

		HandleTimeExit(candle);

		var position = Position;

		if (position > 0m && longExitSignal)
		{
			SellMarket(position);
			_positionOpenTime = null;
			position = 0m;
		}

		position = Position;

		if (position < 0m && shortExitSignal)
		{
			BuyMarket(Math.Abs(position));
			_positionOpenTime = null;
			position = 0m;
		}

		position = Position;

		var volume = OrderVolume;
		var candleTime = candle.CloseTime != default ? candle.CloseTime : candle.OpenTime;

		if (longEntrySignal && volume > 0m && position <= 0m)
		{
			if (position < 0m)
			{
				BuyMarket(Math.Abs(position));
			}

			BuyMarket(volume);
			_positionOpenTime = candleTime;
		}
		else if (shortEntrySignal && volume > 0m && position >= 0m)
		{
			if (position > 0m)
			{
				SellMarket(position);
			}

			SellMarket(volume);
			_positionOpenTime = candleTime;
		}
	}

	private void HandleTimeExit(ICandleMessage candle)
	{
		if (!EnableTimeExit)
			return;

		if (!_positionOpenTime.HasValue)
			return;

		if (Position == 0m)
		{
			_positionOpenTime = null;
			return;
		}

		if (MaxPositionMinutes <= 0)
			return;

		var lifetime = TimeSpan.FromMinutes(MaxPositionMinutes);
		var candleTime = candle.CloseTime != default ? candle.CloseTime : candle.OpenTime;

		if (candleTime - _positionOpenTime.Value < lifetime)
			return;

		if (Position > 0m)
		{
			SellMarket(Position);
		}
		else if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
		}

		_positionOpenTime = null;
	}

	private void UpdateColorHistory(int color)
	{
		if (_colorHistory.Count == _historyCapacity)
			_colorHistory.RemoveAt(_colorHistory.Count - 1);

		_colorHistory.Insert(0, color);
	}
}
