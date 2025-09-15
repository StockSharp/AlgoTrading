using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that enters positions based on price deviation from EMA and standard deviation.
/// Opens long when price is above EMA by K2*StdDev and short when below by K2*StdDev.
/// Closes positions when deviation returns within K1*StdDev.
/// </summary>
public class ColorXvaMaDigitStDevStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _stdLength;
	private readonly StrategyParam<decimal> _k1;
	private readonly StrategyParam<decimal> _k2;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// EMA period length.
	/// </summary>
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }

	/// <summary>
	/// Standard deviation period.
	/// </summary>
	public int StdLength { get => _stdLength.Value; set => _stdLength.Value = value; }

	/// <summary>
	/// Inner deviation multiplier.
	/// </summary>
	public decimal K1 { get => _k1.Value; set => _k1.Value = value; }

	/// <summary>
	/// Outer deviation multiplier.
	/// </summary>
	public decimal K2 { get => _k2.Value; set => _k2.Value = value; }

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public ColorXvaMaDigitStDevStrategy()
	{
		_maLength = Param(nameof(MaLength), 15)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Period for the exponential moving average", "Parameters");

		_stdLength = Param(nameof(StdLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Length", "Period for standard deviation", "Parameters");

		_k1 = Param(nameof(K1), 1.5m)
			.SetDisplay("Deviation K1", "Inner band multiplier", "Parameters");

		_k2 = Param(nameof(K2), 2.5m)
			.SetDisplay("Deviation K2", "Outer band multiplier", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for market data", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ema = new ExponentialMovingAverage { Length = MaLength };
		var std = new StandardDeviation { Length = StdLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, std, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema, "EMA");
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal stdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (stdValue == 0m)
			return;

		var deviation = candle.ClosePrice - emaValue;
		var filter1 = K1 * stdValue;
		var filter2 = K2 * stdValue;

		// Open long when price exceeds the upper band
		if (Position <= 0 && deviation > filter2)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		// Open short when price falls below the lower band
		else if (Position >= 0 && deviation < -filter2)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		// Close long when price returns inside inner band
		else if (Position > 0 && deviation < filter1)
		{
			SellMarket(Math.Abs(Position));
		}
		// Close short when price returns inside inner band
		else if (Position < 0 && deviation > -filter1)
		{
			BuyMarket(Math.Abs(Position));
		}
	}
}
