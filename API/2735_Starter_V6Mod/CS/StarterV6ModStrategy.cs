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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the Starter_v6mod Expert Advisor using the high-level StockSharp API.
/// </summary>
public class StarterV6ModStrategy : Strategy
{
	private readonly StrategyParam<bool> _useManualVolume;
	private readonly StrategyParam<decimal> _manualVolume;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<decimal> _decreaseFactor;
	private readonly StrategyParam<int> _maxLossesPerDay;
	private readonly StrategyParam<decimal> _equityCutoff;
	private readonly StrategyParam<int> _maxOpenTrades;
	private readonly StrategyParam<int> _gridStepPips;
	private readonly StrategyParam<int> _longEmaPeriod;
	private readonly StrategyParam<int> _shortEmaPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _angleThreshold;
	private readonly StrategyParam<decimal> _levelUp;
	private readonly StrategyParam<decimal> _levelDown;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _longEma;
	private ExponentialMovingAverage _shortEma;
	private CommodityChannelIndex _cci;
	private RelativeStrengthIndex _laguerreProxy;

	private decimal? _prevLongEma;
	private decimal? _prevShortEma;

	/// <summary>
	/// Use manual volume instead of risk calculation.
	/// </summary>
	public bool UseManualVolume
	{
		get => _useManualVolume.Value;
		set => _useManualVolume.Value = value;
	}

	/// <summary>
	/// Manual volume for each new entry.
	/// </summary>
	public decimal ManualVolume
	{
		get => _manualVolume.Value;
		set => _manualVolume.Value = value;
	}

	/// <summary>
	/// Risk percentage used when position sizing is automatic.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Additional distance required before the trailing stop starts to follow the price.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Multiplier used to reduce the position size after losses.
	/// </summary>
	public decimal DecreaseFactor
	{
		get => _decreaseFactor.Value;
		set => _decreaseFactor.Value = value;
	}

	/// <summary>
	/// Maximum number of losing trades allowed per day.
	/// </summary>
	public int MaxLossesPerDay
	{
		get => _maxLossesPerDay.Value;
		set => _maxLossesPerDay.Value = value;
	}

	/// <summary>
	/// Equity threshold below which the strategy stops opening new trades.
	/// </summary>
	public decimal EquityCutoff
	{
		get => _equityCutoff.Value;
		set => _equityCutoff.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneously opened grid positions.
	/// </summary>
	public int MaxOpenTrades
	{
		get => _maxOpenTrades.Value;
		set => _maxOpenTrades.Value = value;
	}

	/// <summary>
	/// Grid step in pips used when stacking positions.
	/// </summary>
	public int GridStepPips
	{
		get => _gridStepPips.Value;
		set => _gridStepPips.Value = value;
	}

	/// <summary>
	/// Period for the slow EMA trend filter.
	/// </summary>
	public int LongEmaPeriod
	{
		get => _longEmaPeriod.Value;
		set => _longEmaPeriod.Value = value;
	}

	/// <summary>
	/// Period for the fast EMA trend filter.
	/// </summary>
	public int ShortEmaPeriod
	{
		get => _shortEmaPeriod.Value;
		set => _shortEmaPeriod.Value = value;
	}

	/// <summary>
	/// Period for the CCI momentum filter.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Threshold in ticks for the EMA spread trend detector.
	/// </summary>
	public decimal AngleThreshold
	{
		get => _angleThreshold.Value;
		set => _angleThreshold.Value = value;
	}

	/// <summary>
	/// Upper Laguerre RSI level.
	/// </summary>
	public decimal LevelUp
	{
		get => _levelUp.Value;
		set => _levelUp.Value = value;
	}

	/// <summary>
	/// Lower Laguerre RSI level.
	/// </summary>
	public decimal LevelDown
	{
		get => _levelDown.Value;
		set => _levelDown.Value = value;
	}

	/// <summary>
	/// Candle data type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="StarterV6ModStrategy"/> class.
	/// </summary>
	public StarterV6ModStrategy()
	{
		_useManualVolume = Param(nameof(UseManualVolume), true)
		.SetDisplay("Manual Volume", "Use manual volume instead of risk-based sizing", "Money Management");

		_manualVolume = Param(nameof(ManualVolume), 1m)
		.SetRange(0.01m, 100m)
		.SetDisplay("Volume", "Manual volume per trade", "Money Management")
		;

		_riskPercent = Param(nameof(RiskPercent), 5m)
		.SetRange(0.5m, 20m)
		.SetDisplay("Risk %", "Risk percentage when auto-sizing trades", "Money Management")
		;

		_stopLossPips = Param(nameof(StopLossPips), 35)
		.SetRange(0, 500)
		.SetDisplay("Stop Loss", "Stop-loss distance in pips", "Risk Management");

		_takeProfitPips = Param(nameof(TakeProfitPips), 10)
		.SetRange(0, 500)
		.SetDisplay("Take Profit", "Take-profit distance in pips", "Risk Management");

		_trailingStopPips = Param(nameof(TrailingStopPips), 0)
		.SetRange(0, 500)
		.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk Management");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
		.SetRange(0, 500)
		.SetDisplay("Trailing Step", "Additional distance before trailing activates", "Risk Management");

		_decreaseFactor = Param(nameof(DecreaseFactor), 1.6m)
		.SetRange(1m, 10m)
		.SetDisplay("Decrease Factor", "Volume reduction factor after losses", "Money Management");

		_maxLossesPerDay = Param(nameof(MaxLossesPerDay), 3)
		.SetRange(0, 20)
		.SetDisplay("Daily Loss Limit", "Maximum number of losses per day", "Risk Management");

		_equityCutoff = Param(nameof(EquityCutoff), 800m)
		.SetRange(0m, 1_000_000m)
		.SetDisplay("Equity Cutoff", "Stop trading if equity drops below this value", "Risk Management");

		_maxOpenTrades = Param(nameof(MaxOpenTrades), 10)
		.SetRange(1, 100)
		.SetDisplay("Max Trades", "Maximum simultaneous grid positions", "General");

		_gridStepPips = Param(nameof(GridStepPips), 30)
		.SetRange(0, 500)
		.SetDisplay("Grid Step", "Minimum pip distance between stacked entries", "General");

		_longEmaPeriod = Param(nameof(LongEmaPeriod), 120)
		.SetRange(10, 400)
		.SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
		;

		_shortEmaPeriod = Param(nameof(ShortEmaPeriod), 40)
		.SetRange(5, 200)
		.SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
		;

		_cciPeriod = Param(nameof(CciPeriod), 14)
		.SetRange(5, 100)
		.SetDisplay("CCI Period", "CCI indicator length", "Indicators")
		;

		_angleThreshold = Param(nameof(AngleThreshold), 3m)
		.SetRange(0m, 50m)
		.SetDisplay("Angle Threshold", "EMA spread threshold measured in ticks", "Indicators");

		_levelUp = Param(nameof(LevelUp), 0.85m)
		.SetRange(0.1m, 1m)
		.SetDisplay("Laguerre Up", "Upper Laguerre RSI level", "Indicators");

		_levelDown = Param(nameof(LevelDown), 0.15m)
		.SetRange(0m, 0.9m)
		.SetDisplay("Laguerre Down", "Lower Laguerre RSI level", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for analysis", "General");
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

		_longEma = null;
		_shortEma = null;
		_cci = null;
		_laguerreProxy = null;
		_prevLongEma = null;
		_prevShortEma = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_longEma = new ExponentialMovingAverage { Length = LongEmaPeriod };
		_shortEma = new ExponentialMovingAverage { Length = ShortEmaPeriod };
		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_laguerreProxy = new RelativeStrengthIndex { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_longEma, _shortEma, _cci, _laguerreProxy, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _longEma);
			DrawIndicator(area, _shortEma);
			DrawIndicator(area, _cci);
			DrawIndicator(area, _laguerreProxy);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal longEmaValue, decimal shortEmaValue, decimal cciValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevLongEma is null || _prevShortEma is null)
		{
			_prevLongEma = longEmaValue;
			_prevShortEma = shortEmaValue;
			return;
		}

		if (Position != 0)
		{
			_prevLongEma = longEmaValue;
			_prevShortEma = shortEmaValue;
			return;
		}

		var laguerre = rsiValue / 100m;

		// Buy: RSI low (oversold), EMAs falling (pullback), CCI negative
		var buySignal = laguerre < LevelDown && cciValue < 0m;

		// Sell: RSI high (overbought), EMAs rising, CCI positive
		var sellSignal = laguerre > LevelUp && cciValue > 0m;

		if (buySignal)
			BuyMarket();
		else if (sellSignal)
			SellMarket();

		_prevLongEma = longEmaValue;
		_prevShortEma = shortEmaValue;
	}
}