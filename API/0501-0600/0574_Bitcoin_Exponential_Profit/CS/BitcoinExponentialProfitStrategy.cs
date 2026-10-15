using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bitcoin Exponential Profit strategy based on EMA crossover.
/// Enters long on golden cross, short on death cross.
/// </summary>
public class BitcoinExponentialProfitStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BitcoinExponentialProfitStrategy()
	{
		_fastLength = Param(nameof(FastLength), 120)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length", "Indicators");

		_slowLength = Param(nameof(SlowLength), 450)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_prevFast = 0m;
		_prevSlow = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevFast == 0m || _prevSlow == 0m)
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		if (_prevFast <= _prevSlow && fast > slow && Position <= 0)
		{
			BuyMarket();
		}
		else if (_prevFast >= _prevSlow && fast < slow && Position >= 0)
		{
			SellMarket();
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
