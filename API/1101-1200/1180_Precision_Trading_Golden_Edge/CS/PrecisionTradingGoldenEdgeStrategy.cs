namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Confirms EMA crosses with Hull slope, RSI and ATR/price-range volatility,
/// then opens or fully reverses on finished candles.
/// </summary>
public class PrecisionTradingGoldenEdgeStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _hmaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiLongThreshold;
	private readonly StrategyParam<decimal> _rsiShortThreshold;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<int> _rangeLength;
	private readonly StrategyParam<decimal> _minAtrPercent;
	private readonly StrategyParam<decimal> _rangeAtrMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	private bool _isInitialized;
	private decimal _previousFast;
	private decimal _previousSlow;
	private decimal _previousHma;
	private Order _pendingOrder;

	/// <summary>Initializes EMA14/40, HMA55, RSI14 at 55/45, ATR14 and twenty-bar range confirmation.</summary>
	public PrecisionTradingGoldenEdgeStrategy()
	{
		_fastLength = Param(nameof(FastLength), 14).SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast close EMA period", "Trend");
		_slowLength = Param(nameof(SlowLength), 40).SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow close EMA period", "Trend");
		_hmaLength = Param(nameof(HmaLength), 55).SetRange(2, int.MaxValue)
			.SetDisplay("HMA Length", "Hull close average with integer half and square-root lengths", "Trend");
		_rsiLength = Param(nameof(RsiLength), 14).SetGreaterThanZero()
			.SetDisplay("RSI Length", "Close momentum period", "Momentum");
		_rsiLongThreshold = Param(nameof(RsiLongThreshold), 55m).SetRange(0m, 100m)
			.SetDisplay("Long RSI Threshold", "Strict lower bound for long confirmation", "Momentum");
		_rsiShortThreshold = Param(nameof(RsiShortThreshold), 45m).SetRange(0m, 100m)
			.SetDisplay("Short RSI Threshold", "Strict upper bound for short confirmation", "Momentum");
		_atrLength = Param(nameof(AtrLength), 14).SetGreaterThanZero()
			.SetDisplay("ATR Length", "Wilder true-range period", "Volatility");
		_rangeLength = Param(nameof(RangeLength), 20).SetGreaterThanZero()
			.SetDisplay("Range Length", "Current-inclusive high/low window", "Volatility");
		_minAtrPercent = Param(nameof(MinAtrPercent), 0.05m).SetRange(0m, decimal.MaxValue)
			.SetDisplay("Minimum ATR Percent", "Inclusive minimum ATR as percent of current close", "Volatility");
		_rangeAtrMultiplier = Param(nameof(RangeAtrMultiplier), 3m).SetGreaterThanZero()
			.SetDisplay("Range ATR Multiplier", "Inclusive minimum highest-lowest span as an ATR multiple", "Volatility");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Finished candles supplying all confirmations", "General");
		OrderRegistering += order => _pendingOrder = order;
	}

	/// <summary>Fast close EMA length, strictly less than SlowLength.</summary>
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	/// <summary>Slow close EMA length.</summary>
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	/// <summary>Hull close average length, at least two.</summary>
	public int HmaLength { get => _hmaLength.Value; set => _hmaLength.Value = value; }
	/// <summary>Native close RSI length.</summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	/// <summary>Strict long RSI threshold.</summary>
	public decimal RsiLongThreshold { get => _rsiLongThreshold.Value; set => _rsiLongThreshold.Value = value; }
	/// <summary>Strict short RSI threshold, below RsiLongThreshold.</summary>
	public decimal RsiShortThreshold { get => _rsiShortThreshold.Value; set => _rsiShortThreshold.Value = value; }
	/// <summary>Wilder true-range length.</summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	/// <summary>Current-inclusive highest-high/lowest-low window length.</summary>
	public int RangeLength { get => _rangeLength.Value; set => _rangeLength.Value = value; }
	/// <summary>Inclusive minimum ATR/close percentage.</summary>
	public decimal MinAtrPercent { get => _minAtrPercent.Value; set => _minAtrPercent.Value = value; }
	/// <summary>Inclusive minimum high-low window span as an ATR multiple.</summary>
	public decimal RangeAtrMultiplier { get => _rangeAtrMultiplier.Value; set => _rangeAtrMultiplier.Value = value; }
	/// <summary>Finished candle type; five minutes by default.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	private void ResetState()
	{
		_isInitialized = false;
		_previousFast = _previousSlow = _previousHma = 0m;
		_pendingOrder = null;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		if (FastLength <= 0 || SlowLength <= FastLength || HmaLength < 2 || RsiLength <= 0 || AtrLength <= 0
			|| RangeLength <= 0 || MinAtrPercent < 0m || RangeAtrMultiplier <= 0m
			|| RsiShortThreshold < 0m || RsiLongThreshold > 100m || RsiShortThreshold >= RsiLongThreshold)
			throw new ArgumentException("EMA lengths must be ordered, HMA length at least two, other periods/multiplier positive, and RSI thresholds ordered within [0, 100].");
		ResetState();
		var fast = new ExponentialMovingAverage { Length = FastLength };
		var slow = new ExponentialMovingAverage { Length = SlowLength };
		var hma = new HullMovingAverage { Length = HmaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		var highest = new Highest { Length = RangeLength };
		var lowest = new Lowest { Length = RangeLength };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx([fast, slow, hma, rsi, atr, highest, lowest], ProcessCandle, allowEmpty: true).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawIndicator(area, hma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished || values.Any(value => value.IsEmpty || !value.IsFormed))
			return;
		var fast = values[0].ToDecimal();
		var slow = values[1].ToDecimal();
		var hma = values[2].ToDecimal();
		var rsi = values[3].ToDecimal();
		var atr = values[4].ToDecimal();
		var span = values[5].ToDecimal() - values[6].ToDecimal();
		var volatilityPass = candle.ClosePrice > 0m && atr > 0m && 100m * atr / candle.ClosePrice >= MinAtrPercent
			&& span >= atr * RangeAtrMultiplier;
		if (_isInitialized && volatilityPass && IsFormedAndOnlineAndAllowTrading()
			&& (_pendingOrder is null || _pendingOrder.State is OrderStates.Done or OrderStates.Failed))
		{
			Sides? side = null;
			if (_previousFast <= _previousSlow && fast > slow && hma > _previousHma && rsi > RsiLongThreshold && Position <= 0m)
				side = Sides.Buy;
			else if (_previousFast >= _previousSlow && fast < slow && hma < _previousHma && rsi < RsiShortThreshold && Position >= 0m)
				side = Sides.Sell;
			if (side is Sides direction)
				RegisterOrder(new Order
				{
					Security = Security,
					Portfolio = Portfolio,
					Type = OrderTypes.Market,
					Side = direction,
					Volume = Volume + Math.Abs(Position),
					Comment = "Golden Edge confirmed cross",
				});
		}
		_isInitialized = true;
		_previousFast = fast;
		_previousSlow = slow;
		_previousHma = hma;
	}
}
