using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ichimoku Stochastic strategy.
/// A close above the cloud with Tenkan-sen above Kijun-sen and %K below StochOversold goes long, a close below the cloud with Tenkan-sen
/// below Kijun-sen and %K above StochOverbought goes short, reversing an opposite position; %K is the stochastic over StochPeriod candles smoothed over StochK candles. The cloud is the stop:
/// a long closes when price closes below the cloud and a short when it closes above it.
/// </summary>
public class IchimokuStochasticStrategy : Strategy
{
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouPeriod;
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<int> _stochK;
	private readonly StrategyParam<decimal> _stochOversold;
	private readonly StrategyParam<decimal> _stochOverbought;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Period of Tenkan-sen.
	/// </summary>
	public int TenkanPeriod
	{
		get => _tenkanPeriod.Value;
		set => _tenkanPeriod.Value = value;
	}

	/// <summary>
	/// Period of Kijun-sen.
	/// </summary>
	public int KijunPeriod
	{
		get => _kijunPeriod.Value;
		set => _kijunPeriod.Value = value;
	}

	/// <summary>
	/// Period of Senkou Span B.
	/// </summary>
	public int SenkouPeriod
	{
		get => _senkouPeriod.Value;
		set => _senkouPeriod.Value = value;
	}

	/// <summary>
	/// Lookback period of the raw stochastic.
	/// </summary>
	public int StochPeriod
	{
		get => _stochPeriod.Value;
		set => _stochPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing period of %K.
	/// </summary>
	public int StochK
	{
		get => _stochK.Value;
		set => _stochK.Value = value;
	}

	/// <summary>
	/// %K level for longs.
	/// </summary>
	public decimal StochOversold
	{
		get => _stochOversold.Value;
		set => _stochOversold.Value = value;
	}

	/// <summary>
	/// %K level for shorts.
	/// </summary>
	public decimal StochOverbought
	{
		get => _stochOverbought.Value;
		set => _stochOverbought.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public IchimokuStochasticStrategy()
	{
		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Tenkan Period", "Period of Tenkan-sen", "Ichimoku");

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Kijun Period", "Period of Kijun-sen", "Ichimoku");

		_senkouPeriod = Param(nameof(SenkouPeriod), 52)
			.SetGreaterThanZero()
			.SetDisplay("Senkou Period", "Period of Senkou Span B", "Ichimoku");

		_stochPeriod = Param(nameof(StochPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Period", "Lookback period of the raw stochastic", "Stochastic");

		_stochK = Param(nameof(StochK), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %K", "Smoothing period of %K", "Stochastic");

		_stochOversold = Param(nameof(StochOversold), 20m)
			.SetDisplay("Stochastic Oversold", "%K level for longs", "Stochastic");

		_stochOverbought = Param(nameof(StochOverbought), 80m)
			.SetDisplay("Stochastic Overbought", "%K level for shorts", "Stochastic");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		var ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouPeriod }
		};
		// The D line of the core oscillator is the smoothed %K.
		var stochastic = new StochasticOscillator
		{
			K = { Length = StochPeriod },
			D = { Length = StochK },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ichimoku, stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ichimoku);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, stochastic);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (ichimokuValue is not IIchimokuValue { Tenkan: decimal tenkan, Kijun: decimal kijun, SenkouA: decimal senkouA, SenkouB: decimal senkouB })
			return;

		if (!stochasticValue.IsFormed || stochasticValue is not IStochasticOscillatorValue { D: decimal k })
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var cloudTop = Math.Max(senkouA, senkouB);
		var cloudBottom = Math.Min(senkouA, senkouB);

		if (close > cloudTop && tenkan > kijun && k < StochOversold && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (close < cloudBottom && tenkan < kijun && k > StochOverbought && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && close < cloudBottom)
			SellMarket(Position);
		else if (Position < 0 && close > cloudTop)
			BuyMarket(-Position);
	}
}
