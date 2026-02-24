using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// JBrainTrend1Stop strategy: detects trend reversals using ATR bands and stochastic filter.
/// Uses ATR-based upper/lower bands around smoothed price; stochastic confirms direction.
/// </summary>
public class JBrainTrend1StopStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _stochasticPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _stochUpper;
	private readonly StrategyParam<decimal> _stochLower;

	private decimal _prevClose;
	private bool _hasPrev;

	/// <summary>
	/// Constructor.
	/// </summary>
	public JBrainTrend1StopStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR length for bands", "Indicators");

		_stochasticPeriod = Param(nameof(StochasticPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Period", "Stochastic K period", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Band width multiplier", "Signals");

		_stochUpper = Param(nameof(StochUpper), 70m)
			.SetDisplay("Stoch Upper", "Overbought level", "Signals");

		_stochLower = Param(nameof(StochLower), 30m)
			.SetDisplay("Stoch Lower", "Oversold level", "Signals");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public int StochasticPeriod
	{
		get => _stochasticPeriod.Value;
		set => _stochasticPeriod.Value = value;
	}

	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	public decimal StochUpper
	{
		get => _stochUpper.Value;
		set => _stochUpper.Value = value;
	}

	public decimal StochLower
	{
		get => _stochLower.Value;
		set => _stochLower.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var atr = new AverageTrueRange { Length = AtrPeriod };
		var stoch = new StochasticOscillator
		{
			K = { Length = StochasticPeriod },
			D = { Length = 3 }
		};
		var ema = new EMA { Length = 20 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(atr, stoch, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue, IIndicatorValue stochValue, IIndicatorValue emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var atr = atrValue.ToDecimal();
		var ema = emaValue.ToDecimal();

		var sv = (StochasticOscillatorValue)stochValue;
		if (sv.K is not decimal stochK)
			return;

		var upperBand = ema + atr * AtrMultiplier;
		var lowerBand = ema - atr * AtrMultiplier;

		// Buy: price breaks above upper band with stoch from oversold
		if (candle.ClosePrice > upperBand && stochK < StochUpper && Position <= 0)
		{
			BuyMarket();
		}
		// Sell: price breaks below lower band with stoch from overbought
		else if (candle.ClosePrice < lowerBand && stochK > StochLower && Position >= 0)
		{
			SellMarket();
		}

		_prevClose = candle.ClosePrice;
		_hasPrev = true;
	}
}
