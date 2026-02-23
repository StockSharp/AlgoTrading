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
/// Trades reversals from Bollinger Bands.
/// Buys when price closes below lower band and sells when price closes above upper band.
/// </summary>
public class BollingerBandsSessionReversalStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerWidth;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Length of the Bollinger Bands moving average.
	/// </summary>
	public int BollingerLength
	{
		get => _bollingerLength.Value;
		set => _bollingerLength.Value = value;
	}

	/// <summary>
	/// Width multiplier for Bollinger Bands.
	/// </summary>
	public decimal BollingerWidth
	{
		get => _bollingerWidth.Value;
		set => _bollingerWidth.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public BollingerBandsSessionReversalStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series", "General");

		_bollingerLength = Param(nameof(BollingerLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Length", "MA period for Bollinger Bands", "Indicators");

		_bollingerWidth = Param(nameof(BollingerWidth), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Width", "Band width multiplier", "Indicators");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bollinger = new BollingerBands
		{
			Length = BollingerLength,
			Width = BollingerWidth
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (bbValue is not IBollingerBandsValue bb)
			return;

		var middle = bb.MovingAverage ?? 0m;
		var upper = bb.UpBand ?? 0m;
		var lower = bb.LowBand ?? 0m;

		if (middle == 0m || upper == 0m || lower == 0m)
			return;

		var price = candle.ClosePrice;

		// Reversal: buy when price falls below lower band
		if (price < lower && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
		}
		// Reversal: sell when price rises above upper band
		else if (price > upper && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
		}
		// Exit at middle band
		else if (Position > 0 && price >= middle)
		{
			SellMarket();
		}
		else if (Position < 0 && price <= middle)
		{
			BuyMarket();
		}
	}
}
