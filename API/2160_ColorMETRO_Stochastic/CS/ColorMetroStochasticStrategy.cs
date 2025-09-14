using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ColorMETRO Stochastic crossover strategy based on the Stochastic Oscillator.
/// Generates market entries when %K crosses %D.
/// </summary>
public class ColorMetroStochasticStrategy : Strategy
{
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _slowing;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevK;
	private decimal? _prevD;

	/// <summary>
	/// %K calculation period.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// %D smoothing period.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Additional smoothing value.
	/// </summary>
	public int Slowing
	{
		get => _slowing.Value;
		set => _slowing.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public ColorMetroStochasticStrategy()
	{
		_kPeriod = Param(nameof(KPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("K Period")
			.SetCanOptimize(true)
			.SetOptimize(3, 15, 1);

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("D Period")
			.SetCanOptimize(true)
			.SetOptimize(2, 10, 1);

		_slowing = Param(nameof(Slowing), 3)
			.SetGreaterThanZero()
			.SetDisplay("Slowing")
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Candle Type");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		var stoch = new StochasticOscillator
		{
			K = { Length = Slowing },
			D = { Length = DPeriod },
			Length = KPeriod,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stoch, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stoch);
			DrawOwnTrades(area);
		}

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(2, UnitTypes.Percent));

		base.OnStarted(time);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var stoch = (StochasticOscillatorValue)stochValue;

		if (stoch.K is not decimal k || stoch.D is not decimal d)
			return;

		if (_prevK is decimal prevK && _prevD is decimal prevD)
		{
			// Bullish crossover: %K rises above %D
			if (prevK <= prevD && k > d && Position <= 0)
				BuyMarket();

			// Bearish crossover: %K falls below %D
			else if (prevK >= prevD && k < d && Position >= 0)
				SellMarket();
		}

		_prevK = k;
		_prevD = d;
	}
}