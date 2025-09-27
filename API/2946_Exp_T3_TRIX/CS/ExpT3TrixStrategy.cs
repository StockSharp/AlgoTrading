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
/// Operating modes supported by <see cref="ExpT3TrixStrategy"/>.
/// </summary>
public enum ExpT3TrixModes
{
	/// <summary>
	/// Uses histogram zero cross detection.
	/// </summary>
	Breakdown,

	/// <summary>
	/// Uses turning points of the histogram slope.
	/// </summary>
	Twist,

	/// <summary>
	/// Uses color flip of the fast and slow TRIX cloud.
	/// </summary>
	CloudTwist,
}

/// <summary>
/// Port of the Exp T3 TRIX expert advisor that trades triple smoothed TRIX signals.
/// </summary>
public class ExpT3TrixStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _volumeFactor;
	private readonly StrategyParam<ExpT3TrixModes> _mode;
	private readonly StrategyParam<bool> _allowLongEntry;
	private readonly StrategyParam<bool> _allowShortEntry;
	private readonly StrategyParam<bool> _allowLongExit;
	private readonly StrategyParam<bool> _allowShortExit;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastEma1 = null!;
	private ExponentialMovingAverage _fastEma2 = null!;
	private ExponentialMovingAverage _fastEma3 = null!;
	private ExponentialMovingAverage _fastEma4 = null!;
	private ExponentialMovingAverage _fastEma5 = null!;
	private ExponentialMovingAverage _fastEma6 = null!;

	private ExponentialMovingAverage _slowEma1 = null!;
	private ExponentialMovingAverage _slowEma2 = null!;
	private ExponentialMovingAverage _slowEma3 = null!;
	private ExponentialMovingAverage _slowEma4 = null!;
	private ExponentialMovingAverage _slowEma5 = null!;
	private ExponentialMovingAverage _slowEma6 = null!;

	private decimal? _prevFastT3;
	private decimal? _prevSlowT3;
	private decimal? _prevFastTrix;
	private decimal? _prevPrevFastTrix;
	private decimal? _prevSlowTrix;

	/// <summary>
	/// Fast smoothing depth used for the triple smoothed TRIX.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow smoothing depth used for the triple smoothed TRIX.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Volume factor applied in the Tillson T3 smoothing formula.
	/// </summary>
	public decimal VolumeFactor
	{
		get => _volumeFactor.Value;
		set => _volumeFactor.Value = value;
	}

	/// <summary>
	/// Signal detection mode that mimics the original expert advisor behavior.
	/// </summary>
	public ExpT3TrixModes Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Allows opening long positions.
	/// </summary>
	public bool AllowLongEntry
	{
		get => _allowLongEntry.Value;
		set => _allowLongEntry.Value = value;
	}

	/// <summary>
	/// Allows opening short positions.
	/// </summary>
	public bool AllowShortEntry
	{
		get => _allowShortEntry.Value;
		set => _allowShortEntry.Value = value;
	}

	/// <summary>
	/// Allows closing existing long positions.
	/// </summary>
	public bool AllowLongExit
	{
		get => _allowLongExit.Value;
		set => _allowLongExit.Value = value;
	}

	/// <summary>
	/// Allows closing existing short positions.
	/// </summary>
	public bool AllowShortExit
	{
		get => _allowShortExit.Value;
		set => _allowShortExit.Value = value;
	}

	/// <summary>
	/// Candle aggregation used to feed the indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ExpT3TrixStrategy"/>.
	/// </summary>
	public ExpT3TrixStrategy()
	{
		_fastLength = Param(nameof(FastLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast T3 averaging depth", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(4, 30, 2);

		_slowLength = Param(nameof(SlowLength), 18)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow T3 averaging depth", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(6, 40, 2);

		_volumeFactor = Param(nameof(VolumeFactor), 0.7m)
			.SetDisplay("Volume Factor", "Tillson T3 smoothing factor", "Indicator")
			.SetRange(0m, 1m)
			.SetCanOptimize(true)
			.SetOptimize(0.4m, 0.9m, 0.05m);

		_mode = Param(nameof(Mode), ExpT3TrixModes.Twist)
			.SetDisplay("Mode", "Signal evaluation algorithm", "Trading");

		_allowLongEntry = Param(nameof(AllowLongEntry), true)
			.SetDisplay("Allow Long Entry", "Enable opening long trades", "Permissions");

		_allowShortEntry = Param(nameof(AllowShortEntry), true)
			.SetDisplay("Allow Short Entry", "Enable opening short trades", "Permissions");

		_allowLongExit = Param(nameof(AllowLongExit), true)
			.SetDisplay("Allow Long Exit", "Enable closing long trades", "Permissions");

		_allowShortExit = Param(nameof(AllowShortExit), true)
			.SetDisplay("Allow Short Exit", "Enable closing short trades", "Permissions");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for TRIX", "Market Data");
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

		_fastEma1 = _fastEma2 = _fastEma3 = _fastEma4 = _fastEma5 = _fastEma6 = null!;
		_slowEma1 = _slowEma2 = _slowEma3 = _slowEma4 = _slowEma5 = _slowEma6 = null!;

		_prevFastT3 = null;
		_prevSlowT3 = null;
		_prevFastTrix = null;
		_prevPrevFastTrix = null;
		_prevSlowTrix = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastEma1 = new() { Length = FastLength };
		_fastEma2 = new() { Length = FastLength };
		_fastEma3 = new() { Length = FastLength };
		_fastEma4 = new() { Length = FastLength };
		_fastEma5 = new() { Length = FastLength };
		_fastEma6 = new() { Length = FastLength };

		_slowEma1 = new() { Length = SlowLength };
		_slowEma2 = new() { Length = SlowLength };
		_slowEma3 = new() { Length = SlowLength };
		_slowEma4 = new() { Length = SlowLength };
		_slowEma5 = new() { Length = SlowLength };
		_slowEma6 = new() { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection();

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

		var close = candle.ClosePrice;
		var time = candle.OpenTime;

		var fastT3 = CalcT3(_fastEma1, _fastEma2, _fastEma3, _fastEma4, _fastEma5, _fastEma6, close, time, VolumeFactor);
		var slowT3 = CalcT3(_slowEma1, _slowEma2, _slowEma3, _slowEma4, _slowEma5, _slowEma6, close, time, VolumeFactor);

		var prevFastT3 = _prevFastT3;
		var prevSlowT3 = _prevSlowT3;

		decimal fastTrix = 0m;
		decimal slowTrix = 0m;

		if (prevFastT3 is decimal fast && fast != 0m)
			fastTrix = (fastT3 - fast) / fast;

		if (prevSlowT3 is decimal slow && slow != 0m)
			slowTrix = (slowT3 - slow) / slow;

		_prevFastT3 = fastT3;
		_prevSlowT3 = slowT3;

		var prevFastTrix = _prevFastTrix;
		var prevPrevFastTrix = _prevPrevFastTrix;
		var prevSlowTrix = _prevSlowTrix;

		if (!prevFastTrix.HasValue)
		{
			_prevFastTrix = fastTrix;
			_prevSlowTrix = slowTrix;
			return;
		}

		var openLong = false;
		var openShort = false;
		var closeLong = false;
		var closeShort = false;

		switch (Mode)
		{
			case ExpT3TrixModes.Breakdown:
			{
				if (fastTrix > 0m && prevFastTrix.Value <= 0m)
				{
					openLong = AllowLongEntry;
					closeShort = AllowShortExit;
				}
				else if (fastTrix < 0m && prevFastTrix.Value >= 0m)
				{
					openShort = AllowShortEntry;
					closeLong = AllowLongExit;
				}
				break;
			}
			case ExpT3TrixModes.Twist:
			{
				if (prevPrevFastTrix.HasValue)
				{
					var prevSlope = prevFastTrix.Value - prevPrevFastTrix.Value;
					var currentSlope = fastTrix - prevFastTrix.Value;

					if (prevSlope < 0m && currentSlope > 0m)
					{
						openLong = AllowLongEntry;
						closeShort = AllowShortExit;
					}
					else if (prevSlope > 0m && currentSlope < 0m)
					{
						openShort = AllowShortEntry;
						closeLong = AllowLongExit;
					}
				}
				break;
			}
			case ExpT3TrixModes.CloudTwist:
			{
				if (prevSlowTrix.HasValue)
				{
					if (fastTrix > slowTrix && prevFastTrix.Value <= prevSlowTrix.Value)
					{
						openLong = AllowLongEntry;
						closeShort = AllowShortExit;
					}
					else if (fastTrix < slowTrix && prevFastTrix.Value >= prevSlowTrix.Value)
					{
						openShort = AllowShortEntry;
						closeLong = AllowLongExit;
					}
				}
				break;
			}
		}

		if (closeLong && Position > 0)
			SellMarket(Position);

		if (closeShort && Position < 0)
			BuyMarket(Math.Abs(Position));

		if (openLong)
		{
			if (Position < 0 && !closeShort)
			{
				openLong = false;
			}

			if (openLong && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}

		if (openShort)
		{
			if (Position > 0 && !closeLong)
			{
				openShort = false;
			}

			if (openShort && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_prevPrevFastTrix = prevFastTrix;
		_prevFastTrix = fastTrix;
		_prevSlowTrix = slowTrix;
	}

	private static decimal CalcT3(
		ExponentialMovingAverage ema1,
		ExponentialMovingAverage ema2,
		ExponentialMovingAverage ema3,
		ExponentialMovingAverage ema4,
		ExponentialMovingAverage ema5,
		ExponentialMovingAverage ema6,
		decimal price,
		DateTimeOffset time,
		decimal volumeFactor)
	{
		var e1 = ema1.Process(price, time, true).ToDecimal();
		var e2 = ema2.Process(e1, time, true).ToDecimal();
		var e3 = ema3.Process(e2, time, true).ToDecimal();
		var e4 = ema4.Process(e3, time, true).ToDecimal();
		var e5 = ema5.Process(e4, time, true).ToDecimal();
		var e6 = ema6.Process(e5, time, true).ToDecimal();

		var c1 = -volumeFactor * volumeFactor * volumeFactor;
		var c2 = 3m * volumeFactor * volumeFactor + 3m * volumeFactor * volumeFactor * volumeFactor;
		var c3 = -6m * volumeFactor * volumeFactor - 3m * volumeFactor - 3m * volumeFactor * volumeFactor * volumeFactor;
		var c4 = 1m + 3m * volumeFactor + volumeFactor * volumeFactor * volumeFactor + 3m * volumeFactor * volumeFactor;

		return c1 * e6 + c2 * e5 + c3 * e4 + c4 * e3;
	}
}