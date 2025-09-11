
using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Short strategy entering after consecutive closes above moving average.
/// </summary>
public class ConsecutiveBarsAboveMaStrategy : Strategy {
	private readonly StrategyParam<int> _threshold;
	private readonly StrategyParam<string> _maType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<bool> _useEmaFilter;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startTime;
	private readonly StrategyParam<DateTimeOffset> _endTime;

	private MovingAverage _signalMa;
	private ExponentialMovingAverage _ema200;
	private int _bullCount;
	private decimal? _prevHigh;
	private decimal? _prevLow;

	/// <summary>
	/// Number of bars above MA to trigger entry.
	/// </summary>
	public int Threshold {
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// Moving average type.
	/// </summary>
	public string MaType {
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength {
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Enable EMA trend filter.
	/// </summary>
	public bool UseEmaFilter {
		get => _useEmaFilter.Value;
		set => _useEmaFilter.Value = value;
	}

	/// <summary>
	/// EMA period for trend filter.
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
	/// cref="ConsecutiveBarsAboveMaStrategy"/>.
	/// </summary>
	public ConsecutiveBarsAboveMaStrategy() {
		_threshold = Param(nameof(Threshold), 3)
						 .SetGreaterThanZero()
						 .SetDisplay("Signal Threshold",
									 "Number of bars above MA", "Parameters")
						 .SetCanOptimize(true);

		_maType =
			Param(nameof(MaType), "SMA")
				.SetDisplay("MA Type", "Moving average type", "Parameters");

		_maLength =
			Param(nameof(MaLength), 5)
				.SetGreaterThanZero()
				.SetDisplay("MA Length", "Moving average length", "Parameters")
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

		_signalMa = default;
		_ema200 = default;
		_bullCount = 0;
		_prevHigh = default;
		_prevLow = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time) {
		base.OnStarted(time);

		StartProtection();

		_signalMa = CreateMa(MaType, MaLength);
		_ema200 = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_signalMa, _ema200, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null) {
			DrawCandles(area, subscription);
			DrawIndicator(area, _signalMa);
			DrawIndicator(area, _ema200);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue,
							   decimal emaValue) {
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevHigh is null || _prevLow is null) {
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			return;
		}

		if (candle.ClosePrice > maValue)
			_bullCount++;
		else
			_bullCount = 0;

		var inWindow =
			candle.OpenTime >= StartTime && candle.OpenTime <= EndTime;

		var shortCondition = inWindow && _bullCount >= Threshold &&
							 candle.ClosePrice > _prevHigh &&
							 (!UseEmaFilter || candle.ClosePrice < emaValue);

		if (shortCondition && Position >= 0) {
			var volume = Volume + (Position > 0 ? Position : 0m);
			SellMarket(volume);
		} else if (Position < 0 && candle.ClosePrice < _prevLow) {
			BuyMarket(Math.Abs(Position));
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}

	private MovingAverage CreateMa(string type, int length) {
		return type == "SMA" ? new SimpleMovingAverage { Length = length }
							 : new ExponentialMovingAverage { Length = length };
	}
}
