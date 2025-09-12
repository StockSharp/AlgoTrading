namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Long-only strategy based on smoothed Heikin-Ashi candles.
/// Buys when candle color turns from red to green and exits when it turns back to red.
/// </summary>
public class SmoothedHeikenAshiLongOnlyStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _emaOpen;
	private ExponentialMovingAverage _emaClose;
	private ExponentialMovingAverage _emaHigh;
	private ExponentialMovingAverage _emaLow;
	private ExponentialMovingAverage _emaHaOpen;
	private ExponentialMovingAverage _emaHaClose;

	private decimal? _prevHaOpen;
	private decimal? _prevHaClose;
	private bool? _prevIsGreen;

	/// <summary>
	/// Length for initial EMA smoothing.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Length for smoothing Heikin-Ashi values.
	/// </summary>
	public int SmoothingLength
	{
		get => _smoothingLength.Value;
		set => _smoothingLength.Value = value;
	}

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SmoothedHeikenAshiLongOnlyStrategy"/>.
	/// </summary>
	public SmoothedHeikenAshiLongOnlyStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 10)
			.SetDisplay("EMA Length", "Length for primary EMA smoothing", "General")
			.SetRange(5, 50)
			.SetCanOptimize(true);

		_smoothingLength = Param(nameof(SmoothingLength), 10)
			.SetDisplay("Smoothing Length", "Length for secondary EMA smoothing", "General")
			.SetRange(5, 50)
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		_prevHaOpen = null;
		_prevHaClose = null;
		_prevIsGreen = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_emaOpen = new ExponentialMovingAverage { Length = EmaLength };
		_emaClose = new ExponentialMovingAverage { Length = EmaLength };
		_emaHigh = new ExponentialMovingAverage { Length = EmaLength };
		_emaLow = new ExponentialMovingAverage { Length = EmaLength };
		_emaHaOpen = new ExponentialMovingAverage { Length = SmoothingLength };
		_emaHaClose = new ExponentialMovingAverage { Length = SmoothingLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(OnProcess).Start();
	}

	private void OnProcess(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.OpenTime;
		var open = _emaOpen.Process(candle.OpenPrice, time, true).ToDecimal();
		var close = _emaClose.Process(candle.ClosePrice, time, true).ToDecimal();
		var high = _emaHigh.Process(candle.HighPrice, time, true).ToDecimal();
		var low = _emaLow.Process(candle.LowPrice, time, true).ToDecimal();

		var haClose = (open + high + low + close) / 4m;
		var haOpen = _prevHaOpen is null ? (open + close) / 2m : (_prevHaOpen.Value + _prevHaClose!.Value) / 2m;

		_prevHaOpen = haOpen;
		_prevHaClose = haClose;

		var o2 = _emaHaOpen.Process(haOpen, time, true).ToDecimal();
		var c2 = _emaHaClose.Process(haClose, time, true).ToDecimal();

		var isGreen = c2 >= o2;

		if (isGreen && _prevIsGreen == false && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (!isGreen && _prevIsGreen == true && Position > 0)
		{
			SellMarket(Position);
		}

		_prevIsGreen = isGreen;
	}
}
