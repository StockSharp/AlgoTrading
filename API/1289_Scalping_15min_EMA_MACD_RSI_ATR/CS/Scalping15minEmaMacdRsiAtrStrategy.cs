using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Scalping strategy using EMA, MACD, RSI and ATR-based stop loss / take profit.
/// </summary>
public class Scalping15minEmaMacdRsiAtrStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _slAtrMultiplier;
	private readonly StrategyParam<decimal> _tpAtrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema;
	private MovingAverageConvergenceDivergence _macd;
	private RelativeStrengthIndex _rsi;
	private AverageTrueRange _atr;

	/// <summary>
	/// EMA calculation period.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// MACD fast length.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// MACD slow length.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// MACD signal smoothing length.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
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
	/// ATR calculation period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop loss.
	/// </summary>
	public decimal SlAtrMultiplier
	{
		get => _slAtrMultiplier.Value;
		set => _slAtrMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier for take profit.
	/// </summary>
	public decimal TpAtrMultiplier
	{
		get => _tpAtrMultiplier.Value;
		set => _tpAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type for the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public Scalping15minEmaMacdRsiAtrStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period for EMA indicator", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 5);

		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA length for MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(6, 18, 2);

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA length for MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 2);

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal smoothing length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(6, 18, 1);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period for RSI indicator", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 1);

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetRange(0m, 100m)
			.SetDisplay("RSI Overbought", "RSI overbought level", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(60m, 90m, 5m);

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetRange(0m, 100m)
			.SetDisplay("RSI Oversold", "RSI oversold level", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10m, 40m, 5m);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR indicator period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 7);

		_slAtrMultiplier = Param(nameof(SlAtrMultiplier), 1m)
			.SetRange(0.1m, decimal.MaxValue)
			.SetDisplay("SL ATR Mult", "ATR multiplier for stop loss", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3m, 0.5m);

		_tpAtrMultiplier = Param(nameof(TpAtrMultiplier), 2m)
			.SetRange(0.1m, decimal.MaxValue)
			.SetDisplay("TP ATR Mult", "ATR multiplier for take profit", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy", "Common");
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

		_ema = null;
		_macd = null;
		_rsi = null;
		_atr = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema = new ExponentialMovingAverage { Length = EmaPeriod };
		_macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = MacdFast,
			LongPeriod = MacdSlow,
			SignalPeriod = MacdSignal
		};
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_macd, _ema, _rsi, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawIndicator(area, _macd);
			DrawIndicator(area, _rsi);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal macdValue, decimal signalValue, decimal histValue, decimal emaValue, decimal rsiValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			var stopLevel = candle.ClosePrice - SlAtrMultiplier * atrValue;
			var takeLevel = candle.ClosePrice + TpAtrMultiplier * atrValue;

			if (candle.LowPrice <= stopLevel || candle.HighPrice >= takeLevel)
			{
				SellMarket(Position);
				return;
			}
		}
		else if (Position < 0)
		{
			var stopLevel = candle.ClosePrice + SlAtrMultiplier * atrValue;
			var takeLevel = candle.ClosePrice - TpAtrMultiplier * atrValue;

			if (candle.HighPrice >= stopLevel || candle.LowPrice <= takeLevel)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}
		}

		var longCond = candle.ClosePrice > emaValue && histValue > 0m && rsiValue > 50m && rsiValue < RsiOverbought;
		var shortCond = candle.ClosePrice < emaValue && histValue < 0m && rsiValue < 50m && rsiValue > RsiOversold;

		if (longCond && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (shortCond && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
	}
}

