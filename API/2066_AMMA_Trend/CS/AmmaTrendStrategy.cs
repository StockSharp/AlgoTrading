using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AMMA Trend strategy using Modified Moving Average direction changes.
/// Buys when MMA turns up, sells when MMA turns down.
/// </summary>
public class AmmaTrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;

	private decimal? _mma0;
	private decimal? _mma1;
	private decimal? _mma2;
	private decimal? _mma3;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }

	public AmmaTrendStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use for analysis", "General");

		_maPeriod = Param(nameof(MaPeriod), 25)
			.SetGreaterThanZero()
			.SetDisplay("AMMA Period", "Period of the modified moving average", "Indicator");
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
		_mma0 = _mma1 = _mma2 = _mma3 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var mma = new SmoothedMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(mma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, mma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal mmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_mma3 = _mma2;
		_mma2 = _mma1;
		_mma1 = _mma0;
		_mma0 = mmaValue;

		if (_mma1 is null || _mma2 is null || _mma3 is null)
			return;

		// Upward movement detected: MMA turned from falling to rising
		if (_mma2 < _mma3 && _mma1 > _mma2 && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		// Downward movement detected: MMA turned from rising to falling
		else if (_mma2 > _mma3 && _mma1 < _mma2 && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}
	}
}
