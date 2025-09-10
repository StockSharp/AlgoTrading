using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Advanced Supertrend strategy with optional RSI and MA filters.
/// </summary>
public class AdvancedSupertrendStrategy : Strategy
{
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<bool> _useRsiFilter;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<bool> _useMaFilter;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<MaTypeEnum> _maType;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _slMultiplier;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _tpMultiplier;
	private readonly StrategyParam<bool> _useTrendStrength;
	private readonly StrategyParam<int> _minTrendBars;
	private readonly StrategyParam<bool> _useBreakoutConfirmation;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr;
	private RelativeStrengthIndex _rsi;
	private IIndicator _ma;

	private decimal _prevTrendUp;
	private decimal _prevTrendDown;
	private int _prevTrend = 1;
	private int _trendStrength;
	private decimal _prevSupertrend;
	private decimal _stopLossLevel;
	private decimal _takeProfitLevel;
	private decimal _prevClose;

	/// <summary>
	/// ATR length for Supertrend and risk management.
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
	/// Use RSI filter.
	/// </summary>
	public bool UseRsiFilter
	{
		get => _useRsiFilter.Value;
		set => _useRsiFilter.Value = value;
	}

	/// <summary>
	/// RSI length.
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
	/// Use MA filter.
	/// </summary>
	public bool UseMaFilter
	{
		get => _useMaFilter.Value;
		set => _useMaFilter.Value = value;
	}

	/// <summary>
	/// MA length.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Moving average type.
	/// </summary>
	public MaTypeEnum MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Enable stop loss.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Stop loss ATR multiplier.
	/// </summary>
	public decimal SlMultiplier
	{
		get => _slMultiplier.Value;
		set => _slMultiplier.Value = value;
	}

	/// <summary>
	/// Enable take profit.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// Take profit ATR multiplier.
	/// </summary>
	public decimal TpMultiplier
	{
		get => _tpMultiplier.Value;
		set => _tpMultiplier.Value = value;
	}

	/// <summary>
	/// Enable trend strength filter.
	/// </summary>
	public bool UseTrendStrength
	{
		get => _useTrendStrength.Value;
		set => _useTrendStrength.Value = value;
	}

	/// <summary>
	/// Minimum bars in current trend.
	/// </summary>
	public int MinTrendBars
	{
		get => _minTrendBars.Value;
		set => _minTrendBars.Value = value;
	}

	/// <summary>
	/// Enable breakout confirmation.
	/// </summary>
	public bool UseBreakoutConfirmation
	{
		get => _useBreakoutConfirmation.Value;
		set => _useBreakoutConfirmation.Value = value;
	}

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="AdvancedSupertrendStrategy"/>.
	/// </summary>
	public AdvancedSupertrendStrategy()
	{
		_atrLength = Param(nameof(AtrLength), 6)
			.SetDisplay("ATR Length", "ATR period", "Supertrend Settings")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_multiplier = Param(nameof(Multiplier), 3m)
			.SetDisplay("Multiplier", "Supertrend multiplier", "Supertrend Settings")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_useRsiFilter = Param(nameof(UseRsiFilter), false)
			.SetDisplay("Use RSI Filter", "Enable RSI filter", "RSI Filter");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "RSI period", "RSI Filter")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 1);

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "Overbought level", "RSI Filter");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "Oversold level", "RSI Filter");

		_useMaFilter = Param(nameof(UseMaFilter), true)
			.SetDisplay("Use MA Filter", "Enable MA filter", "MA Filter");

		_maLength = Param(nameof(MaLength), 50)
			.SetDisplay("MA Length", "MA period", "MA Filter")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 10);

		_maType = Param(nameof(MaType), MaTypeEnum.Weighted)
			.SetDisplay("MA Type", "Type of moving average", "MA Filter");

		_useStopLoss = Param(nameof(UseStopLoss), true)
			.SetDisplay("Use Stop Loss", "Enable stop loss", "Risk Management");

		_slMultiplier = Param(nameof(SlMultiplier), 3m)
			.SetDisplay("SL Multiplier", "Stop loss ATR multiplier", "Risk Management");

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
			.SetDisplay("Use Take Profit", "Enable take profit", "Risk Management");

		_tpMultiplier = Param(nameof(TpMultiplier), 9m)
			.SetDisplay("TP Multiplier", "Take profit ATR multiplier", "Risk Management");

		_useTrendStrength = Param(nameof(UseTrendStrength), false)
			.SetDisplay("Use Trend Strength", "Enable trend strength filter", "Advanced")
			.SetCanOptimize(true)
			.SetOptimize(false, true, true);

		_minTrendBars = Param(nameof(MinTrendBars), 2)
			.SetDisplay("Min Trend Bars", "Minimum trend bars", "Advanced");

		_useBreakoutConfirmation = Param(nameof(UseBreakoutConfirmation), true)
			.SetDisplay("Use Breakout", "Enable breakout confirmation", "Advanced");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevTrendUp = default;
		_prevTrendDown = default;
		_prevTrend = 1;
		_trendStrength = default;
		_prevSupertrend = default;
		_stopLossLevel = default;
		_takeProfitLevel = default;
		_prevClose = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange { Length = AtrLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		_ma = MaType switch
		{
			MaTypeEnum.Exponential => new ExponentialMovingAverage { Length = MaLength },
			MaTypeEnum.Weighted => new WeightedMovingAverage { Length = MaLength },
			_ => new SimpleMovingAverage { Length = MaLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_atr, _rsi, _ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}

		StartProtection(new Unit(), new Unit());
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal rsiValue, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_atr.IsFormed)
			return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var up = median - Multiplier * atrValue;
		var down = median + Multiplier * atrValue;

		if (_prevTrendUp == 0m)
		{
			_prevTrendUp = up;
			_prevTrendDown = down;
			_prevSupertrend = down;
			_prevClose = candle.ClosePrice;
			return;
		}

		var trendUp = _prevClose > _prevTrendUp ? Math.Max(up, _prevTrendUp) : up;
		var trendDown = _prevClose < _prevTrendDown ? Math.Min(down, _prevTrendDown) : down;
		var trend = candle.ClosePrice <= _prevTrendDown ? -1 : candle.ClosePrice >= _prevTrendUp ? 1 : _prevTrend;
		var supertrend = trend == 1 ? trendUp : trendDown;

		var supertrendBullish = trend == 1 && _prevTrend == -1;
		var supertrendBearish = trend == -1 && _prevTrend == 1;

		_trendStrength = trend != _prevTrend ? 1 : _trendStrength + 1;

		var rsiBuyCondition = !UseRsiFilter || (rsiValue > RsiOversold && rsiValue < RsiOverbought);
		var rsiSellCondition = !UseRsiFilter || (rsiValue < RsiOverbought && rsiValue > RsiOversold);
		var maBuyCondition = !UseMaFilter || candle.ClosePrice > maValue;
		var maSellCondition = !UseMaFilter || candle.ClosePrice < maValue;
		var trendStrengthCondition = !UseTrendStrength || _trendStrength >= MinTrendBars;
		var breakoutBuy = !UseBreakoutConfirmation || candle.ClosePrice > _prevSupertrend;
		var breakoutSell = !UseBreakoutConfirmation || candle.ClosePrice < _prevSupertrend;

		if (supertrendBullish && rsiBuyCondition && maBuyCondition && trendStrengthCondition && breakoutBuy && Position <= 0)
		{
			BuyMarket();
			_stopLossLevel = UseStopLoss ? candle.ClosePrice - atrValue * SlMultiplier : 0m;
			_takeProfitLevel = UseTakeProfit ? candle.ClosePrice + atrValue * TpMultiplier : 0m;
		}
		else if (supertrendBearish && rsiSellCondition && maSellCondition && trendStrengthCondition && breakoutSell && Position >= 0)
		{
			SellMarket();
			_stopLossLevel = UseStopLoss ? candle.ClosePrice + atrValue * SlMultiplier : 0m;
			_takeProfitLevel = UseTakeProfit ? candle.ClosePrice - atrValue * TpMultiplier : 0m;
		}
		else if (Position > 0)
		{
			if (UseStopLoss && candle.LowPrice <= _stopLossLevel)
				ClosePosition();
			else if (UseTakeProfit && candle.HighPrice >= _takeProfitLevel)
				ClosePosition();
		}
		else if (Position < 0)
		{
			if (UseStopLoss && candle.HighPrice >= _stopLossLevel)
				ClosePosition();
			else if (UseTakeProfit && candle.LowPrice <= _takeProfitLevel)
				ClosePosition();
		}

		_prevTrendUp = trendUp;
		_prevTrendDown = trendDown;
		_prevTrend = trend;
		_prevSupertrend = supertrend;
		_prevClose = candle.ClosePrice;
	}

	/// <summary>
	/// Moving average types.
	/// </summary>
	public enum MaTypeEnum
	{
		/// <summary>Simple moving average.</summary>
		Simple,
		/// <summary>Exponential moving average.</summary>
		Exponential,
		/// <summary>Weighted moving average.</summary>
		Weighted
	}
}
