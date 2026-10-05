using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Advanced Supertrend strategy.
/// A Supertrend flip to bullish opens a long and a flip to bearish opens a short, subject to optional filters: RSI below
/// RsiOverbought for longs and above RsiOversold for shorts, the close on the trade side of a moving average, the previous trend
/// having lasted at least MinTrendBars bars, and a close beyond the previous candle's high or low. An opposite flip closes the
/// position, and optional stop loss and take profit sit SlMultiplier and TpMultiplier ATRs from the entry.
/// </summary>
public class AdvancedSupertrendStrategy : Strategy
{
	/// <summary>
	/// Moving average types of the filter.
	/// </summary>
	public enum MaTypes
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		Simple,

		/// <summary>
		/// Exponential moving average.
		/// </summary>
		Exponential,

		/// <summary>
		/// Weighted moving average.
		/// </summary>
		Weighted,
	}

	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<bool> _useRsiFilter;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<bool> _useMaFilter;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<MaTypes> _maType;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _slMultiplier;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _tpMultiplier;
	private readonly StrategyParam<bool> _useTrendStrength;
	private readonly StrategyParam<int> _minTrendBars;
	private readonly StrategyParam<bool> _useBreakoutConfirmation;
	private readonly StrategyParam<DataType> _candleType;

	private bool? _prevUpTrend;
	private int _trendBars;
	private decimal? _prevHigh;
	private decimal? _prevLow;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// ATR length of the Supertrend and of the stops.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Supertrend ATR multiplier.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Enable the RSI filter.
	/// </summary>
	public bool UseRsiFilter
	{
		get => _useRsiFilter.Value;
		set => _useRsiFilter.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI level longs must stay below.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// RSI level shorts must stay above.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// Enable the moving average filter.
	/// </summary>
	public bool UseMaFilter
	{
		get => _useMaFilter.Value;
		set => _useMaFilter.Value = value;
	}

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Moving average type.
	/// </summary>
	public MaTypes MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Enable the ATR stop loss.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the stop loss.
	/// </summary>
	public decimal SlMultiplier
	{
		get => _slMultiplier.Value;
		set => _slMultiplier.Value = value;
	}

	/// <summary>
	/// Enable the ATR take profit.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the take profit.
	/// </summary>
	public decimal TpMultiplier
	{
		get => _tpMultiplier.Value;
		set => _tpMultiplier.Value = value;
	}

	/// <summary>
	/// Enable the trend strength filter.
	/// </summary>
	public bool UseTrendStrength
	{
		get => _useTrendStrength.Value;
		set => _useTrendStrength.Value = value;
	}

	/// <summary>
	/// Bars the previous trend must have lasted.
	/// </summary>
	public int MinTrendBars
	{
		get => _minTrendBars.Value;
		set => _minTrendBars.Value = value;
	}

	/// <summary>
	/// Require a close beyond the previous candle's extreme.
	/// </summary>
	public bool UseBreakoutConfirmation
	{
		get => _useBreakoutConfirmation.Value;
		set => _useBreakoutConfirmation.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public AdvancedSupertrendStrategy()
	{
		_atrLength = Param(nameof(AtrLength), 6)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR length of the Supertrend and of the stops", "Supertrend");

		_multiplier = Param(nameof(Multiplier), 3.0m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Supertrend ATR multiplier", "Supertrend");

		_useRsiFilter = Param(nameof(UseRsiFilter), false)
			.SetDisplay("Use RSI Filter", "Enable the RSI filter", "Filters");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Filters");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "RSI level longs must stay below", "Filters");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "RSI level shorts must stay above", "Filters");

		_useMaFilter = Param(nameof(UseMaFilter), true)
			.SetDisplay("Use MA Filter", "Enable the moving average filter", "Filters");

		_maLength = Param(nameof(MaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average period", "Filters");

		_maType = Param(nameof(MaType), MaTypes.Weighted)
			.SetDisplay("MA Type", "Moving average type", "Filters");

		_useStopLoss = Param(nameof(UseStopLoss), true)
			.SetDisplay("Use Stop Loss", "Enable the ATR stop loss", "Risk");

		_slMultiplier = Param(nameof(SlMultiplier), 3.0m)
			.SetNotNegative()
			.SetDisplay("SL Multiplier", "ATR multiplier of the stop loss", "Risk");

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
			.SetDisplay("Use Take Profit", "Enable the ATR take profit", "Risk");

		_tpMultiplier = Param(nameof(TpMultiplier), 9.0m)
			.SetNotNegative()
			.SetDisplay("TP Multiplier", "ATR multiplier of the take profit", "Risk");

		_useTrendStrength = Param(nameof(UseTrendStrength), false)
			.SetDisplay("Use Trend Strength", "Enable the trend strength filter", "Filters");

		_minTrendBars = Param(nameof(MinTrendBars), 2)
			.SetGreaterThanZero()
			.SetDisplay("Min Trend Bars", "Bars the previous trend must have lasted", "Filters");

		_useBreakoutConfirmation = Param(nameof(UseBreakoutConfirmation), true)
			.SetDisplay("Use Breakout Confirmation", "Require a close beyond the previous candle's extreme", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var superTrend = new SuperTrend { Length = AtrLength, Multiplier = Multiplier };
		var atr = new AverageTrueRange { Length = AtrLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		IIndicator ma = MaType switch
		{
			MaTypes.Simple => new SimpleMovingAverage { Length = MaLength },
			MaTypes.Exponential => new ExponentialMovingAverage { Length = MaLength },
			_ => new WeightedMovingAverage { Length = MaLength },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(superTrend, atr, rsi, ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, superTrend);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ResetState()
	{
		_prevUpTrend = null;
		_trendBars = 0;
		_prevHigh = null;
		_prevLow = null;
		_stopPrice = null;
		_takePrice = null;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue superTrendValue, IIndicatorValue atrValue, IIndicatorValue rsiValue, IIndicatorValue maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevHigh = _prevHigh;
		var prevLow = _prevLow;
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;

		if (!superTrendValue.IsFormed || !atrValue.IsFormed || !rsiValue.IsFormed || !maValue.IsFormed)
			return;

		var upTrend = ((SuperTrendIndicatorValue)superTrendValue).IsUpTrend;
		var prevUpTrend = _prevUpTrend;
		var priorTrendBars = _trendBars;

		if (prevUpTrend == upTrend)
			_trendBars++;
		else
			_trendBars = 1;

		_prevUpTrend = upTrend;

		if (prevUpTrend is not bool wasUp || prevHigh is not decimal pHigh || prevLow is not decimal pLow)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (Position > 0 && ((_stopPrice is decimal ls && candle.LowPrice <= ls) || (_takePrice is decimal lt && candle.HighPrice >= lt)))
		{
			SellMarket(Position);
			ClearLevels();
			return;
		}

		if (Position < 0 && ((_stopPrice is decimal ss && candle.HighPrice >= ss) || (_takePrice is decimal st && candle.LowPrice <= st)))
		{
			BuyMarket(-Position);
			ClearLevels();
			return;
		}

		var bullishFlip = upTrend && !wasUp;
		var bearishFlip = !upTrend && wasUp;

		if (!bullishFlip && !bearishFlip)
			return;

		var rsi = rsiValue.ToDecimal();
		var ma = maValue.ToDecimal();
		var strongTrend = !UseTrendStrength || priorTrendBars >= MinTrendBars;

		var longOk = bullishFlip && strongTrend
			&& (!UseRsiFilter || rsi < RsiOverbought)
			&& (!UseMaFilter || close > ma)
			&& (!UseBreakoutConfirmation || close > pHigh);

		var shortOk = bearishFlip && strongTrend
			&& (!UseRsiFilter || rsi > RsiOversold)
			&& (!UseMaFilter || close < ma)
			&& (!UseBreakoutConfirmation || close < pLow);

		var atr = atrValue.ToDecimal();

		if (longOk && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			SetLevels(close, atr, true);
		}
		else if (shortOk && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			SetLevels(close, atr, false);
		}
		else if (bearishFlip && Position > 0)
		{
			SellMarket(Position);
			ClearLevels();
		}
		else if (bullishFlip && Position < 0)
		{
			BuyMarket(-Position);
			ClearLevels();
		}
	}

	private void SetLevels(decimal price, decimal atr, bool isLong)
	{
		var stop = atr * SlMultiplier;
		var take = atr * TpMultiplier;
		_stopPrice = UseStopLoss && stop > 0 ? (isLong ? price - stop : price + stop) : null;
		_takePrice = UseTakeProfit && take > 0 ? (isLong ? price + take : price - take) : null;
	}

	private void ClearLevels()
	{
		_stopPrice = null;
		_takePrice = null;
	}
}
