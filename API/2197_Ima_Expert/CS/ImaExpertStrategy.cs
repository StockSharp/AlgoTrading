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
/// Strategy based on relative change of price to its SMA.
/// </summary>
public class ImaExpertStrategy : Strategy
{
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<decimal> _signalLevel;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousIma;

	/// <summary>
	/// Period of the SMA indicator.
	/// </summary>
	public int SmaPeriod { get => _smaPeriod.Value; set => _smaPeriod.Value = value; }

	/// <summary>
	/// Threshold for signal generation.
	/// </summary>
	public decimal SignalLevel { get => _signalLevel.Value; set => _signalLevel.Value = value; }

	/// <summary>
	/// Candle type for indicator calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public ImaExpertStrategy()
	{
		_smaPeriod = Param(nameof(SmaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("SMA Period", "Length of moving average", "Parameters");

		_signalLevel = Param(nameof(SignalLevel), 0.5m)
			.SetDisplay("Signal Level", "IMA change threshold", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");
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
		_previousIma = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = SmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (smaValue == 0)
			return;

		var price = candle.ClosePrice;
		var ima = price / smaValue - 1m;

		if (_previousIma is null || _previousIma.Value == 0)
		{
			_previousIma = ima;
			return;
		}

		var k1 = (ima - _previousIma.Value) / Math.Abs(_previousIma.Value);
		_previousIma = ima;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position == 0)
		{
			if (k1 >= SignalLevel)
				BuyMarket();
			else if (k1 <= -SignalLevel)
				SellMarket();
		}
		else if (Position > 0 && k1 <= -SignalLevel)
		{
			SellMarket(Position + Volume);
		}
		else if (Position < 0 && k1 >= SignalLevel)
		{
			BuyMarket(Math.Abs(Position) + Volume);
		}
	}
}
