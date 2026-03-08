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
/// Volatility Quality strategy. Trades when smoothed median price slope changes direction.
/// </summary>
public class VolatilityQualityStrategy : Strategy
{
	private readonly StrategyParam<int> _lengthParam;
	private readonly StrategyParam<DataType> _candleTypeParam;

	private ExponentialMovingAverage _sma;
	private decimal _prevSma;
	private int _prevColor;

	/// <summary>
	/// Smoothing period for median price.
	/// </summary>
	public int Length
	{
		get => _lengthParam.Value;
		set => _lengthParam.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="VolatilityQualityStrategy"/> class.
	/// </summary>
	public VolatilityQualityStrategy()
	{
		_lengthParam = Param(nameof(Length), 5)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Smoothing period for median price", "Parameters")
			
			.SetOptimize(5, 30, 5);

		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy", "Common");
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

		_sma = null;
		_prevSma = 0m;
		_prevColor = -1;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// Create indicator for smoothed median price
		_sma = new ExponentialMovingAverage { Length = Length };

		// Subscribe to candle series and bind indicator
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_sma, ProcessCandle)
			.Start();

		// Draw chart elements if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}

		// Basic position protection
		StartProtection(
			takeProfit: new Unit(2m, UnitTypes.Percent),
			stopLoss: new Unit(1m, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Determine indicator color based on slope
		int color;

		if (_prevSma == 0m)
		{
			_prevSma = smaValue;
			_prevColor = -1;
			return;
		}

		if (smaValue > _prevSma)
			color = 0; // rising line
		else if (smaValue < _prevSma)
			color = 1; // falling line
		else
			color = _prevColor; // unchanged

		// Check for color change and trade accordingly
		if (_prevColor == 1 && color == 0 && Position <= 0)
		{
			// Slope turned up - buy
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		else if (_prevColor == 0 && color == 1 && Position >= 0)
		{
			// Slope turned down - sell
			if (Position > 0)
				SellMarket();
			SellMarket();
		}

		_prevSma = smaValue;
		_prevColor = color;
	}
}