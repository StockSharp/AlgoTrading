using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Drawing;

using StockSharp.Messages;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Indicators;
using StockSharp.BusinessEntities;
using StockSharp.Localization;
using StockSharp.Charting;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands + Aroon Strategy
/// </summary>
public class BollingerAroonStrategy : Strategy
{
	private decimal? _aroonUpValue;
	private decimal? _aroonDownValue;

	public BollingerAroonStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_bbLength = Param(nameof(BBLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Bollinger Bands");

		_bbMultiplier = Param(nameof(BBMultiplier), 2.0m)
			.SetDisplay("BB StdDev", "Bollinger Bands standard deviation multiplier", "Bollinger Bands");

		_aroonLength = Param(nameof(AroonLength), 288)
			.SetGreaterThanZero()
			.SetDisplay("Aroon Period", "Aroon indicator period", "Aroon");

		_aroonConfirmation = Param(nameof(AroonConfirmation), 90m)
			.SetDisplay("Aroon Confirmation", "Aroon confirmation level", "Aroon");

		_aroonStop = Param(nameof(AroonStop), 70m)
			.SetDisplay("Aroon Stop", "Aroon stop level", "Aroon");
	}

	private readonly StrategyParam<DataType> _candleTypeParam;
	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	private readonly StrategyParam<int> _bbLength;
	public int BBLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	private readonly StrategyParam<decimal> _bbMultiplier;
	public decimal BBMultiplier
	{
		get => _bbMultiplier.Value;
		set => _bbMultiplier.Value = value;
	}

	private readonly StrategyParam<int> _aroonLength;
	public int AroonLength
	{
		get => _aroonLength.Value;
		set => _aroonLength.Value = value;
	}

	private readonly StrategyParam<decimal> _aroonConfirmation;
	public decimal AroonConfirmation
	{
		get => _aroonConfirmation.Value;
		set => _aroonConfirmation.Value = value;
	}

	private readonly StrategyParam<decimal> _aroonStop;
	public decimal AroonStop
	{
		get => _aroonStop.Value;
		set => _aroonStop.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_aroonUpValue = default;
		_aroonDownValue = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create Bollinger Bands indicator
		var bollinger = new BollingerBands
		{
			Length = BBLength,
			Width = BBMultiplier
		};

		// Create Aroon indicator
		var aroon = new Aroon { Length = AroonLength };

		// Subscribe to candles
		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(bollinger, aroon, OnProcess)
			.Start();

		// Configure chart
		var area = CreateChartArea();

		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, 
		IIndicatorValue bollingerValue, IIndicatorValue aroonValue)
	{
		// Only process finished candles
		if (candle.State != CandleStates.Finished)
			return;

		var bbTyped = (BollingerBandsValue)bollingerValue;

		var closePrice = candle.ClosePrice;
		var lowerBand = bbTyped.LowBand;
		var upperBand = bbTyped.UpBand;

		var aaTyped = (AroonValue)aroonValue;

		// Store Aroon values
		_aroonUpValue = aaTyped.Up;
		_aroonDownValue = aaTyped.Down;

		// Entry conditions
		var bullish = closePrice < lowerBand;
		var bearish = closePrice < upperBand;

		// Check Aroon confirmation for entry
		var aroonConfirmed = _aroonUpValue > AroonConfirmation;
		
		// Check Aroon stop condition
		var aroonStopTriggered = _aroonUpValue < AroonStop;

		// Long entry
		if (bullish && aroonConfirmed && Position == 0)
		{
			BuyMarket(Volume);
		}
		
		// Exit conditions
		if (Position > 0 && (bearish || aroonStopTriggered))
		{
			SellMarket(Volume);
		}
	}
}