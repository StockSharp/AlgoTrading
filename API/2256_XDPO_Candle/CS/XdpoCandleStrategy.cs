namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// XDPO candle strategy that trades on color changes of double smoothed candles.
/// </summary>
public class XdpoCandleStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _emaOpen1;
	private decimal? _emaOpen2;
	private decimal? _emaClose1;
	private decimal? _emaClose2;
	private int? _previousColor;

	/// <summary>
	/// Length of the first exponential moving average.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Length of the second exponential moving average.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="XdpoCandleStrategy"/>.
	/// </summary>
	public XdpoCandleStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Length of the first EMA", "Parameters")
			.SetCanOptimize(true);

		_slowLength = Param(nameof(SlowLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Length of the second EMA", "Parameters")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[] { (Security, CandleType) };
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_emaOpen1 = _emaOpen2 = _emaClose1 = _emaClose2 = null;
		_previousColor = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var open1 = CalcEma(candle.OpenPrice, ref _emaOpen1, FastLength);
		var open2 = CalcEma(open1, ref _emaOpen2, SlowLength);
		var close1 = CalcEma(candle.ClosePrice, ref _emaClose1, FastLength);
		var close2 = CalcEma(close1, ref _emaClose2, SlowLength);

		var color = open2 < close2 ? 2 : open2 > close2 ? 0 : 1;
		var goLong = color == 2 && _previousColor != 2;
		var goShort = color == 0 && _previousColor != 0;

		if (goLong && Position <= 0)
		BuyMarket(Volume + Math.Abs(Position));
		else if (goShort && Position >= 0)
		SellMarket(Volume + Math.Abs(Position));

		_previousColor = color;
	}

	private static decimal CalcEma(decimal price, ref decimal? prev, int length)
	{
		var k = 2m / (length + 1m);
		var result = prev.HasValue ? price * k + prev.Value * (1m - k) : price;
		prev = result;
		return result;
	}
}
