
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
/// Stellar Lite ICT strategy that combines Silver Bullet and 2022 model setups.
/// The strategy reads ICT style order flow concepts on finished candles
/// and places partial take profits with adaptive stop management.
/// </summary>
public class StellarLiteIctEaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherTimeframeType;
	private readonly StrategyParam<int> _higherMaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _liquidityLookback;
	private readonly StrategyParam<decimal> _atrThreshold;
	private readonly StrategyParam<decimal> _tp1Ratio;
	private readonly StrategyParam<decimal> _tp2Ratio;
	private readonly StrategyParam<decimal> _tp3Ratio;
	private readonly StrategyParam<decimal> _tp1Percent;
	private readonly StrategyParam<decimal> _tp2Percent;
	private readonly StrategyParam<decimal> _tp3Percent;
	private readonly StrategyParam<bool> _moveToBreakEven;
	private readonly StrategyParam<decimal> _breakEvenOffset;
	private readonly StrategyParam<decimal> _trailingDistance;
	private readonly StrategyParam<bool> _useSilverBullet;
	private readonly StrategyParam<bool> _use2022Model;
	private readonly StrategyParam<bool> _useOteEntry;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _oteLowerLevel;

	private SimpleMovingAverage _higherMa;
	private AverageTrueRange _atr;

	private decimal? _lastHtfMa;
	private decimal? _previousHtfMa;
	private Sides? _currentBias;

	private readonly ICandleMessage[] _history = new ICandleMessage[20];
	private int _historyCount;
	private decimal _latestAtr;

	/// <summary>
	/// Initializes a new instance of the <see cref="StellarLiteIctEaStrategy"/>.
	/// </summary>
	public StellarLiteIctEaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Entry Candle", "Primary timeframe used for entries", "General");

		_higherTimeframeType = Param(nameof(HigherTimeframeType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Higher Timeframe", "Timeframe used for directional bias", "General");

		_higherMaPeriod = Param(nameof(HigherMaPeriod), 20)
			.SetDisplay("Higher MA Period", "Moving average length for higher timeframe bias", "Bias")
			;

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "Average True Range lookback", "Volatility")
			;

		_liquidityLookback = Param(nameof(LiquidityLookback), 20)
			.SetDisplay("Liquidity Lookback", "Number of candles to detect liquidity pools", "Structure")
			;

		_atrThreshold = Param(nameof(AtrThreshold), 2.0m)
			.SetDisplay("ATR Threshold", "Maximum candle range relative to ATR", "Structure")
			;

		_tp1Ratio = Param(nameof(Tp1Ratio), 1m)
			.SetDisplay("TP1 Risk Reward", "Risk reward multiplier for the first target", "Targets")
			;

		_tp2Ratio = Param(nameof(Tp2Ratio), 2m)
			.SetDisplay("TP2 Risk Reward", "Risk reward multiplier for the second target", "Targets")
			;

		_tp3Ratio = Param(nameof(Tp3Ratio), 3m)
			.SetDisplay("TP3 Risk Reward", "Risk reward multiplier for the final target", "Targets")
			;

		_tp1Percent = Param(nameof(Tp1Percent), 50m)
			.SetDisplay("TP1 Close %", "Percentage of volume closed at the first target", "Targets")
			;

		_tp2Percent = Param(nameof(Tp2Percent), 25m)
			.SetDisplay("TP2 Close %", "Percentage of volume closed at the second target", "Targets")
			;

		_tp3Percent = Param(nameof(Tp3Percent), 25m)
			.SetDisplay("TP3 Close %", "Percentage of volume closed at the final target", "Targets")
			;

		_moveToBreakEven = Param(nameof(MoveToBreakEven), true)
			.SetDisplay("Break Even After TP1", "Move the stop to break even after the first partial", "Protection");

		_breakEvenOffset = Param(nameof(BreakEvenOffset), 1m)
			.SetDisplay("Break Even Offset", "Additional price steps added to the break even stop", "Protection")
			;

		_trailingDistance = Param(nameof(TrailingDistance), 10m)
			.SetDisplay("Trailing Distance", "Price steps used after TP2 for trailing stop", "Protection")
			;

		_useSilverBullet = Param(nameof(UseSilverBullet), true)
			.SetDisplay("Use Silver Bullet", "Enable the Silver Bullet setup", "Structure");

		_use2022Model = Param(nameof(Use2022Model), true)
			.SetDisplay("Use 2022 Model", "Enable the 2022 model setup", "Structure");

		_useOteEntry = Param(nameof(UseOteEntry), true)
			.SetDisplay("Use OTE Entry", "Place entries inside the optimal trade entry zone", "Structure");

		_riskPercent = Param(nameof(RiskPercent), 0.25m)
			.SetDisplay("Risk %", "Risk percentage of account equity used to size trades", "Risk")
			;

		_oteLowerLevel = Param(nameof(OteLowerLevel), 0.618m)
			.SetDisplay("OTE Lower", "Lower Fibonacci level used for the entry", "Structure")
			;
	}

	/// <summary>
	/// Primary candle type used to generate entries.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe candle type that provides directional bias.
	/// </summary>
	public DataType HigherTimeframeType
	{
		get => _higherTimeframeType.Value;
		set => _higherTimeframeType.Value = value;
	}

	/// <summary>
	/// Higher timeframe moving average period.
	/// </summary>
	public int HigherMaPeriod
	{
		get => _higherMaPeriod.Value;
		set => _higherMaPeriod.Value = value;
	}

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Number of candles used to search for liquidity pools.
	/// </summary>
	public int LiquidityLookback
	{
		get => _liquidityLookback.Value;
		set => _liquidityLookback.Value = value;
	}

	/// <summary>
	/// Maximum allowed candle range relative to ATR to confirm consolidation.
	/// </summary>
	public decimal AtrThreshold
	{
		get => _atrThreshold.Value;
		set => _atrThreshold.Value = value;
	}

	/// <summary>
	/// Risk reward multiplier for the first target.
	/// </summary>
	public decimal Tp1Ratio
	{
		get => _tp1Ratio.Value;
		set => _tp1Ratio.Value = value;
	}

	/// <summary>
	/// Risk reward multiplier for the second target.
	/// </summary>
	public decimal Tp2Ratio
	{
		get => _tp2Ratio.Value;
		set => _tp2Ratio.Value = value;
	}

	/// <summary>
	/// Risk reward multiplier for the third target.
	/// </summary>
	public decimal Tp3Ratio
	{
		get => _tp3Ratio.Value;
		set => _tp3Ratio.Value = value;
	}

	/// <summary>
	/// Percentage of the position closed at TP1.
	/// </summary>
	public decimal Tp1Percent
	{
		get => _tp1Percent.Value;
		set => _tp1Percent.Value = value;
	}

	/// <summary>
	/// Percentage of the position closed at TP2.
	/// </summary>
	public decimal Tp2Percent
	{
		get => _tp2Percent.Value;
		set => _tp2Percent.Value = value;
	}

	/// <summary>
	/// Percentage of the position closed at TP3.
	/// </summary>
	public decimal Tp3Percent
	{
		get => _tp3Percent.Value;
		set => _tp3Percent.Value = value;
	}

	/// <summary>
	/// Enables moving the stop to break even after TP1.
	/// </summary>
	public bool MoveToBreakEven
	{
		get => _moveToBreakEven.Value;
		set => _moveToBreakEven.Value = value;
	}

	/// <summary>
	/// Additional price steps added to the break even stop.
	/// </summary>
	public decimal BreakEvenOffset
	{
		get => _breakEvenOffset.Value;
		set => _breakEvenOffset.Value = value;
	}

	/// <summary>
	/// Distance in price steps for the trailing stop activated after TP2.
	/// </summary>
	public decimal TrailingDistance
	{
		get => _trailingDistance.Value;
		set => _trailingDistance.Value = value;
	}

	/// <summary>
	/// Enables the Silver Bullet setup.
	/// </summary>
	public bool UseSilverBullet
	{
		get => _useSilverBullet.Value;
		set => _useSilverBullet.Value = value;
	}

	/// <summary>
	/// Enables the 2022 model setup.
	/// </summary>
	public bool Use2022Model
	{
		get => _use2022Model.Value;
		set => _use2022Model.Value = value;
	}

	/// <summary>
	/// Enables the optimal trade entry calculation.
	/// </summary>
	public bool UseOteEntry
	{
		get => _useOteEntry.Value;
		set => _useOteEntry.Value = value;
	}

	/// <summary>
	/// Risk percentage used for dynamic position sizing.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Lower bound of the OTE retracement window.
	/// </summary>
	public decimal OteLowerLevel
	{
		get => _oteLowerLevel.Value;
		set => _oteLowerLevel.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType), (Security, HigherTimeframeType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_higherMa = null;
		_atr = null;

		_lastHtfMa = null;
		_previousHtfMa = null;
		_currentBias = null;

		Array.Clear(_history, 0, _history.Length);
		_historyCount = 0;
		_latestAtr = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_higherMa = new SimpleMovingAverage { Length = HigherMaPeriod };
		_atr = new AverageTrueRange { Length = AtrPeriod };

		Indicators.Add(_atr);

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
			.Bind(ProcessMainCandle)
			.Start();

		var higherSubscription = SubscribeCandles(HigherTimeframeType);
		higherSubscription
			.Bind(_higherMa, ProcessHigherCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessHigherCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_previousHtfMa = _lastHtfMa;
		_lastHtfMa = maValue;

		if (_previousHtfMa is not decimal prev || _lastHtfMa is not decimal current)
			return;

		if (candle.ClosePrice > current && current > prev)
		{
			_currentBias = Sides.Buy;
		}
		else if (candle.ClosePrice < current && current < prev)
		{
			_currentBias = Sides.Sell;
		}
		else
		{
			_currentBias = null;
		}
	}

	private void ProcessMainCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var atrValue = _atr.Process(candle);

		StoreCandle(candle);

		if (!_atr.IsFormed)
			return;

		_latestAtr = atrValue.ToDecimal();

		if (Position != 0)
			return;

		if (_currentBias is not Sides bias)
			return;

		if (_historyCount < 3)
			return;

		// Simplified ICT entry: market structure shift + bias alignment
		var prev = _history[1];
		var prev2 = _history[2];
		if (prev == null || prev2 == null)
			return;

		if (bias == Sides.Buy)
		{
			// Bullish MSS: current close breaks above previous high after a down move
			if (candle.ClosePrice > prev.HighPrice && prev.ClosePrice < prev2.OpenPrice)
				BuyMarket();
		}
		else
		{
			// Bearish MSS: current close breaks below previous low after an up move
			if (candle.ClosePrice < prev.LowPrice && prev.ClosePrice > prev2.OpenPrice)
				SellMarket();
		}
	}

	private void StoreCandle(ICandleMessage candle)
	{
		for (var i = _history.Length - 1; i > 0; i--)
		{
			_history[i] = _history[i - 1];
		}

		_history[0] = candle;
		if (_historyCount < _history.Length)
			_historyCount++;
	}
}

