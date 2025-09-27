namespace StockSharp.Samples.Strategies;

using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

using StockSharp.Algo;

/// <summary>
/// Broadening Top pattern strategy that follows the original MQL logic.
/// </summary>
public class BroadeningTopStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumBuyThreshold;
	private readonly StrategyParam<decimal> _momentumSellThreshold;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _enableLongs;
	private readonly StrategyParam<bool> _enableShorts;

	private LinearWeightedMovingAverage _fastMa;
	private LinearWeightedMovingAverage _slowMa;
	private Momentum _momentum;
	private MovingAverageConvergenceDivergenceSignal _macd;

	private decimal _pipSize;
	private decimal? _momentumCurrent;
	private decimal? _momentumPrev1;
	private decimal? _momentumPrev2;

	/// <summary>
	/// Initializes a new instance of the <see cref="BroadeningTopStrategy"/> class.
	/// </summary>
	public BroadeningTopStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Trade size in lots or contracts", "Trading");

		_fastMaLength = Param(nameof(FastMaLength), 6)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Length of the fast LWMA", "Filters")
			.SetCanOptimize(true);

		_slowMaLength = Param(nameof(SlowMaLength), 85)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Length of the slow LWMA", "Filters")
			.SetCanOptimize(true);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Momentum lookback period", "Filters")
			.SetCanOptimize(true);

		_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Buy Threshold", "Minimum distance from 100 for long setups", "Filters")
			.SetCanOptimize(true);

		_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Sell Threshold", "Minimum distance from 100 for short setups", "Filters")
			.SetCanOptimize(true);

		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA length for MACD", "Filters");

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA length for MACD", "Filters");

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal EMA length for MACD", "Filters");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetDisplay("Take Profit (pips)", "Distance to place the take-profit", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 20)
			.SetDisplay("Stop Loss (pips)", "Distance to place the stop-loss", "Risk");

		_trailingStopPips = Param(nameof(TrailingStopPips), 40)
			.SetDisplay("Trailing Stop (pips)", "Trailing distance for profit protection", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 10)
			.SetDisplay("Trailing Step (pips)", "Additional distance before adjusting the trailing stop", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candle subscription", "General");

		_enableLongs = Param(nameof(EnableLongs), true)
			.SetDisplay("Enable Longs", "Allow long signals", "Trading");

		_enableShorts = Param(nameof(EnableShorts), true)
			.SetDisplay("Enable Shorts", "Allow short signals", "Trading");
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Fast moving average length.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Slow moving average length.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Momentum calculation period.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimum momentum distance for long trades.
	/// </summary>
	public decimal MomentumBuyThreshold
	{
		get => _momentumBuyThreshold.Value;
		set => _momentumBuyThreshold.Value = value;
	}

	/// <summary>
	/// Minimum momentum distance for short trades.
	/// </summary>
	public decimal MomentumSellThreshold
	{
		get => _momentumSellThreshold.Value;
		set => _momentumSellThreshold.Value = value;
	}

	/// <summary>
	/// Fast EMA length for MACD.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// Slow EMA length for MACD.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// Signal EMA length for MACD.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in price steps.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Extra distance before the trailing stop is adjusted.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
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
	/// Enables long trades.
	/// </summary>
	public bool EnableLongs
	{
		get => _enableLongs.Value;
		set => _enableLongs.Value = value;
	}

	/// <summary>
	/// Enables short trades.
	/// </summary>
	public bool EnableShorts
	{
		get => _enableShorts.Value;
		set => _enableShorts.Value = value;
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

		_fastMa = null;
		_slowMa = null;
		_momentum = null;
		_macd = null;

		_pipSize = 0m;
		_momentumCurrent = null;
		_momentumPrev1 = null;
		_momentumPrev2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		_fastMa = new LinearWeightedMovingAverage { Length = FastMaLength };
		_slowMa = new LinearWeightedMovingAverage { Length = SlowMaLength };
		_momentum = new Momentum { Length = MomentumPeriod };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Fast = MacdFast,
			Slow = MacdSlow,
			Signal = MacdSignal
		};

		_pipSize = CalculatePipSize();

		var takeProfit = TakeProfitPips > 0 ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute) : null;
		var stopLoss = StopLossPips > 0 ? new Unit(StopLossPips * _pipSize, UnitTypes.Absolute) : null;
		var trailingStop = TrailingStopPips > 0 ? new Unit(TrailingStopPips * _pipSize, UnitTypes.Absolute) : null;
		var trailingStep = TrailingStopPips > 0 && TrailingStepPips > 0 ? new Unit(TrailingStepPips * _pipSize, UnitTypes.Absolute) : null;

		// Start protection once to manage stop, take-profit, and trailing logic automatically.
		StartProtection(takeProfit, stopLoss, trailingStop, trailingStep, true);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(new IIndicator[] { _fastMa, _slowMa, _momentum, _macd }, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (_fastMa == null || _slowMa == null || _momentum == null || _macd == null)
			return;

		if (Volume != OrderVolume)
			Volume = OrderVolume;

		if (_fastMa.Length != FastMaLength)
			_fastMa.Length = FastMaLength;

		if (_slowMa.Length != SlowMaLength)
			_slowMa.Length = SlowMaLength;

		if (_momentum.Length != MomentumPeriod)
			_momentum.Length = MomentumPeriod;

		if (_macd.Fast != MacdFast)
			_macd.Fast = MacdFast;

		if (_macd.Slow != MacdSlow)
			_macd.Slow = MacdSlow;

		if (_macd.Signal != MacdSignal)
			_macd.Signal = MacdSignal;

		if (candle.State != CandleStates.Finished)
			return;

		if (values[0] is not DecimalIndicatorValue { IsFinal: true, Value: var fastValue })
			return;

		if (values[1] is not DecimalIndicatorValue { IsFinal: true, Value: var slowValue })
			return;

		if (values[2] is not DecimalIndicatorValue { IsFinal: true, Value: var momentumValue })
			return;

		if (values[3] is not MovingAverageConvergenceDivergenceSignalValue { IsFinal: true } macdValue)
			return;

		// Store the latest momentum observations to emulate the original three-bar check.
		UpdateMomentumHistory(momentumValue);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bullishTrend = fastValue > slowValue;
		var bearishTrend = fastValue < slowValue;

		var hasBullishMomentum = HasMomentumAboveThreshold(MomentumBuyThreshold);
		var hasBearishMomentum = HasMomentumAboveThreshold(MomentumSellThreshold);

		var macdLine = macdValue.Macd;
		var macdSignal = macdValue.Signal;

		var bullishMacd = macdLine > macdSignal;
		var bearishMacd = macdLine < macdSignal;

		if (EnableLongs && Position <= 0 && bullishTrend && hasBullishMomentum && bullishMacd)
		{
			if (Position < 0)
				ClosePosition();

			// Enter long when the pattern requirements are satisfied.
			BuyMarket(OrderVolume);
		}
		else if (EnableShorts && Position >= 0 && bearishTrend && hasBearishMomentum && bearishMacd)
		{
			if (Position > 0)
				ClosePosition();

			// Enter short when the pattern requirements are satisfied.
			SellMarket(OrderVolume);
		}
	}

	private void UpdateMomentumHistory(decimal newValue)
	{
		_momentumPrev2 = _momentumPrev1;
		_momentumPrev1 = _momentumCurrent;
		_momentumCurrent = newValue;
	}

	private bool HasMomentumAboveThreshold(decimal threshold)
	{
		if (threshold <= 0m)
			return true;

		var current = _momentumCurrent;
		var prev1 = _momentumPrev1;
		var prev2 = _momentumPrev2;

		if (current.HasValue && Math.Abs(current.Value - 100m) >= threshold)
			return true;

		if (prev1.HasValue && Math.Abs(prev1.Value - 100m) >= threshold)
			return true;

		if (prev2.HasValue && Math.Abs(prev2.Value - 100m) >= threshold)
			return true;

		return false;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? Security?.Step ?? 0m;
		if (step <= 0m)
			step = 1m;

		var decimals = Security?.Decimals ?? 0;
		var multiplier = decimals is 3 or 5 ? 10m : 1m;

		return step * multiplier;
	}
}