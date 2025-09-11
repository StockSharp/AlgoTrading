using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Stochastic oscillator crossovers.
/// Enters long when %K crosses above %D below 80.
/// Enters short when %K crosses below %D above 20.
/// Uses take-profit defined in ticks.
/// </summary>
public class HowToUseLeverageAndMarginStrategy : Strategy
{
	private readonly StrategyParam<int> _stochPeriod;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevK;
	private decimal _prevD;
	private bool _isInitialized;

	/// <summary>
	/// Stochastic period.
	/// </summary>
	public int StochPeriod
	{
		get => _stochPeriod.Value;
		set => _stochPeriod.Value = value;
	}

	/// <summary>
	/// %K smoothing period.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// %D period.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Take profit distance in ticks.
	/// </summary>
	public int TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HowToUseLeverageAndMarginStrategy"/> class.
	/// </summary>
	public HowToUseLeverageAndMarginStrategy()
	{
		_stochPeriod = Param(nameof(StochPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Period", "Lookback period for Stochastic", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_kPeriod = Param(nameof(KPeriod), 4)
			.SetGreaterThanZero()
			.SetDisplay("%K Period", "Smoothing period for %K line", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("%D Period", "Smoothing period for %D line", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 100)
			.SetRange(10, 500)
			.SetDisplay("Take Profit Ticks", "Take profit distance in ticks", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevK = 0m;
		_prevD = 0m;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var stochastic = new StochasticOscillator
		{
			Length = StochPeriod,
			K = { Length = KPeriod },
			D = { Length = DPeriod },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stochastic, ProcessCandle)
			.Start();

		var step = Security?.PriceStep ?? 1m;
		StartProtection(
			takeProfit: new Unit(TakeProfitTicks * step, UnitTypes.Absolute),
			stopLoss: default);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var stoch = (StochasticOscillatorValue)stochValue;
		if (stoch.K is not decimal kValue || stoch.D is not decimal dValue)
			return;

		if (!_isInitialized)
		{
			_prevK = kValue;
			_prevD = dValue;
			_isInitialized = true;
			return;
		}

		var crossAbove = _prevK <= _prevD && kValue > dValue;
		var crossBelow = _prevK >= _prevD && kValue < dValue;

		var volume = Volume + Math.Abs(Position);

		if (crossAbove && kValue < 80 && Position <= 0)
			BuyMarket(volume);
		else if (crossBelow && kValue > 20 && Position >= 0)
			SellMarket(volume);

		_prevK = kValue;
		_prevD = dValue;
	}
}

