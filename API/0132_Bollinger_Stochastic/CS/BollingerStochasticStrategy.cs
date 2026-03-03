using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining Bollinger Bands and Stochastic oscillator for mean-reversion.
/// Buys when price touches lower band with oversold stochastic, sells at upper band with overbought.
/// </summary>
public class BollingerStochasticStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<decimal> _stochOversold;
	private readonly StrategyParam<decimal> _stochOverbought;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _stochK;
	private int _cooldown;

	/// <summary>
	/// Data type for candles.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period for Bollinger Bands calculation.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for Bollinger Bands.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Stochastic oversold level.
	/// </summary>
	public decimal StochOversold
	{
		get => _stochOversold.Value;
		set => _stochOversold.Value = value;
	}

	/// <summary>
	/// Stochastic overbought level.
	/// </summary>
	public decimal StochOverbought
	{
		get => _stochOverbought.Value;
		set => _stochOverbought.Value = value;
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
	/// Initializes a new instance of the <see cref="BollingerStochasticStrategy"/>.
	/// </summary>
	public BollingerStochasticStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetRange(10, 50)
			.SetDisplay("BB Period", "Period for Bollinger Bands", "Bollinger Settings");

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
			.SetDisplay("BB Deviation", "Standard deviation multiplier", "Bollinger Settings");

		_stochOversold = Param(nameof(StochOversold), 20m)
			.SetDisplay("Oversold Level", "Stochastic oversold level", "Stochastic Settings");

		_stochOverbought = Param(nameof(StochOverbought), 80m)
			.SetDisplay("Overbought Level", "Stochastic overbought level", "Stochastic Settings");

		_cooldownBars = Param(nameof(CooldownBars), 50)
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
		_stochK = 50;
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

		var stochastic = new StochasticOscillator();

		var subscription = SubscribeCandles(CandleType);

		// Bind stochastic with BindEx
		subscription.BindEx(stochastic, OnStochastic);

		// Bind bollinger bands with BindEx
		subscription
			.BindEx(bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);

			var stochArea = CreateChartArea();
			if (stochArea != null)
				DrawIndicator(stochArea, stochastic);
		}
	}

	private void OnStochastic(ICandleMessage candle, IIndicatorValue stochValue)
	{
		var stoch = (IStochasticOscillatorValue)stochValue;
		if (stoch.K is decimal k)
			_stochK = k;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bbValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bb = (BollingerBandsValue)bbValue;
		if (bb.UpBand is not decimal upper ||
			bb.LowBand is not decimal lower ||
			bb.MovingAverage is not decimal middle)
			return;

		var close = candle.ClosePrice;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Buy: price at lower band + stochastic oversold
		if (close <= lower && _stochK < StochOversold && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Sell: price at upper band + stochastic overbought
		else if (close >= upper && _stochK > StochOverbought && Position == 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}

		// Exit long at middle band
		if (Position > 0 && close > middle)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Exit short at middle band
		else if (Position < 0 && close < middle)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
