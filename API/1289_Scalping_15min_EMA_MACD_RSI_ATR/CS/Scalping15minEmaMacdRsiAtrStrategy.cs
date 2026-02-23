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

	private AverageTrueRange _atr;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal SlAtrMultiplier { get => _slAtrMultiplier.Value; set => _slAtrMultiplier.Value = value; }
	public decimal TpAtrMultiplier { get => _tpAtrMultiplier.Value; set => _tpAtrMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Scalping15minEmaMacdRsiAtrStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period for EMA indicator", "Indicators")
			.SetOptimize(10, 100, 5);

		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA length for MACD", "Indicators");

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA length for MACD", "Indicators");

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal smoothing length", "Indicators");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "Period for RSI indicator", "Indicators");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "RSI overbought level", "Indicators");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "RSI oversold level", "Indicators");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR indicator period", "Indicators");

		_slAtrMultiplier = Param(nameof(SlAtrMultiplier), 1m)
			.SetDisplay("SL ATR Mult", "ATR multiplier for stop loss", "Risk");

		_tpAtrMultiplier = Param(nameof(TpAtrMultiplier), 2m)
			.SetDisplay("TP ATR Mult", "ATR multiplier for take profit", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_atr = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var macd = new MovingAverageConvergenceDivergenceSignal();
		macd.Macd.ShortMa.Length = MacdFast;
		macd.Macd.LongMa.Length = MacdSlow;
		macd.SignalMa.Length = MacdSignal;
		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ema, rsi, macd, ProcessCandle)
			.Start();

		// no separate protection
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaIV, IIndicatorValue rsiIV, IIndicatorValue macdIV)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var emaValue = emaIV.GetValue<decimal>();
		var rsiValue = rsiIV.GetValue<decimal>();

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdIV;
		var macdLine = macdTyped.Macd ?? 0m;
		var signalLine = macdTyped.Signal ?? 0m;
		var histValue = macdLine - signalLine;

		var atrResult = _atr.Process(candle);
		var atrValue = atrResult.IsFormed ? atrResult.GetValue<decimal>() : 0m;

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
