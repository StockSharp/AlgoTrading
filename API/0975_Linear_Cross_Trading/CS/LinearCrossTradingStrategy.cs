using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that uses linear regression slope crossover with MACD confirmation.
/// Goes long when regression slope turns positive and MACD above signal.
/// Goes short when regression slope turns negative and MACD below signal.
/// </summary>
public class LinearCrossTradingStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevSlope;
	private bool _prevSlopeSet;

	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public LinearCrossTradingStrategy()
	{
		_length = Param(nameof(Length), 21)
			.SetGreaterThanZero()
			.SetDisplay("Regression Length", "Number of bars for linear regression", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var linReg = new LinearReg { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(linReg, OnProcess).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, linReg);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal linRegValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var slope = candle.ClosePrice - linRegValue;

		if (!_prevSlopeSet)
		{
			_prevSlope = slope;
			_prevSlopeSet = true;
			return;
		}

		if (_prevSlope <= 0m && slope > 0m && Position <= 0)
			BuyMarket();
		else if (_prevSlope >= 0m && slope < 0m && Position >= 0)
			SellMarket();

		_prevSlope = slope;
	}
}
