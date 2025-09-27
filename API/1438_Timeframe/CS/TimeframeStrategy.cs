using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with basic timeframe filters.
/// </summary>
public class TimeframeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _trailingPercent;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<bool> _useTrailing;
	private readonly StrategyParam<DataType> _candleType;

	private int _cooldownCounter;
	private decimal _prevEma9;
	private decimal _prevEma20;

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Trailing stop percentage.
	/// </summary>
	public decimal TrailingPercent
	{
		get => _trailingPercent.Value;
		set => _trailingPercent.Value = value;
	}

	/// <summary>
	/// Start hour (UTC).
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// End hour (UTC).
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Cooldown bars.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Use trailing stop.
	/// </summary>
	public bool UseTrailing
	{
		get => _useTrailing.Value;
		set => _useTrailing.Value = value;
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
	/// Initialize the strategy.
	/// </summary>
	public TimeframeStrategy()
	{
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 1.5m)
			.SetDisplay("Take Profit %", "Percentage take profit", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3m, 0.5m);

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetDisplay("Stop Loss %", "Percentage stop loss", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 2m, 0.5m);

		_trailingPercent = Param(nameof(TrailingPercent), 0.5m)
			.SetDisplay("Trailing %", "Trailing stop percent", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.2m, 1m, 0.2m);

		_startHour = Param(nameof(StartHour), 15)
			.SetDisplay("Start Hour", "Hour to start trading (UTC)", "Time");

		_endHour = Param(nameof(EndHour), 20)
			.SetDisplay("End Hour", "Hour to stop trading (UTC)", "Time");

		_cooldownBars = Param(nameof(CooldownBars), 5)
			.SetDisplay("Cooldown Bars", "Bars to wait after a trade", "General");

		_useTrailing = Param(nameof(UseTrailing), true)
			.SetDisplay("Use Trailing", "Enable trailing stop", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_cooldownCounter = int.MaxValue;
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
		_cooldownCounter = int.MaxValue;
		_prevEma9 = default;
		_prevEma20 = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ema9 = new ExponentialMovingAverage { Length = 9 };
		var ema20 = new ExponentialMovingAverage { Length = 20 };
		var ema50 = new ExponentialMovingAverage { Length = 50 };
		var ema200 = new ExponentialMovingAverage { Length = 200 };
		var rsi = new RelativeStrengthIndex { Length = 14 };
		var adx = new AverageDirectionalIndex { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ema9, ema20, ema50, ema200, rsi, adx, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPercent / 100m, UnitTypes.Percent),
			stopLoss: new Unit(UseTrailing ? TrailingPercent / 100m : StopLossPercent / 100m, UnitTypes.Percent),
			isStopTrailing: UseTrailing);
	}

	private void ProcessCandle(
		ICandleMessage candle,
		IIndicatorValue ema9Value,
		IIndicatorValue ema20Value,
		IIndicatorValue ema50Value,
		IIndicatorValue ema200Value,
		IIndicatorValue rsiValue,
		IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!ema9Value.IsFinal || !ema20Value.IsFinal || !ema50Value.IsFinal || !ema200Value.IsFinal || !rsiValue.IsFinal || !adxValue.IsFinal)
			return;

		var ema9 = ema9Value.ToDecimal();
		var ema20 = ema20Value.ToDecimal();
		var ema50 = ema50Value.ToDecimal();
		var ema200 = ema200Value.ToDecimal();
		var rsi = rsiValue.ToDecimal();

		var adxTyped = (AverageDirectionalIndexValue)adxValue;
		if (adxTyped.MovingAverage is not decimal adx)
			return;

		var hour = candle.OpenTime.Hour;
		var timeFilter = hour >= StartHour && hour < EndHour;

		var longSignal = _prevEma9 <= _prevEma20 && ema9 > ema20 && ema50 > ema200;
		var shortSignal = _prevEma9 >= _prevEma20 && ema9 < ema20 && ema50 < ema200;

		var rsiLong = rsi > 50m;
		var rsiShort = rsi < 50m;

		var cooldownOk = _cooldownCounter >= CooldownBars;
		var adxFilter = adx > 15m;

		if (longSignal && rsiLong && timeFilter && adxFilter && cooldownOk && Position <= 0)
		{
			BuyMarket();
			_cooldownCounter = 0;
		}
		else if (shortSignal && rsiShort && timeFilter && adxFilter && cooldownOk && Position >= 0)
		{
			SellMarket();
			_cooldownCounter = 0;
		}

		if (_cooldownCounter < int.MaxValue)
			_cooldownCounter++;

		_prevEma9 = ema9;
		_prevEma20 = ema20;
	}
}
