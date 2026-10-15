using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MA Rounding Candle strategy.
/// Opens a long position when a smoothed candle is bullish and a short position when it is bearish.
/// </summary>
public class MaRoundingCandleStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _openMa;
	private ExponentialMovingAverage _closeMa;
	private int _prevColor = 1;

	public MaRoundingCandleStrategy()
	{
		_maLength = Param(nameof(MaLength), 12)
			.SetDisplay("MA Length", "Moving average length", "Parameters");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame());
	}

	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_openMa = default;
		_closeMa = default;
		_prevColor = 1;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_openMa = new ExponentialMovingAverage { Length = MaLength };
		_closeMa = new ExponentialMovingAverage { Length = MaLength };

		Indicators.Add(_openMa);
		Indicators.Add(_closeMa);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _openMa);
			DrawIndicator(area, _closeMa);
			DrawOwnTrades(area);
		}

		StartProtection(null, null);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var openVal = _openMa.Process(candle.OpenPrice, candle.OpenTime, true).ToDecimal();
		var closeVal = _closeMa.Process(candle.ClosePrice, candle.OpenTime, true).ToDecimal();

		if (!_openMa.IsFormed || !_closeMa.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var color = openVal < closeVal ? 2 : openVal > closeVal ? 0 : 1;

		if (_prevColor == 2 && color != 2 && Position <= 0)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
		}
		else if (_prevColor == 0 && color != 0 && Position >= 0)
		{
			if (Position > 0) SellMarket();
			SellMarket();
		}

		_prevColor = color;
	}
}
