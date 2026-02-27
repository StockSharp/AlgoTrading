using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with Parabolic SAR confirmation.
/// Buys when fast EMA above slow EMA and SAR below price.
/// Sells when fast EMA below slow EMA and SAR above price.
/// </summary>
public class EmaSarPowerStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public EmaSarPowerStrategy()
	{
		_fastLength = Param(nameof(FastLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");

		_slowLength = Param(nameof(SlowLength), 34)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0;
		_prevSlow = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastEma = new ExponentialMovingAverage { Length = FastLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowLength };
		var sar = new ParabolicSar();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, sar, ProcessCandle)
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

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal sar)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_hasPrev = true;
			return;
		}

		// Buy: EMA crossover up + SAR below price
		if (_prevFast <= _prevSlow && fast > slow && sar < candle.LowPrice)
		{
			if (Position <= 0)
				BuyMarket();
		}
		// Sell: EMA crossover down + SAR above price
		else if (_prevFast >= _prevSlow && fast < slow && sar > candle.HighPrice)
		{
			if (Position >= 0)
				SellMarket();
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
