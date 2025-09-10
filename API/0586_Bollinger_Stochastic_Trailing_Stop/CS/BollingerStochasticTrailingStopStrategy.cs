using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using Bollinger Bands and Stochastic oscillator with ATR trailing stop.
/// </summary>
public class BollingerStochasticTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _stochLength;
	private readonly StrategyParam<int> _stochSmooth;
	private readonly StrategyParam<int> _stochOversold;
	private readonly StrategyParam<int> _stochOverbought;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;

	private decimal _trailingStopLong;
	private decimal _trailingStopShort;

	/// <summary>
	/// Candle type.
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
	/// Bollinger Bands deviation.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Stochastic period.
	/// </summary>
	public int StochLength
	{
		get => _stochLength.Value;
		set => _stochLength.Value = value;
	}

	/// <summary>
	/// Smoothing length for %K and %D.
	/// </summary>
	public int StochSmooth
	{
		get => _stochSmooth.Value;
		set => _stochSmooth.Value = value;
	}

	/// <summary>
	/// Stochastic oversold level.
	/// </summary>
	public int StochOversold
	{
		get => _stochOversold.Value;
		set => _stochOversold.Value = value;
	}

	/// <summary>
	/// Stochastic overbought level.
	/// </summary>
	public int StochOverbought
	{
		get => _stochOverbought.Value;
		set => _stochOverbought.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for trailing stop.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BollingerStochasticTrailingStopStrategy"/>.
	/// </summary>
	public BollingerStochasticTrailingStopStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetRange(10, 50)
			.SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
			.SetCanOptimize(true);

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetRange(1m, 3m)
			.SetDisplay("BB Deviation", "Bollinger Bands deviation", "Indicators")
			.SetCanOptimize(true);

		_stochLength = Param(nameof(StochLength), 14)
			.SetRange(5, 30)
			.SetDisplay("Stoch Length", "Stochastic period", "Indicators")
			.SetCanOptimize(true);

		_stochSmooth = Param(nameof(StochSmooth), 3)
			.SetRange(1, 10)
			.SetDisplay("Stoch Smooth", "Smoothing for %K and %D", "Indicators")
			.SetCanOptimize(true);

		_stochOversold = Param(nameof(StochOversold), 20)
			.SetRange(5, 30)
			.SetDisplay("Oversold", "Oversold level", "Signals")
			.SetCanOptimize(true);

		_stochOverbought = Param(nameof(StochOverbought), 80)
			.SetRange(70, 95)
			.SetDisplay("Overbought", "Overbought level", "Signals")
			.SetCanOptimize(true);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetRange(7, 28)
			.SetDisplay("ATR Period", "ATR calculation period", "Risk")
			.SetCanOptimize(true);

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetRange(0.5m, 3m)
			.SetDisplay("ATR Multiplier", "ATR multiplier for trailing stop", "Risk")
			.SetCanOptimize(true);
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		var stochastic = new StochasticOscillator
		{
			Length = StochLength,
			K = { Length = StochSmooth },
			D = { Length = StochSmooth },
		};

		var atr = new AverageTrueRange
		{
			Length = AtrPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, stochastic, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);

			var stochArea = CreateChartArea();
			DrawIndicator(stochArea, stochastic);

			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue stochValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bb = (BollingerBandsValue)bollingerValue;
		if (bb.UpBand is not decimal upper || bb.LowBand is not decimal lower)
			return;

		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.K is not decimal k)
			return;

		var atr = atrValue.ToDecimal();
		var price = candle.ClosePrice;

		if (price < lower && k < StochOversold && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_trailingStopLong = price - atr * AtrMultiplier;
		}
		else if (price > upper && k > StochOverbought && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_trailingStopShort = price + atr * AtrMultiplier;
		}

		if (Position > 0)
		{
			_trailingStopLong = Math.Max(_trailingStopLong, price - atr * AtrMultiplier);
			if (price <= _trailingStopLong)
			{
				SellMarket(Math.Abs(Position));
				_trailingStopLong = 0m;
			}
		}
		else if (Position < 0)
		{
			_trailingStopShort = Math.Min(_trailingStopShort, price + atr * AtrMultiplier);
			if (price >= _trailingStopShort)
			{
				BuyMarket(Math.Abs(Position));
				_trailingStopShort = 0m;
			}
		}
	}
}
