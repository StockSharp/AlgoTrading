using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades hammer and hanging man patterns confirmed by the stochastic oscillator.
/// </summary>
public class HammerHangingStochasticStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stochPeriodK;
	private readonly StrategyParam<int> _stochPeriodD;
	private readonly StrategyParam<int> _stochPeriodSlow;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _exitLowerLevel;
	private readonly StrategyParam<decimal> _exitUpperLevel;
	private readonly StrategyParam<decimal> _maxBodyRatio;
	private readonly StrategyParam<decimal> _lowerShadowMultiplier;
	private readonly StrategyParam<decimal> _upperShadowMultiplier;

	private StochasticOscillator? _stochastic;
	private decimal? _previousStochD;
	private decimal? _previous2StochD;

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public HammerHangingStochasticStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for pattern detection", "General");

		_stochPeriodK = Param(nameof(StochPeriodK), 15)
			.SetDisplay("Stochastic %K", "%K period of the stochastic oscillator", "Stochastic")
			.SetCanOptimize(true);

		_stochPeriodD = Param(nameof(StochPeriodD), 49)
			.SetDisplay("Stochastic %D", "%D period (smoothing) of the stochastic oscillator", "Stochastic")
			.SetCanOptimize(true);

		_stochPeriodSlow = Param(nameof(StochPeriodSlow), 25)
			.SetDisplay("Stochastic Slow", "Slow smoothing period for %K", "Stochastic")
			.SetCanOptimize(true);

		_oversoldLevel = Param(nameof(OversoldLevel), 30m)
			.SetDisplay("Oversold Level", "Threshold used to confirm hammer signals", "Stochastic")
			.SetCanOptimize(true);

		_overboughtLevel = Param(nameof(OverboughtLevel), 70m)
			.SetDisplay("Overbought Level", "Threshold used to confirm hanging man signals", "Stochastic")
			.SetCanOptimize(true);

		_exitLowerLevel = Param(nameof(ExitLowerLevel), 20m)
			.SetDisplay("Exit Lower Level", "Level where long trades are closed on upward cross", "Risk")
			.SetCanOptimize(true);

		_exitUpperLevel = Param(nameof(ExitUpperLevel), 80m)
			.SetDisplay("Exit Upper Level", "Level where trades are closed on extreme cross", "Risk")
			.SetCanOptimize(true);

		_maxBodyRatio = Param(nameof(MaxBodyRatio), 0.35m)
			.SetDisplay("Max Body Ratio", "Maximum candle body relative to range", "Pattern")
			.SetCanOptimize(true);

		_lowerShadowMultiplier = Param(nameof(LowerShadowMultiplier), 2.5m)
			.SetDisplay("Lower Shadow Multiplier", "Minimum lower shadow length in body multiples", "Pattern")
			.SetCanOptimize(true);

		_upperShadowMultiplier = Param(nameof(UpperShadowMultiplier), 0.3m)
			.SetDisplay("Upper Shadow Multiplier", "Maximum upper shadow relative to body", "Pattern")
			.SetCanOptimize(true);
	}

	/// <summary>
	/// Candle type and timeframe used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stochastic %K length.
	/// </summary>
	public int StochPeriodK
	{
		get => _stochPeriodK.Value;
		set => _stochPeriodK.Value = value;
	}

	/// <summary>
	/// Stochastic %D length.
	/// </summary>
	public int StochPeriodD
	{
		get => _stochPeriodD.Value;
		set => _stochPeriodD.Value = value;
	}

	/// <summary>
	/// Stochastic slow smoothing length.
	/// </summary>
	public int StochPeriodSlow
	{
		get => _stochPeriodSlow.Value;
		set => _stochPeriodSlow.Value = value;
	}

	/// <summary>
	/// Oversold threshold required before entering a long trade.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Overbought threshold required before entering a short trade.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// Lower threshold used to close long trades on a rising stochastic.
	/// </summary>
	public decimal ExitLowerLevel
	{
		get => _exitLowerLevel.Value;
		set => _exitLowerLevel.Value = value;
	}

	/// <summary>
	/// Upper threshold used to close trades once momentum becomes extreme.
	/// </summary>
	public decimal ExitUpperLevel
	{
		get => _exitUpperLevel.Value;
		set => _exitUpperLevel.Value = value;
	}

	/// <summary>
	/// Maximum body-to-range ratio to qualify as a hammer or hanging man.
	/// </summary>
	public decimal MaxBodyRatio
	{
		get => _maxBodyRatio.Value;
		set => _maxBodyRatio.Value = value;
	}

	/// <summary>
	/// Required lower shadow length expressed in body multiples.
	/// </summary>
	public decimal LowerShadowMultiplier
	{
		get => _lowerShadowMultiplier.Value;
		set => _lowerShadowMultiplier.Value = value;
	}

	/// <summary>
	/// Maximum upper shadow length expressed in body multiples.
	/// </summary>
	public decimal UpperShadowMultiplier
	{
		get => _upperShadowMultiplier.Value;
		set => _upperShadowMultiplier.Value = value;
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
		_previousStochD = null;
		_previous2StochD = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_stochastic = new StochasticOscillator
		{
			Length = StochPeriodK,
			K = { Length = StochPeriodSlow },
			D = { Length = StochPeriodD },
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(_stochastic, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!stochValue.IsFinal)
			return;

		// Convert indicator output to typed stochastic value.
		var stoch = (StochasticOscillatorValue)stochValue;

		if (stoch.D is not decimal currentD)
			return;

		var prev1 = _previousStochD;
		var prev2 = _previous2StochD;

		// Store first stochastic readings until the rolling window is filled.
		if (prev1 is null)
		{
			_previousStochD = currentD;
			_previous2StochD = null;
			return;
		}

		if (prev2 is null)
		{
			_previous2StochD = prev1;
			_previousStochD = currentD;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previous2StochD = prev1;
			_previousStochD = currentD;
			return;
		}

		var prevValue = prev1.Value;
		var prev2Value = prev2.Value;

		// Evaluate candlestick patterns on the finished bar.
		var hammer = IsHammer(candle);
		var hangingMan = IsHangingMan(candle);

		// Enter trades only when the stochastic confirms the pattern.
		if (Position <= 0 && hammer && prevValue < OversoldLevel)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (Position >= 0 && hangingMan && prevValue > OverboughtLevel)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}

		// Close positions when stochastic crosses configured thresholds.
		if (Position > 0 && ShouldCloseLong(prev2Value, prevValue))
		{
			SellMarket(Position);
		}
		else if (Position < 0 && ShouldCloseShort(prev2Value, prevValue))
		{
			BuyMarket(Math.Abs(Position));
		}

		_previous2StochD = prev1;
		_previousStochD = currentD;
	}

	// Determine if a long position should be closed based on stochastic crossings.
	private bool ShouldCloseLong(decimal prev2, decimal prev1)
	{
		return (prev1 > ExitLowerLevel && prev2 <= ExitLowerLevel) ||
			(prev1 > ExitUpperLevel && prev2 <= ExitUpperLevel);
	}

	// Determine if a short position should be closed based on stochastic crossings.
	private bool ShouldCloseShort(decimal prev2, decimal prev1)
	{
		return (prev1 < ExitUpperLevel && prev2 >= ExitUpperLevel) ||
			(prev1 < ExitLowerLevel && prev2 >= ExitLowerLevel);
	}

	// Basic hammer detection using body and shadow ratios.
	private bool IsHammer(ICandleMessage candle)
	{
		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var range = candle.HighPrice - candle.LowPrice;

		if (range <= 0m)
			return false;

		var lowerShadow = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;
		var upperShadow = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);

		if (body <= 0m)
			return false;

		var bodyRatio = body / range;

		return candle.ClosePrice > candle.OpenPrice &&
			bodyRatio <= MaxBodyRatio &&
			lowerShadow >= LowerShadowMultiplier * body &&
			upperShadow <= UpperShadowMultiplier * body;
	}

	// Basic hanging man detection mirroring the hammer conditions for bearish setups.
	private bool IsHangingMan(ICandleMessage candle)
	{
		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var range = candle.HighPrice - candle.LowPrice;

		if (range <= 0m)
			return false;

		var lowerShadow = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice;
		var upperShadow = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice);

		if (body <= 0m)
			return false;

		var bodyRatio = body / range;

		return candle.ClosePrice < candle.OpenPrice &&
			bodyRatio <= MaxBodyRatio &&
			lowerShadow >= LowerShadowMultiplier * body &&
			upperShadow <= UpperShadowMultiplier * body;
	}
}