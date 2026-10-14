using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Stochastic Keltner strategy.
/// The Keltner Channel is the EmaPeriod EMA plus and minus KeltnerMultiplier times the AtrPeriod ATR; %K is the stochastic over StochPeriod
/// candles smoothed over StochK candles. %K below StochOversold with a close below the lower band goes long and %K above StochOverbought
/// with a close above the upper band goes short, reversing an opposite position. A position closes once price returns to the middle band.
/// The stop lies AtrMultiplier ATR from the entry close and is checked on candle closes.
/// </summary>
public class StochasticKeltnerStrategy : Strategy
{
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<int> _stochK;
	private readonly StrategyParam<decimal> _stochOversold;
	private readonly StrategyParam<decimal> _stochOverbought;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _keltnerMultiplier;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopPrice;

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
	/// Period of the channel EMA.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the channel width.
	/// </summary>
	public decimal KeltnerMultiplier
	{
		get => _keltnerMultiplier.Value;
		set => _keltnerMultiplier.Value = value;
	}

	/// <summary>
	/// Period of the channel and stop ATR.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Stop distance from the entry in ATRs.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
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
	public StochasticKeltnerStrategy()
	{
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

		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period of the channel EMA", "Keltner");

		_keltnerMultiplier = Param(nameof(KeltnerMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Keltner Multiplier", "ATR multiplier of the channel width", "Keltner");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period of the channel and stop ATR", "Keltner");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetNotNegative()
			.SetDisplay("ATR Multiplier", "Stop distance from the entry in ATRs", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_stopPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_stopPrice = default;

		var stochastic = new StochasticOscillator
		{
			K = { Length = StochPeriod },
			D = { Length = StochK },
		};
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stochastic, ema, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, stochastic);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochasticValue, IIndicatorValue emaValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!stochasticValue.IsFormed || !emaValue.IsFormed || !atrValue.IsFormed)
			return;

		// The smoothed %K is the moving average the core oscillator exposes as D.
		if (stochasticValue is not IStochasticOscillatorValue { D: decimal k })
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var middle = emaValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();
		var upper = middle + KeltnerMultiplier * atr;
		var lower = middle - KeltnerMultiplier * atr;
		var close = candle.ClosePrice;

		if (k < StochOversold && close < lower && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = close - AtrMultiplier * atr;
		}
		else if (k > StochOverbought && close > upper && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = close + AtrMultiplier * atr;
		}
		else if (Position > 0 && (close >= middle || (AtrMultiplier > 0 && close <= _stopPrice)))
		{
			SellMarket(Position);
		}
		else if (Position < 0 && (close <= middle || (AtrMultiplier > 0 && close >= _stopPrice)))
		{
			BuyMarket(-Position);
		}
	}
}
