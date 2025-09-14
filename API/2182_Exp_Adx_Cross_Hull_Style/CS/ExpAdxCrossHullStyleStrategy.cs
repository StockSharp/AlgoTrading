using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
namespace StockSharp.Samples.Strategies;
/// <summary>
/// Strategy based on ADX directional cross and Hull moving averages.
/// Buys when +DI crosses above -DI and sells when -DI crosses above +DI.
/// Exits positions when fast Hull MA crosses slow Hull MA.
/// </summary>
public class ExpAdxCrossHullStyleStrategy : Strategy
{
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<int> _fastHullLength;
	private readonly StrategyParam<int> _slowHullLength;
	private readonly StrategyParam<DataType> _candleType;
	private decimal _prevPlusDi;
	private decimal _prevMinusDi;
	/// <summary>
	/// ADX calculation period.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}
	/// <summary>
	/// Fast Hull Moving Average length.
	/// </summary>
	public int FastHullLength
	{
		get => _fastHullLength.Value;
		set => _fastHullLength.Value = value;
	}
	/// <summary>
	/// Slow Hull Moving Average length.
	/// </summary>
	public int SlowHullLength
	{
		get => _slowHullLength.Value;
		set => _slowHullLength.Value = value;
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
	/// Initializes a new instance of the <see cref="ExpAdxCrossHullStyleStrategy"/>.
	/// </summary>
	public ExpAdxCrossHullStyleStrategy()
	{
		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 5);
		_fastHullLength = Param(nameof(FastHullLength), 20)
			.SetDisplay("Fast Hull Length", "Period of the fast Hull MA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);
		_slowHullLength = Param(nameof(SlowHullLength), 50)
			.SetDisplay("Slow Hull Length", "Period of the slow Hull MA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(40, 100, 10);
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used by the strategy", "General");
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
		_prevPlusDi = 0m;
		_prevMinusDi = 0m;
	}
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var fastHull = new HullMovingAverage { Length = FastHullLength };
		var slowHull = new HullMovingAverage { Length = SlowHullLength };
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(adx, fastHull, slowHull, ProcessCandle)
			.Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastHull);
			DrawIndicator(area, slowHull);
			DrawOwnTrades(area);
		}
	}
	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue, IIndicatorValue fastHullValue, IIndicatorValue slowHullValue)
	{
		if (candle.State != CandleStates.Finished)
			return;
		if (!adxValue.IsFinal || !fastHullValue.IsFinal || !slowHullValue.IsFinal)
			return;
		if (!IsFormedAndOnlineAndAllowTrading())
			return;
		var adx = (AverageDirectionalIndexValue)adxValue;
		var plusDi = adx.Dx.Plus;
		var minusDi = adx.Dx.Minus;
		var fastHull = fastHullValue.GetValue<decimal>();
		var slowHull = slowHullValue.GetValue<decimal>();
		// Entry signals based on DI cross
		if (_prevPlusDi <= _prevMinusDi && plusDi > minusDi && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (_prevPlusDi >= _prevMinusDi && plusDi < minusDi && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		// Exit signals based on Hull MA cross
		if (Position > 0 && fastHull < slowHull)
			SellMarket(Math.Abs(Position));
		else if (Position < 0 && fastHull > slowHull)
			BuyMarket(Math.Abs(Position));
		_prevPlusDi = plusDi;
		_prevMinusDi = minusDi;
	}
}
