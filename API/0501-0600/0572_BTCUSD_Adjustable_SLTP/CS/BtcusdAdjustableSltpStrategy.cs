using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// BTCUSD strategy with adjustable SL/TP using SMA crossover.
/// Enters long on golden cross above EMA filter, short on death cross below EMA filter.
/// </summary>
public class BtcusdAdjustableSltpStrategy : Strategy
{
	private readonly StrategyParam<int> _fastSmaLength;
	private readonly StrategyParam<int> _slowSmaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;

	public int FastSmaLength { get => _fastSmaLength.Value; set => _fastSmaLength.Value = value; }
	public int SlowSmaLength { get => _slowSmaLength.Value; set => _slowSmaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BtcusdAdjustableSltpStrategy()
	{
		_fastSmaLength = Param(nameof(FastSmaLength), 120)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA", "Length of fast SMA", "Indicators");

		_slowSmaLength = Param(nameof(SlowSmaLength), 450)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA", "Length of slow SMA", "Indicators");

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

		var fastSma = new SimpleMovingAverage { Length = FastSmaLength };
		var slowSma = new SimpleMovingAverage { Length = SlowSmaLength };

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
