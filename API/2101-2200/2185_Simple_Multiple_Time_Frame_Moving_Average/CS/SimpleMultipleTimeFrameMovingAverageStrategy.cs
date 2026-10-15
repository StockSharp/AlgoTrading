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
/// Strategy using fast and slow SMA slopes for trade direction.
/// Buys when both SMAs are rising, sells when both are falling.
/// </summary>
public class SimpleMultipleTimeFrameMovingAverageStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevFast;
	private decimal? _prevSlow;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SimpleMultipleTimeFrameMovingAverageStrategy()
	{
		_fastLength = Param(nameof(FastLength), 5)
			.SetDisplay("Fast MA", "Fast moving average period", "General");

		_slowLength = Param(nameof(SlowLength), 20)
			.SetDisplay("Slow MA", "Slow moving average period", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = _prevSlow = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastSma = new ExponentialMovingAverage { Length = FastLength };
		var slowSma = new ExponentialMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastSma, slowSma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastSma);
			DrawIndicator(area, slowSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		if (_prevFast is decimal pf && _prevSlow is decimal ps)
		{
			var fastUp = fast > pf;
			var fastDown = fast < pf;
			var slowUp = slow > ps;
			var slowDown = slow < ps;

			if (fastUp && slowUp && Position <= 0)
				BuyMarket();
			else if (fastDown && slowDown && Position >= 0)
				SellMarket();
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
