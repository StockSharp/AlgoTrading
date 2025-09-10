using System;
using System.Collections.Generic;

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
	private readonly StrategyParam<int> _volLookback;
	private readonly StrategyParam<decimal> _volSpikeMultiplier;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private bool _prevShortSetup;
	private bool _prevLongSetup;
	private decimal _prevMacdHist;
	private readonly SimpleMovingAverage _volSma = new();

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public int RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }
	public int VolLookback { get => _volLookback.Value; set => _volLookback.Value = value; }
	public decimal VolSpikeMultiplier { get => _volSpikeMultiplier.Value; set => _volSpikeMultiplier.Value = value; }
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

		_atrMultiplier = Param(nameof(AtrMultiplier), 4.4m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Multiplier for ATR bands", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Length for RSI", "Indicators");

		_rsiOverbought = Param(nameof(RsiOverbought), 68)
			.SetDisplay("RSI Overbought", "Overbought level for RSI", "Indicators");

		_rsiOversold = Param(nameof(RsiOversold), 28)
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

		_volLookback = Param(nameof(VolLookback), 16)
			.SetGreaterThanZero()
			.SetDisplay("Volume MA Length", "Length for volume moving average", "Filters");

		_volSpikeMultiplier = Param(nameof(VolSpikeMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Sell Spike Mult", "Multiplier for volume spike", "Filters");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 0.75m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (%)", "Take profit percent", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 0.4m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (%)", "Stop loss percent", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_volSma.Length = VolLookback;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

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

		StartProtection(new Unit(TakeProfitPercent, UnitTypes.Percent),
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
		DrawIndicator(area, atr);
		DrawIndicator(area, rsi);
		DrawIndicator(area, macd);
		DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue emaValue, IIndicatorValue atrValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var ema = emaValue.ToDecimal();
		var atr = atrValue.ToDecimal();
		var rsi = rsiValue.ToDecimal();
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macdHist = macdTyped.Histogram;

		var upperBand = ema + AtrMultiplier * atr;
		var lowerBand = ema - AtrMultiplier * atr;
		var volumeSma = _volSma.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();
		var sellSpike = candle.TotalVolume > volumeSma * VolSpikeMultiplier && candle.ClosePrice < candle.OpenPrice;

		var shortSetup = candle.HighPrice > upperBand &&
		rsi > RsiOverbought &&
		macdHist < _prevMacdHist && _prevMacdHist > 0 &&
		candle.ClosePrice < candle.OpenPrice;

		var longSetup = candle.LowPrice < lowerBand &&
		rsi < RsiOversold &&
		macdHist > _prevMacdHist && _prevMacdHist < 0 &&
		candle.ClosePrice > candle.OpenPrice;

		var shortConfirmed = shortSetup && !_prevShortSetup;
		var longConfirmed = longSetup && !_prevLongSetup && !sellSpike;

		if (shortConfirmed && Position >= 0)
		{
		var volume = Volume + Math.Abs(Position);
		SellMarket(volume);
		}
		else if (longConfirmed && Position <= 0)
		{
		var volume = Volume + Math.Abs(Position);
		BuyMarket(volume);
		}

		_prevShortSetup = shortSetup;
		_prevLongSetup = longSetup;
		_prevMacdHist = macdHist;
	}
}
