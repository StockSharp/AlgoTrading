
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ATR Sell the Rip mean reversion short strategy.
/// </summary>
public class AtrSellTheRipMeanReversionStrategy : Strategy {
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _smoothPeriod;
	private readonly StrategyParam<bool> _useEmaFilter;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startTime;
	private readonly StrategyParam<DateTimeOffset> _endTime;

	private AverageTrueRange _atr;
	private SimpleMovingAverage _smoothSma;
	private ExponentialMovingAverage _ema200;
	private decimal? _prevLow;

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrPeriod {
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier.
	/// </summary>
	public decimal AtrMultiplier {
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Smoothing period for ATR trigger.
	/// </summary>
	public int SmoothPeriod {
		get => _smoothPeriod.Value;
		set => _smoothPeriod.Value = value;
	}

	/// <summary>
	/// Enable EMA trend filter.
	/// </summary>
	public bool UseEmaFilter {
		get => _useEmaFilter.Value;
		set => _useEmaFilter.Value = value;
	}

	/// <summary>
	/// EMA length for trend filter.
	/// </summary>
	public int EmaPeriod {
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType {
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Start time for signals.
	/// </summary>
	public DateTimeOffset StartTime {
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// End time for signals.
	/// </summary>
	public DateTimeOffset EndTime {
		get => _endTime.Value;
		set => _endTime.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see
	/// cref="AtrSellTheRipMeanReversionStrategy"/>.
	/// </summary>
	public AtrSellTheRipMeanReversionStrategy() {
		_atrPeriod = Param(nameof(AtrPeriod), 20)
						 .SetGreaterThanZero()
						 .SetDisplay("ATR Period", "ATR calculation period",
									 "Parameters")
						 .SetCanOptimize(true);

		_atrMultiplier =
			Param(nameof(AtrMultiplier), 1m)
				.SetDisplay("ATR Multiplier", "ATR multiplier", "Parameters")
				.SetCanOptimize(true);

		_smoothPeriod =
			Param(nameof(SmoothPeriod), 10)
				.SetGreaterThanZero()
				.SetDisplay("Smoothing Period", "SMA period for ATR trigger",
							"Parameters")
				.SetCanOptimize(true);

		_useEmaFilter =
			Param(nameof(UseEmaFilter), true)
				.SetDisplay("Use EMA Filter", "Enable 200 EMA trend filter",
							"Filters");

		_emaPeriod = Param(nameof(EmaPeriod), 200)
						 .SetGreaterThanZero()
						 .SetDisplay("EMA Period",
									 "EMA length for trend filter", "Filters")
						 .SetCanOptimize(true);

		_candleType =
			Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles", "General");

		_startTime =
			Param(nameof(StartTime),
				  new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero))
				.SetDisplay("Start Time", "Start time for signals", "Time");

		_endTime = Param(nameof(EndTime),
						 new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero))
					   .SetDisplay("End Time", "End time for signals", "Time");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)>
	GetWorkingSecurities() {
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted() {
		base.OnReseted();

		_atr = default;
		_smoothSma = default;
		_ema200 = default;
		_prevLow = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time) {
		base.OnStarted(time);

		StartProtection();

		_atr = new AverageTrueRange { Length = AtrPeriod };
		_smoothSma = new SimpleMovingAverage { Length = SmoothPeriod };
		_ema200 = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_atr, _ema200, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null) {
			DrawCandles(area, subscription);
			DrawIndicator(area, _smoothSma);
			DrawIndicator(area, _ema200);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue,
							   decimal emaValue) {
		if (candle.State != CandleStates.Finished)
			return;

		var atrThreshold = candle.ClosePrice + atrValue * AtrMultiplier;
		var smoothValue = _smoothSma.Process(atrThreshold);

		if (!smoothValue.IsFinal) {
			_prevLow = candle.LowPrice;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading()) {
			_prevLow = candle.LowPrice;
			return;
		}

		var signalTrigger = smoothValue.ToDecimal();
		var inWindow =
			candle.OpenTime >= StartTime && candle.OpenTime <= EndTime;

		var shortCondition = inWindow && candle.ClosePrice > signalTrigger &&
							 (!UseEmaFilter || candle.ClosePrice < emaValue);

		if (shortCondition && Position >= 0) {
			var volume = Volume + (Position > 0 ? Position : 0m);
			SellMarket(volume);
		} else if (Position < 0 && _prevLow is decimal prevLow &&
				   candle.ClosePrice < prevLow) {
			BuyMarket(Math.Abs(Position));
		}

		_prevLow = candle.LowPrice;
	}
}
