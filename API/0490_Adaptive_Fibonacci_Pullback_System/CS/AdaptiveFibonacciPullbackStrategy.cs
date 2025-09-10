namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Adaptive Fibonacci Pullback strategy.
/// Combines multiple SuperTrend lines and an adaptive moving average channel.
/// </summary>
public class AdaptiveFibonacciPullbackStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _factor1;
	private readonly StrategyParam<decimal> _factor2;
	private readonly StrategyParam<decimal> _factor3;
	private readonly StrategyParam<int> _smoothLength;
	private readonly StrategyParam<int> _amaLength;
	private readonly StrategyParam<bool> _useRsi;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiBuy;
	private readonly StrategyParam<decimal> _rsiSell;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;

	private SuperTrend _st1;
	private SuperTrend _st2;
	private SuperTrend _st3;
	private ExponentialMovingAverage _stSmooth;
	private ExponentialMovingAverage _amaMid;
	private AverageTrueRange _atr;
	private RelativeStrengthIndex _rsi;

	private decimal _prevClose;
	private decimal _prevSmooth;
	private bool _isFirst;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// SuperTrend ATR period.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// Weak Fibonacci factor.
	/// </summary>
	public decimal Factor1 { get => _factor1.Value; set => _factor1.Value = value; }

	/// <summary>
	/// Golden ratio factor.
	/// </summary>
	public decimal Factor2 { get => _factor2.Value; set => _factor2.Value = value; }

	/// <summary>
	/// Extended Fibonacci factor.
	/// </summary>
	public decimal Factor3 { get => _factor3.Value; set => _factor3.Value = value; }

	/// <summary>
	/// Smoothing EMA length for SuperTrend average.
	/// </summary>
	public int SmoothLength { get => _smoothLength.Value; set => _smoothLength.Value = value; }

	/// <summary>
	/// AMA midline length.
	/// </summary>
	public int AmaLength { get => _amaLength.Value; set => _amaLength.Value = value; }

	/// <summary>
	/// Use RSI filter.
	/// </summary>
	public bool UseRsiFilter { get => _useRsi.Value; set => _useRsi.Value = value; }

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// RSI buy threshold.
	/// </summary>
	public decimal RsiBuy { get => _rsiBuy.Value; set => _rsiBuy.Value = value; }

	/// <summary>
	/// RSI sell threshold.
	/// </summary>
	public decimal RsiSell { get => _rsiSell.Value; set => _rsiSell.Value = value; }

	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public AdaptiveFibonacciPullbackStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("SuperTrend ATR Length", "ATR period for SuperTrend", "SuperTrend");

		_factor1 = Param(nameof(Factor1), 0.618m)
			.SetDisplay("Factor 1", "Weak Fibonacci factor", "SuperTrend");

		_factor2 = Param(nameof(Factor2), 1.618m)
			.SetDisplay("Factor 2", "Golden Ratio factor", "SuperTrend");

		_factor3 = Param(nameof(Factor3), 2.618m)
			.SetDisplay("Factor 3", "Extended Fibonacci factor", "SuperTrend");

		_smoothLength = Param(nameof(SmoothLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing Length", "EMA length for SuperTrend average", "SuperTrend");

		_amaLength = Param(nameof(AmaLength), 55)
			.SetGreaterThanZero()
			.SetDisplay("AMA Length", "Length for AMA midline and ATR", "AMA");

		_useRsi = Param(nameof(UseRsiFilter), true)
			.SetDisplay("Use RSI Filter", "Enable RSI filter", "RSI");

		_rsiLength = Param(nameof(RsiLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI");

		_rsiBuy = Param(nameof(RsiBuy), 70m)
			.SetRange(0m, 100m)
			.SetDisplay("RSI Buy Threshold", "RSI must be above for long", "RSI");

		_rsiSell = Param(nameof(RsiSell), 30m)
			.SetRange(0m, 100m)
			.SetDisplay("RSI Sell Threshold", "RSI must be below for short", "RSI");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 5m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 0.75m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");
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

		_prevClose = default;
		_prevSmooth = default;
		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_st1 = new SuperTrend { Length = AtrPeriod, Multiplier = Factor1 };
		_st2 = new SuperTrend { Length = AtrPeriod, Multiplier = Factor2 };
		_st3 = new SuperTrend { Length = AtrPeriod, Multiplier = Factor3 };
		_stSmooth = new ExponentialMovingAverage { Length = SmoothLength };
		_amaMid = new ExponentialMovingAverage { Length = AmaLength };
		_atr = new AverageTrueRange { Length = AmaLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(new IIndicator[] { _st1, _st2, _st3, _amaMid, _atr, _rsi }, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection(
			new Unit(TakeProfitPercent, UnitTypes.Percent),
			new Unit(StopLossPercent, UnitTypes.Percent)
		);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (values[0].ToNullableDecimal() is not decimal st1 ||
			values[1].ToNullableDecimal() is not decimal st2 ||
			values[2].ToNullableDecimal() is not decimal st3 ||
			values[3].ToNullableDecimal() is not decimal mid ||
			values[4].ToNullableDecimal() is not decimal atrVal ||
			values[5].ToNullableDecimal() is not decimal rsiVal)
			return;

		var avg = (st1 + st2 + st3) / 3m;
		var smooth = _stSmooth.Process(avg, candle.ServerTime, true).ToDecimal();

		if (_isFirst)
		{
			_prevClose = candle.ClosePrice;
			_prevSmooth = smooth;
			_isFirst = false;
			return;
		}

		var baseLong = candle.LowPrice < avg && candle.ClosePrice > smooth && _prevClose > mid;
		var baseShort = candle.HighPrice > avg && candle.ClosePrice < smooth && _prevClose < mid;

		var longEntry = baseLong && candle.ClosePrice > mid && (!UseRsiFilter || rsiVal > RsiBuy);
		var shortEntry = baseShort && candle.ClosePrice < mid && (!UseRsiFilter || rsiVal < RsiSell);

		if (longEntry && Position <= 0)
			RegisterBuy();

		if (shortEntry && Position >= 0)
			RegisterSell();

		var longExit = _prevClose > _prevSmooth && candle.ClosePrice <= smooth && Position > 0;
		var shortExit = _prevClose < _prevSmooth && candle.ClosePrice >= smooth && Position < 0;

		if (longExit)
			RegisterSell();

		if (shortExit)
			RegisterBuy();

		_prevClose = candle.ClosePrice;
		_prevSmooth = smooth;
	}
}
