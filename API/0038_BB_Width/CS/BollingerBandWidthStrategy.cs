using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades on Bollinger Bands Width expansion.
/// It identifies periods of increasing volatility (widening Bollinger Bands)
/// and trades in the direction of the trend as identified by price position relative to the middle band.
/// </summary>
public class BollingerBandWidthStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevWidth;
	private int _cooldown;

	/// <summary>
	/// Period for Bollinger Bands calculation.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Deviation for Bollinger Bands calculation.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Type of candles used for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
	/// Initialize the Bollinger Band Width strategy.
	/// </summary>
	public BollingerBandWidthStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Indicators")
			.SetOptimize(10, 30, 5);

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
			.SetDisplay("Bollinger Deviation", "Deviation for Bollinger Bands calculation", "Indicators")
			.SetOptimize(1.5m, 2.5m, 0.25m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cooldownBars = Param(nameof(CooldownBars), 500)
			.SetRange(1, 1000)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "General");
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
		_prevWidth = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevWidth = 0;
		_cooldown = 0;

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

		if (!bollingerValue.IsFormed)
			return;

		var bb = (BollingerBandsValue)bollingerValue;

		if (bb.UpBand is not decimal upperBand ||
			bb.LowBand is not decimal lowerBand ||
			bb.MovingAverage is not decimal middleBand)
			return;

		var bbWidth = upperBand - lowerBand;

		if (_prevWidth == 0)
		{
			_prevWidth = bbWidth;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevWidth = bbWidth;
			return;
		}

		var isBBWidthExpanding = bbWidth > _prevWidth;

		if (Position == 0 && isBBWidthExpanding)
		{
			if (candle.ClosePrice > middleBand)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0 && !isBBWidthExpanding)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && !isBBWidthExpanding)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevWidth = bbWidth;
	}
}
