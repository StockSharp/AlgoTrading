using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that uses Bollinger Bands for mean reversion.
/// Enters when price touches/breaks bands, exits at middle band.
/// </summary>
public class BollingerVolumeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _cooldownBars;

	private int _cooldown;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands standard deviation multiplier.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Strategy constructor.
	/// </summary>
	public BollingerVolumeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetRange(10, 50)
			.SetDisplay("Bollinger Period", "Period of the Bollinger Bands", "Indicators");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
			.SetDisplay("Bollinger Deviation", "Standard deviation multiplier", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 100)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General")
			.SetRange(5, 500);
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
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
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

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bb = (BollingerBandsValue)bollingerValue;

		if (bb.UpBand is not decimal upperBand ||
			bb.LowBand is not decimal lowerBand ||
			bb.MovingAverage is not decimal middleBand)
			return;

		var close = candle.ClosePrice;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Long: price below lower band (mean reversion buy)
		if (close < lowerBand && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Short: price above upper band (mean reversion sell)
		else if (close > upperBand && Position == 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}

		// Exit long: price returns to middle band
		if (Position > 0 && close > middleBand)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Exit short: price returns to middle band
		else if (Position < 0 && close < middleBand)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
