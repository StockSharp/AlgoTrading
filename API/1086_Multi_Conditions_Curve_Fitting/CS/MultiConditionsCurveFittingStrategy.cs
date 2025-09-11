namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy combining EMA crossover, RSI and Stochastic oscillator.
/// </summary>
public class MultiConditionsCurveFittingStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<int> _stochLength;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	/// <summary>
	/// RSI period length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// Stochastic oscillator period.
	/// </summary>
	public int StochLength
	{
		get => _stochLength.Value;
		set => _stochLength.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private RelativeStrengthIndex _rsi;
	private StochasticOscillator _stoch;

	/// <summary>
	/// Initializes a new instance of <see cref="MultiConditionsCurveFittingStrategy"/>.
	/// </summary>
	public MultiConditionsCurveFittingStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 10).SetCanOptimize();
		_slowEmaLength = Param(nameof(SlowEmaLength), 25).SetCanOptimize();
		_rsiLength = Param(nameof(RsiLength), 14).SetCanOptimize();
		_rsiOverbought = Param(nameof(RsiOverbought), 80m).SetCanOptimize();
		_rsiOversold = Param(nameof(RsiOversold), 20m).SetCanOptimize();
		_stochLength = Param(nameof(StochLength), 14).SetCanOptimize();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame());
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

		StartProtection();

		_fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		_slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_stoch = new StochasticOscillator
		{
			K = { Length = StochLength },
			D = { Length = 3 }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(_fastEma, _slowEma, _rsi, _stoch, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _slowEma);
			DrawIndicator(area, _rsi);

			var stochArea = CreateChartArea();
			DrawIndicator(stochArea, _stoch);

			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastEmaValue, IIndicatorValue slowEmaValue, IIndicatorValue rsiValue, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fastEma = fastEmaValue.ToDecimal();
		var slowEma = slowEmaValue.ToDecimal();
		var rsi = rsiValue.ToDecimal();

		var stochTyped = (StochasticOscillatorValue)stochValue;
		var k = stochTyped.K;
		var d = stochTyped.D;

		var longCondition = fastEma > slowEma && rsi < RsiOversold && k < 20m;
		var shortCondition = fastEma < slowEma && rsi > RsiOverbought && k > 80m;

		if (longCondition && Position <= 0)
			BuyMarket();

		if (shortCondition && Position >= 0)
			SellMarket();

		if (Position > 0 && (fastEma < slowEma || rsi > RsiOverbought || k > d))
			SellMarket();

		if (Position < 0 && (fastEma > slowEma || rsi < RsiOversold || k < d))
			BuyMarket();
	}
}
