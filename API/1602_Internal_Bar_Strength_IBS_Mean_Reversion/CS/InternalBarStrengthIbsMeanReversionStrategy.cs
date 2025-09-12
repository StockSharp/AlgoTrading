
using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Short strategy using internal bar strength for mean reversion.
/// </summary>
public class InternalBarStrengthIbsMeanReversionStrategy : Strategy {
	private readonly StrategyParam<decimal> _upperThreshold;
	private readonly StrategyParam<decimal> _lowerThreshold;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startTime;
	private readonly StrategyParam<DateTimeOffset> _endTime;

	private decimal? _prevHigh;

	/// <summary>
	/// IBS value to trigger entry.
	/// </summary>
	public decimal UpperThreshold {
		get => _upperThreshold.Value;
		set => _upperThreshold.Value = value;
	}

	/// <summary>
	/// IBS value to exit position.
	/// </summary>
	public decimal LowerThreshold {
		get => _lowerThreshold.Value;
		set => _lowerThreshold.Value = value;
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
	/// cref="InternalBarStrengthIbsMeanReversionStrategy"/>.
	/// </summary>
	public InternalBarStrengthIbsMeanReversionStrategy() {
		_upperThreshold =
			Param(nameof(UpperThreshold), 0.9m)
				.SetRange(0m, 1m)
				.SetDisplay("Upper Threshold", "IBS value to trigger entry",
							"Parameters")
				.SetCanOptimize(true);

		_lowerThreshold = Param(nameof(LowerThreshold), 0.3m)
							  .SetRange(0m, 1m)
							  .SetDisplay("Lower Threshold",
										  "IBS value to exit", "Parameters")
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

		_prevHigh = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time) {
		base.OnStarted(time);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null) {
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle) {
		if (candle.State != CandleStates.Finished)
			return;

		var range = candle.HighPrice - candle.LowPrice;
		if (range == 0) {
			_prevHigh = candle.HighPrice;
			return;
		}

		var ibs = (candle.ClosePrice - candle.LowPrice) / range;
		var inWindow =
			candle.OpenTime >= StartTime && candle.OpenTime <= EndTime;

		var shortCondition = _prevHigh is decimal prevHigh &&
							 candle.ClosePrice > prevHigh &&
							 ibs >= UpperThreshold && inWindow;

		if (shortCondition && Position >= 0) {
			var volume = Volume + (Position > 0 ? Position : 0m);
			SellMarket(volume);
		} else if (Position < 0 && ibs <= LowerThreshold) {
			BuyMarket(Math.Abs(Position));
		}

		_prevHigh = candle.HighPrice;
	}
}
