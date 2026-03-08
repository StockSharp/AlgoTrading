using System;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with volume spike confirmation.
/// </summary>
public class SyndicateTraderStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _volumeMaLength;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<bool> _useSessionFilter;
	private readonly StrategyParam<int> _sessionStartHour;
	private readonly StrategyParam<int> _sessionStartMinute;
	private readonly StrategyParam<int> _sessionEndHour;
	private readonly StrategyParam<int> _sessionEndMinute;
	private readonly StrategyParam<DataType> _candleType;

	private readonly ExponentialMovingAverage _fastEma = new();
	private readonly ExponentialMovingAverage _slowEma = new();
	private readonly SimpleMovingAverage _volumeMa = new();
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isInitialized;
	private int _barsSinceTrade;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	/// <summary>
	/// Volume moving average length.
	/// </summary>
	public int VolumeMaLength
	{
		get => _volumeMaLength.Value;
		set => _volumeMaLength.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the volume average.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	/// <summary>
	/// Take profit in price points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss in price points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Bars to wait after a completed trade.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Enable session filtering.
	/// </summary>
	public bool UseSessionFilter
	{
		get => _useSessionFilter.Value;
		set => _useSessionFilter.Value = value;
	}

	/// <summary>
	/// Session start hour.
	/// </summary>
	public int SessionStartHour
	{
		get => _sessionStartHour.Value;
		set => _sessionStartHour.Value = value;
	}

	/// <summary>
	/// Session start minute.
	/// </summary>
	public int SessionStartMinute
	{
		get => _sessionStartMinute.Value;
		set => _sessionStartMinute.Value = value;
	}

	/// <summary>
	/// Session end hour.
	/// </summary>
	public int SessionEndHour
	{
		get => _sessionEndHour.Value;
		set => _sessionEndHour.Value = value;
	}

	/// <summary>
	/// Session end minute.
	/// </summary>
	public int SessionEndMinute
	{
		get => _sessionEndMinute.Value;
		set => _sessionEndMinute.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public SyndicateTraderStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length", "General");

		_slowEmaLength = Param(nameof(SlowEmaLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length", "General");

		_volumeMaLength = Param(nameof(VolumeMaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume MA", "Volume MA length", "General");

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 1.8m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Mult", "Volume spike multiplier", "General");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 1200m)
			.SetDisplay("Take Profit", "Take profit in price points", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 700m)
			.SetDisplay("Stop Loss", "Stop loss in price points", "Risk");

		_cooldownBars = Param(nameof(CooldownBars), 2)
			.SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Risk");

		_useSessionFilter = Param(nameof(UseSessionFilter), false)
			.SetDisplay("Use Session", "Enable session filter", "Session");

		_sessionStartHour = Param(nameof(SessionStartHour), 0)
			.SetDisplay("Start Hour", "Session start hour", "Session");

		_sessionStartMinute = Param(nameof(SessionStartMinute), 0)
			.SetDisplay("Start Minute", "Session start minute", "Session");

		_sessionEndHour = Param(nameof(SessionEndHour), 23)
			.SetDisplay("End Hour", "Session end hour", "Session");

		_sessionEndMinute = Param(nameof(SessionEndMinute), 59)
			.SetDisplay("End Minute", "Session end minute", "Session");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_fastEma.Length = FastEmaLength;
		_slowEma.Length = SlowEmaLength;
		_volumeMa.Length = VolumeMaLength;
		_fastEma.Reset();
		_slowEma.Reset();
		_volumeMa.Reset();
		_prevFast = 0m;
		_prevSlow = 0m;
		_isInitialized = false;
		_barsSinceTrade = CooldownBars;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastEma.Length = FastEmaLength;
		_slowEma.Length = SlowEmaLength;
		_volumeMa.Length = VolumeMaLength;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPoints, UnitTypes.Absolute),
			stopLoss: new Unit(StopLossPoints, UnitTypes.Absolute));

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (UseSessionFilter)
		{
			var time = candle.OpenTime.TimeOfDay;
			var start = new TimeSpan(SessionStartHour, SessionStartMinute, 0);
			var end = new TimeSpan(SessionEndHour, SessionEndMinute, 0);

			if (time < start || time > end)
				return;
		}

		var fast = _fastEma.Process(new DecimalIndicatorValue(_fastEma, candle.ClosePrice, candle.OpenTime) { IsFinal = true }).ToDecimal();
		var slow = _slowEma.Process(new DecimalIndicatorValue(_slowEma, candle.ClosePrice, candle.OpenTime) { IsFinal = true }).ToDecimal();
		var volumeAvg = _volumeMa.Process(new DecimalIndicatorValue(_volumeMa, candle.TotalVolume, candle.OpenTime) { IsFinal = true }).ToDecimal();

		if (!_fastEma.IsFormed || !_slowEma.IsFormed || !_volumeMa.IsFormed)
			return;

		if (_barsSinceTrade < CooldownBars)
			_barsSinceTrade++;

		if (!_isInitialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_isInitialized = true;
			return;
		}

		var crossUp = fast > slow && _prevFast <= _prevSlow;
		var crossDown = fast < slow && _prevFast >= _prevSlow;
		var hasVolumeSpike = candle.TotalVolume >= volumeAvg * VolumeMultiplier;

		if (_barsSinceTrade >= CooldownBars && hasVolumeSpike)
		{
			if (crossUp && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_barsSinceTrade = 0;
			}
			else if (crossDown && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_barsSinceTrade = 0;
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
