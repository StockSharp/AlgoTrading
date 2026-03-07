using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands Winner LITE Strategy.
/// Buys when candle body extends below lower BB, sells when above upper BB.
/// </summary>
public class BollingerWinnerLiteStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<decimal> _candlePercent;
	private readonly StrategyParam<bool> _showShort;
	private readonly StrategyParam<int> _cooldownBars;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BBLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands standard deviation multiplier.
	/// </summary>
	public decimal BBMultiplier
	{
		get => _bbMultiplier.Value;
		set => _bbMultiplier.Value = value;
	}

	/// <summary>
	/// Candle percentage below/above the BB.
	/// </summary>
	public decimal CandlePercent
	{
		get => _candlePercent.Value;
		set => _candlePercent.Value = value;
	}

	/// <summary>
	/// Enable short entries.
	/// </summary>
	public bool ShowShort
	{
		get => _showShort.Value;
		set => _showShort.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	private BollingerBands _bollinger;
	private int _cooldownRemaining;

	public BollingerWinnerLiteStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_bbLength = Param(nameof(BBLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Period", "Bollinger Bands period", "Bollinger Bands");

		_bbMultiplier = Param(nameof(BBMultiplier), 1.5m)
			.SetDisplay("BB StdDev", "Bollinger Bands standard deviation multiplier", "Bollinger Bands");

		_candlePercent = Param(nameof(CandlePercent), 30m)
			.SetDisplay("Candle %", "Candle percentage below/above the BB", "Strategy");

		_showShort = Param(nameof(ShowShort), true)
			.SetDisplay("Short entries", "Enable short entries", "Strategy");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bollinger = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_bollinger = new BollingerBands
		{
			Length = BBLength,
			Width = BBMultiplier
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(_bollinger, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue bollingerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_bollinger.IsFormed)
			return;

		var bb = (BollingerBandsValue)bollingerValue;
		if (bb.UpBand is not decimal upperBand ||
			bb.LowBand is not decimal lowerBand)
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		var close = candle.ClosePrice;
		var open = candle.OpenPrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;

		// Buy: close below lower band (oversold)
		var buy = close <= lowerBand;

		// Sell: close above upper band (overbought)
		var sell = close >= upperBand;

		if (buy && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		else if (sell)
		{
			if (Position > 0)
			{
				SellMarket(Math.Abs(Position));
				_cooldownRemaining = CooldownBars;
			}
			else if (ShowShort && Position == 0)
			{
				SellMarket(Volume);
				_cooldownRemaining = CooldownBars;
			}
		}
	}
}
