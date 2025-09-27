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
/// Implements the Exp_TimeZonePivotsOpenSystem MetaTrader strategy using StockSharp's high level API.
/// The strategy anchors a symmetric price channel to the daily opening price at a configurable hour
/// and reacts when closed candles break above or below that band.
/// </summary>
public class TimeZonePivotsOpenSystemStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<decimal> _offsetPoints;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _enableLongEntry;
	private readonly StrategyParam<bool> _enableShortEntry;
	private readonly StrategyParam<bool> _closeLongOnBearishBreak;
	private readonly StrategyParam<bool> _closeShortOnBullishBreak;

	private decimal _priceStep;
	private decimal _offsetDistance;
	private decimal? _anchorPrice;
	private DateTime? _anchorDate;
	private decimal _upperZone;
	private decimal _lowerZone;
	private TimeSpan _candleSpan;
	private DateTimeOffset? _nextLongTradeTime;
	private DateTimeOffset? _nextShortTradeTime;

	private readonly List<SignalRecord> _signalHistory = new();

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public TimeZonePivotsOpenSystemStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle type", "Timeframe that feeds the Time Zone Pivots logic.", "General");

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetNotNegative()
			.SetDisplay("Order volume", "Volume used when opening a new position.", "Trading");

		_startHour = Param(nameof(StartHour), 0)
			.SetNotNegative()
			.SetDisplay("Start hour", "Hour (0-23) whose opening price anchors the bands.", "Indicator");

		_offsetPoints = Param(nameof(OffsetPoints), 100m)
			.SetNotNegative()
			.SetDisplay("Offset (points)", "Distance from the anchor price expressed in price steps.", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
			.SetNotNegative()
			.SetDisplay("Signal bar", "Shift of the confirmation candle used to trigger trades.", "Signals");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
			.SetNotNegative()
			.SetDisplay("Stop loss (points)", "Protective stop distance in price steps.", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
			.SetNotNegative()
			.SetDisplay("Take profit (points)", "Profit target distance in price steps.", "Risk");

		_enableLongEntry = Param(nameof(EnableLongEntry), true)
			.SetDisplay("Enable long entries", "Allow opening long positions after bullish breakouts.", "Signals");

		_enableShortEntry = Param(nameof(EnableShortEntry), true)
			.SetDisplay("Enable short entries", "Allow opening short positions after bearish breakouts.", "Signals");

		_closeLongOnBearishBreak = Param(nameof(CloseLongOnBearishBreak), true)
			.SetDisplay("Close longs on bearish break", "Exit long trades when price falls below the lower band.", "Risk");

		_closeShortOnBullishBreak = Param(nameof(CloseShortOnBullishBreak), true)
			.SetDisplay("Close shorts on bullish break", "Exit short trades when price rallies above the upper band.", "Risk");
	}

	/// <summary>
	/// Candle type that defines the working timeframe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Volume sent with each new position.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Hour of the day used to anchor the pivot bands.
	/// </summary>
	public int StartHour
	{
		get => ClampHour(_startHour.Value);
		set => _startHour.Value = ClampHour(value);
	}

	/// <summary>
	/// Offset from the anchor price expressed in price steps.
	/// </summary>
	public decimal OffsetPoints
	{
		get => _offsetPoints.Value;
		set => _offsetPoints.Value = value;
	}

	/// <summary>
	/// Number of closed candles between the current bar and the breakout confirmation bar.
	/// </summary>
	public int SignalBar
	{
		get => Math.Max(1, _signalBar.Value);
		set => _signalBar.Value = Math.Max(1, value);
	}

	/// <summary>
	/// Stop loss distance measured in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance measured in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Enables long position entries after bullish breakouts.
	/// </summary>
	public bool EnableLongEntry
	{
		get => _enableLongEntry.Value;
		set => _enableLongEntry.Value = value;
	}

	/// <summary>
	/// Enables short position entries after bearish breakouts.
	/// </summary>
	public bool EnableShortEntry
	{
		get => _enableShortEntry.Value;
		set => _enableShortEntry.Value = value;
	}

	/// <summary>
	/// Enables closing long positions when bearish breakouts occur.
	/// </summary>
	public bool CloseLongOnBearishBreak
	{
		get => _closeLongOnBearishBreak.Value;
		set => _closeLongOnBearishBreak.Value = value;
	}

	/// <summary>
	/// Enables closing short positions when bullish breakouts occur.
	/// </summary>
	public bool CloseShortOnBullishBreak
	{
		get => _closeShortOnBullishBreak.Value;
		set => _closeShortOnBullishBreak.Value = value;
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

		_anchorPrice = null;
		_anchorDate = null;
		_upperZone = 0m;
		_lowerZone = 0m;
		_offsetDistance = 0m;
		_signalHistory.Clear();
		_nextLongTradeTime = null;
		_nextShortTradeTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		_priceStep = Security?.PriceStep ?? 0m;
		if (_priceStep <= 0m)
		{
			_priceStep = 1m;
		}

		_candleSpan = CandleType.Arg is TimeSpan span && span > TimeSpan.Zero
			? span
			: TimeSpan.FromHours(1);

		_offsetDistance = OffsetPoints * _priceStep;

		var stopLossDistance = StopLossPoints * _priceStep;
		var takeProfitDistance = TakeProfitPoints * _priceStep;

		if (stopLossDistance > 0m || takeProfitDistance > 0m)
		{
			StartProtection(
				stopLoss: stopLossDistance > 0m ? new Unit(stopLossDistance, UnitTypes.Absolute) : null,
				takeProfit: takeProfitDistance > 0m ? new Unit(takeProfitDistance, UnitTypes.Absolute) : null,
				useMarketOrders: true);
		}

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

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

		_offsetDistance = OffsetPoints * _priceStep;
		Volume = OrderVolume;

		UpdateAnchor(candle);

		var signal = CalculateSignal(candle);
		RecordSignal(candle.OpenTime, signal);

		if (_signalHistory.Count <= SignalBar)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var confirmIndex = SignalBar;
		var currentIndex = SignalBar - 1;

		if (currentIndex < 0 || confirmIndex >= _signalHistory.Count)
			return;

		var currentSignal = _signalHistory[currentIndex];
		var confirmSignal = _signalHistory[confirmIndex];

		var bullishBreakout = confirmSignal.Signal <= 1;
		var bearishBreakout = confirmSignal.Signal >= 3;

		var position = Position;

		if (position > 0m && bearishBreakout && CloseLongOnBearishBreak)
		{
			SellMarket(position);
			position = Position;
		}

		if (position < 0m && bullishBreakout && CloseShortOnBullishBreak)
		{
			BuyMarket(Math.Abs(position));
			position = Position;
		}

		var volume = OrderVolume;
		if (volume <= 0m)
			return;

		var signalTime = confirmSignal.OpenTime + _candleSpan;
		var candleTime = candle.CloseTime != default ? candle.CloseTime : candle.OpenTime;

		if (EnableLongEntry && bullishBreakout && currentSignal.Signal > 1 && position == 0m)
		{
			if (!_nextLongTradeTime.HasValue || candleTime >= _nextLongTradeTime.Value)
			{
				BuyMarket(volume);
				_nextLongTradeTime = signalTime;
			}
		}

		if (EnableShortEntry && bearishBreakout && currentSignal.Signal < 3 && position == 0m)
		{
			if (!_nextShortTradeTime.HasValue || candleTime >= _nextShortTradeTime.Value)
			{
				SellMarket(volume);
				_nextShortTradeTime = signalTime;
			}
		}
	}

	private void UpdateAnchor(ICandleMessage candle)
	{
		var candleDate = candle.OpenTime.Date;
		var hour = candle.OpenTime.Hour;

		if (hour == StartHour && (_anchorDate == null || _anchorDate.Value != candleDate))
		{
			_anchorDate = candleDate;
			_anchorPrice = candle.OpenPrice;
		}

		if (_anchorPrice.HasValue)
		{
			_upperZone = _anchorPrice.Value + _offsetDistance;
			_lowerZone = _anchorPrice.Value - _offsetDistance;
		}
	}

	private int CalculateSignal(ICandleMessage candle)
	{
		if (!_anchorPrice.HasValue)
			return 2;

		var close = candle.ClosePrice;
		var open = candle.OpenPrice;

		if (close > _upperZone)
			return close >= open ? 0 : 1;

		if (close < _lowerZone)
			return close <= open ? 4 : 3;

		return 2;
	}

	private void RecordSignal(DateTimeOffset time, int signal)
	{
		_signalHistory.Insert(0, new SignalRecord(signal, time));

		var maxCapacity = Math.Max(SignalBar + 2, 4);
		if (_signalHistory.Count > maxCapacity)
		{
			_signalHistory.RemoveRange(maxCapacity, _signalHistory.Count - maxCapacity);
		}
	}

	private static int ClampHour(int hour)
	{
		if (hour < 0)
			return 0;

		if (hour > 23)
			return 23;

		return hour;
	}

	private sealed class SignalRecord
	{
		public SignalRecord(int signal, DateTimeOffset openTime)
		{
			Signal = signal;
			OpenTime = openTime;
		}

		public int Signal { get; }

		public DateTimeOffset OpenTime { get; }
	}
}

