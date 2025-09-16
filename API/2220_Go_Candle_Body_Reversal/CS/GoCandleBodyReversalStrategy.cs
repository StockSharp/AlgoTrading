using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on a moving average of the candle body.
/// </summary>
public class GoCandleBodyReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _sma;
	private int _prevSign;

	/// <summary>
	/// Length of the moving average.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Type of candles to subscribe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public GoCandleBodyReversalStrategy()
	{
		_period = Param(nameof(Period), 174)
			.SetGreaterThanZero()
			.SetDisplay("Period", "SMA period for candle body", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(50, 300, 25);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");
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
		_prevSign = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_sma = new SimpleMovingAverage { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		var maValue = _sma.Process(
			candle.ClosePrice - candle.OpenPrice,
			candle.ServerTime,
			candle.State == CandleStates.Finished);

		if (!maValue.IsFinal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var value = maValue.ToDecimal();
		var sign = value > 0 ? 1 : value < 0 ? -1 : 0;

		if (sign < 0 && _prevSign > 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));

			BuyMarket(Volume);
		}
		else if (sign > 0 && _prevSign < 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			SellMarket(Volume);
		}

		_prevSign = sign;
	}
}

