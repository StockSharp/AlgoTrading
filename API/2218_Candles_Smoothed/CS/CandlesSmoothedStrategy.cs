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
/// Strategy based on smoothed candle body direction.
/// Smooths (close-open) with WMA, trades on color changes.
/// </summary>
public class CandlesSmoothedStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maLength;

	private WeightedMovingAverage _ma;
	private int? _prevColor;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }

	public CandlesSmoothedStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame", "General");

		_maLength = Param(nameof(MaLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average smoothing length", "Indicator");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_ma = null;
		_prevColor = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ma = new WeightedMovingAverage { Length = MaLength };

		// Use a dummy SMA for warmup binding
		var warmup = new SimpleMovingAverage { Length = MaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(warmup, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal _smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Calculate close-open diff and smooth with WMA
		var diff = candle.ClosePrice - candle.OpenPrice;
		var maResult = _ma.Process(new DecimalIndicatorValue(_ma, diff, candle.OpenTime) { IsFinal = true });

		if (!maResult.IsFormed)
			return;

		var smoothed = maResult.GetValue<decimal>();
		var color = smoothed > 0m ? 0 : 1;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevColor = color;
			return;
		}

		if (_prevColor is int prev)
		{
			// Color change from negative to positive -> buy
			if (color == 1 && prev == 0 && Position <= 0)
				BuyMarket();
			// Color change from positive to negative -> sell
			else if (color == 0 && prev == 1 && Position >= 0)
				SellMarket();
		}

		_prevColor = color;
	}
}
