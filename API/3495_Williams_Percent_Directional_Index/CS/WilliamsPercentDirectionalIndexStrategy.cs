using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Williams %R confirmed by directional movement and momentum oscillators.
/// </summary>
public class WilliamsPercentDirectionalIndexStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _williamsPeriod;
	private readonly StrategyParam<int> _directionalPeriod;
	private readonly StrategyParam<int> _moneyFlowPeriod;
	private readonly StrategyParam<decimal> _moneyFlowLevel;
	private readonly StrategyParam<int> _stochasticKPeriod;
	private readonly StrategyParam<int> _stochasticDPeriod;
	private readonly StrategyParam<int> _stochasticSmoothing;

	private WilliamsR _williams = null!;
	private AverageDirectionalIndex _directional = null!;
	private MoneyFlowIndex _moneyFlow = null!;
	private StochasticOscillator _stochastic = null!;

	private decimal? _williamsPrev1;
	private decimal? _williamsPrev2;
	private decimal? _directionalDiffPrev1;
	private decimal? _directionalDiffPrev2;
	private decimal? _moneyFlowPrev1;
	private decimal? _moneyFlowPrev2;
	private decimal? _stochasticPrev1;
	private decimal? _stochasticPrev2;

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Williams %R look-back period.
	/// </summary>
	public int WilliamsPeriod
	{
		get => _williamsPeriod.Value;
		set => _williamsPeriod.Value = value;
	}

	/// <summary>
	/// Directional indicator period.
	/// </summary>
	public int DirectionalPeriod
	{
		get => _directionalPeriod.Value;
		set => _directionalPeriod.Value = value;
	}

	/// <summary>
	/// Money Flow Index look-back period.
	/// </summary>
	public int MoneyFlowPeriod
	{
		get => _moneyFlowPeriod.Value;
		set => _moneyFlowPeriod.Value = value;
	}

	/// <summary>
	/// Overbought level for the Money Flow Index.
	/// </summary>
	public decimal MoneyFlowLevel
	{
		get => _moneyFlowLevel.Value;
		set => _moneyFlowLevel.Value = value;
	}

	/// <summary>
	/// Stochastic oscillator %K period.
	/// </summary>
	public int StochasticKPeriod
	{
		get => _stochasticKPeriod.Value;
		set => _stochasticKPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic oscillator %D period.
	/// </summary>
	public int StochasticDPeriod
	{
		get => _stochasticDPeriod.Value;
		set => _stochasticDPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic oscillator smoothing period.
	/// </summary>
	public int StochasticSmoothing
	{
		get => _stochasticSmoothing.Value;
		set => _stochasticSmoothing.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="WilliamsPercentDirectionalIndexStrategy"/> class.
	/// </summary>
	public WilliamsPercentDirectionalIndexStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Time frame used for signals", "General");

		_williamsPeriod = Param(nameof(WilliamsPeriod), 42)
			.SetGreaterThanZero()
			.SetDisplay("Williams %R Period", "Look-back period for Williams %R", "Indicators")
			.SetCanOptimize(true);

		_directionalPeriod = Param(nameof(DirectionalPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Directional Period", "Length of the directional index", "Indicators")
			.SetCanOptimize(true);

		_moneyFlowPeriod = Param(nameof(MoneyFlowPeriod), 19)
			.SetGreaterThanZero()
			.SetDisplay("MFI Period", "Money Flow Index period", "Indicators")
			.SetCanOptimize(true);

		_moneyFlowLevel = Param(nameof(MoneyFlowLevel), 79m)
			.SetRange(50m, 90m)
			.SetDisplay("MFI Level", "Overbought threshold for MFI", "Indicators")
			.SetCanOptimize(true);

		_stochasticKPeriod = Param(nameof(StochasticKPeriod), 22)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %K", "%K period of the stochastic oscillator", "Indicators")
			.SetCanOptimize(true);

		_stochasticDPeriod = Param(nameof(StochasticDPeriod), 16)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %D", "%D period of the stochastic oscillator", "Indicators")
			.SetCanOptimize(true);

		_stochasticSmoothing = Param(nameof(StochasticSmoothing), 21)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Smoothing", "Slowing period for the stochastic oscillator", "Indicators")
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

		_williams = new WilliamsR { Length = WilliamsPeriod };
		_directional = new AverageDirectionalIndex { Length = DirectionalPeriod };
		_moneyFlow = new MoneyFlowIndex { Length = MoneyFlowPeriod };
		_stochastic = new StochasticOscillator
		{
			KPeriod = StochasticKPeriod,
			DPeriod = StochasticDPeriod,
			Smooth = StochasticSmoothing,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_williams, _directional, _moneyFlow, _stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _williams);
			DrawIndicator(area, _directional);
			DrawIndicator(area, _moneyFlow);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal williamsValue, IIndicatorValue directionalValue, decimal moneyFlowValue, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_williams.IsFormed || !_directional.IsFormed || !_moneyFlow.IsFormed || !_stochastic.IsFormed)
			return;

		if (directionalValue is not AverageDirectionalIndexValue directionalTyped)
			return;

		if (stochasticValue is not StochasticOscillatorValue stochasticTyped)
			return;

		if (directionalTyped.PlusDI is not decimal plusDi || directionalTyped.MinusDI is not decimal minusDi)
			return;

		if (stochasticTyped.K is not decimal stochasticK)
			return;

		var directionalDiff = plusDi - minusDi;

		var canOpenLong = false;
		var canOpenShort = false;

		if (_williamsPrev2 is decimal williamsPrev2 && _williamsPrev1 is decimal williamsPrev1 &&
			_directionalDiffPrev2 is decimal directionalPrev2 && _directionalDiffPrev1 is decimal directionalPrev1)
		{
			var williamsRising = williamsPrev2 > williamsPrev1;
			var williamsFalling = williamsPrev2 < williamsPrev1;

			var directionalCrossUp = directionalPrev2 < 0m && directionalPrev1 > 0m;
			var directionalCrossDown = directionalPrev2 > 0m && directionalPrev1 < 0m;

			canOpenLong = williamsRising && directionalCrossUp;
			canOpenShort = williamsFalling && directionalCrossDown;
		}

		if (canOpenLong && canOpenShort)
		{
			canOpenLong = false;
			canOpenShort = false;
		}

		if (canOpenLong && Position <= 0)
			BuyMarket();
		else if (canOpenShort && Position >= 0)
			SellMarket();

		var exitLong = false;
		var exitShort = false;

		if (_moneyFlowPrev2 is decimal moneyFlowPrev2)
		{
			exitLong |= moneyFlowPrev2 > MoneyFlowLevel;
			exitShort |= moneyFlowPrev2 < 100m - MoneyFlowLevel;
		}

		if (_stochasticPrev2 is decimal stochasticPrev2 && _stochasticPrev1 is decimal stochasticPrev1)
		{
			exitLong |= stochasticPrev2 > stochasticPrev1 && stochasticPrev1 < stochasticK;
			exitShort |= stochasticPrev2 < stochasticPrev1 && stochasticPrev1 > stochasticK;
		}

		if (exitLong && Position > 0)
			ClosePosition();
		else if (exitShort && Position < 0)
			ClosePosition();

		_williamsPrev2 = _williamsPrev1;
		_williamsPrev1 = williamsValue;

		_directionalDiffPrev2 = _directionalDiffPrev1;
		_directionalDiffPrev1 = directionalDiff;

		_moneyFlowPrev2 = _moneyFlowPrev1;
		_moneyFlowPrev1 = moneyFlowValue;

		_stochasticPrev2 = _stochasticPrev1;
		_stochasticPrev1 = stochasticK;
	}
}
