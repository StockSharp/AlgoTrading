using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// BTC chop top/bottom reversal strategy.
/// Enters when price reverses at ATR bands with MACD histogram confirmation.
/// </summary>
public class BtcChopReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiOverbought;
	private readonly StrategyParam<int> _rsiOversold;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private bool _prevShortSetup;
	private bool _prevLongSetup;
	private decimal _prevMacdHist;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public int RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BtcChopReversalStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 23)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Length of EMA", "Indicators");

		_atrLength = Param(nameof(AtrLength), 55)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR calculation length", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Multiplier for ATR bands", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Length for RSI", "Indicators");

		_rsiOverbought = Param(nameof(RsiOverbought), 63)
			.SetDisplay("RSI Overbought", "Overbought level for RSI", "Indicators");

		_rsiOversold = Param(nameof(RsiOversold), 37)
			.SetDisplay("RSI Oversold", "Oversold level for RSI", "Indicators");

		_macdFast = Param(nameof(MacdFast), 14)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA length for MACD", "Indicators");

		_macdSlow = Param(nameof(MacdSlow), 44)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA length for MACD", "Indicators");

		_macdSignal = Param(nameof(MacdSignal), 3)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal length for MACD", "Indicators");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 0.75m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (%)", "Take profit percent", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 0.4m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (%)", "Stop loss percent", "Risk");

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
		_prevShortSetup = false;
		_prevLongSetup = false;
		_prevMacdHist = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = EmaPeriod };
		var atr = new AverageTrueRange { Length = AtrLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};

		StartProtection(
			new Unit(TakeProfitPercent, UnitTypes.Percent),
			new Unit(StopLossPercent, UnitTypes.Percent));

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(macd, ema, atr, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue emaValue, IIndicatorValue atrValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var emaVal = emaValue.ToDecimal();
		var atrVal = atrValue.ToDecimal();
		var rsiVal = rsiValue.ToDecimal();

		if (macdValue is not IMovingAverageConvergenceDivergenceSignalValue macdTyped)
			return;

		var macdLine = macdTyped.Macd ?? 0m;
		var signalLine = macdTyped.Signal ?? 0m;
		var macdHist = macdLine - signalLine;

		if (atrVal <= 0)
			return;

		var upperBand = emaVal + AtrMultiplier * atrVal;
		var lowerBand = emaVal - AtrMultiplier * atrVal;

		var shortSetup = candle.HighPrice > upperBand &&
			rsiVal > RsiOverbought &&
			macdHist < _prevMacdHist &&
			candle.ClosePrice < candle.OpenPrice;

		var longSetup = candle.LowPrice < lowerBand &&
			rsiVal < RsiOversold &&
			macdHist > _prevMacdHist &&
			candle.ClosePrice > candle.OpenPrice;

		var shortConfirmed = shortSetup && !_prevShortSetup;
		var longConfirmed = longSetup && !_prevLongSetup;

		if (shortConfirmed && Position >= 0)
		{
			SellMarket();
		}
		else if (longConfirmed && Position <= 0)
		{
			BuyMarket();
		}

		_prevShortSetup = shortSetup;
		_prevLongSetup = longSetup;
		_prevMacdHist = macdHist;
	}
}
