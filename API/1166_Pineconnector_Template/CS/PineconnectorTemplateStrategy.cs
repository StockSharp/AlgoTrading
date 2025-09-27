using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Template strategy demonstrating how to connect any indicator and trade on its signals.
/// Uses two simple moving averages as an example indicator.
/// </summary>
public class PineconnectorTemplateStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Fast MA period length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow MA period length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public PineconnectorTemplateStrategy()
	{
		_fastLength = Param(nameof(FastLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Length", "Period of the fast moving average", "MA Settings")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);

		_slowLength = Param(nameof(SlowLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Length", "Period of the slow moving average", "MA Settings")
			.SetCanOptimize(true)
			.SetOptimize(50, 200, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		var fastMa = new SMA { Length = FastLength };
		var slowMa = new SMA { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(fastMa, slowMa, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (fastValue > slowValue && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (fastValue < slowValue && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}
