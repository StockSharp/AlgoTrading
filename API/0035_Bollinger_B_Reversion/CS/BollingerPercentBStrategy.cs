using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades on Bollinger %B indicator.
/// Bollinger %B shows where price is relative to the Bollinger Bands.
/// Values below 0 or above 1 indicate price outside the bands.
/// </summary>
public class BollingerPercentBStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<decimal> _exitValue;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

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
	/// Exit threshold for %B.
	/// </summary>
	public decimal ExitValue
	{
		get => _exitValue.Value;
		set => _exitValue.Value = value;
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
	/// Initialize the Bollinger %B Reversion strategy.
	/// </summary>
	public BollingerPercentBStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Indicators")
			.SetOptimize(10, 30, 5);

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
			.SetDisplay("Bollinger Deviation", "Deviation for Bollinger Bands calculation", "Indicators")
			.SetOptimize(1.5m, 2.5m, 0.25m);

		_exitValue = Param(nameof(ExitValue), 0.5m)
			.SetDisplay("Exit %B Value", "Exit threshold for %B", "Exit")
			.SetOptimize(0.3m, 0.7m, 0.1m);

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
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

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
			bb.LowBand is not decimal lowerBand)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Calculate Bollinger %B: (Price - Lower Band) / (Upper Band - Lower Band)
		decimal percentB = 0;
		if (upperBand != lowerBand)
			percentB = (candle.ClosePrice - lowerBand) / (upperBand - lowerBand);

		if (Position == 0)
		{
			if (percentB < 0)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (percentB > 1)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0)
		{
			if (percentB > ExitValue)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position < 0)
		{
			if (percentB < ExitValue)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
		}
	}
}
