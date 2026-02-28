namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Strategy combining VWAP and Stochastic indicators.
/// Buys when price is below VWAP and Stochastic is oversold.
/// Sells when price is above VWAP and Stochastic is overbought.
/// </summary>
public class VwapStochasticStrategy : Strategy
{
	private readonly StrategyParam<int> _stochKPeriod;
	private readonly StrategyParam<int> _stochDPeriod;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Stochastic %K smoothing period.
	/// </summary>
	public int StochKPeriod
	{
		get => _stochKPeriod.Value;
		set => _stochKPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %D period.
	/// </summary>
	public int StochDPeriod
	{
		get => _stochDPeriod.Value;
		set => _stochDPeriod.Value = value;
	}

	/// <summary>
	/// Overbought level for stochastic (0-100).
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Oversold level for stochastic (0-100).
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy.
	/// </summary>
	public VwapStochasticStrategy()
	{
		_stochKPeriod = Param(nameof(StochKPeriod), 3)
			.SetDisplay("Stoch %K", "Smoothing period for %K line", "Indicators");

		_stochDPeriod = Param(nameof(StochDPeriod), 3)
			.SetDisplay("Stoch %D", "Smoothing period for %D line", "Indicators");

		_overboughtLevel = Param(nameof(OverboughtLevel), 80m)
			.SetDisplay("Overbought Level", "Level considered overbought", "Trading Levels");

		_oversoldLevel = Param(nameof(OversoldLevel), 20m)
			.SetDisplay("Oversold Level", "Level considered oversold", "Trading Levels");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var vwap = new VolumeWeightedMovingAverage();
		var stochastic = new StochasticOscillator
		{
			K = { Length = StochKPeriod },
			D = { Length = StochDPeriod },
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(vwap, stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, vwap);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue vwapValue, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (vwapValue.IsEmpty || stochValue.IsEmpty)
			return;

		var stochTyped = (StochasticOscillatorValue)stochValue;

		if (stochTyped.K is not decimal kValue)
			return;

		var vwapDec = vwapValue.ToDecimal();

		if (candle.ClosePrice < vwapDec && kValue < OversoldLevel && Position <= 0)
		{
			BuyMarket();
		}
		else if (candle.ClosePrice > vwapDec && kValue > OverboughtLevel && Position >= 0)
		{
			SellMarket();
		}
	}
}
