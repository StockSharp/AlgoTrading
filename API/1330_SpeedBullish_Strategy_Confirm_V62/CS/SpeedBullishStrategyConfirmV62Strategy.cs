using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on EMA, MACD histogram and RSI with optional ATR and volume confirmations.
/// Enters long when bullish conditions are met and short on opposite conditions.
/// Applies take profit, trailing stop and manual stop loss in points.
/// </summary>
public class SpeedBullishStrategyConfirmV62Strategy : Strategy
{
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiEntryLevel;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingDistancePoints;
	private readonly StrategyParam<bool> _useAtrConfirmation;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<bool> _useVolumeConfirmation;
	private readonly StrategyParam<int> _volumeLength;
	private readonly StrategyParam<decimal> _volumeThresholdMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema10 = null!;
	private ExponentialMovingAverage _ema15 = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private RelativeStrengthIndex _rsi = null!;
	private AverageTrueRange _atr = null!;
	private SimpleMovingAverage _atrSma = null!;
	private SimpleMovingAverage _volumeSma = null!;

	private decimal _prevHist;
	private decimal _entryPrice;

	/// <summary>
	/// MACD fast length.
	/// </summary>
	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }

	/// <summary>
	/// MACD slow length.
	/// </summary>
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }

	/// <summary>
	/// MACD signal smoothing length.
	/// </summary>
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }

	/// <summary>
	/// RSI period length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// RSI threshold for entries.
	/// </summary>
	public decimal RsiEntryLevel { get => _rsiEntryLevel.Value; set => _rsiEntryLevel.Value = value; }

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }

	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }

	/// <summary>
	/// Trailing distance in points.
	/// </summary>
	public decimal TrailingDistancePoints { get => _trailingDistancePoints.Value; set => _trailingDistancePoints.Value = value; }

	/// <summary>
	/// Use ATR confirmation.
	/// </summary>
	public bool UseAtrConfirmation { get => _useAtrConfirmation.Value; set => _useAtrConfirmation.Value = value; }

	/// <summary>
	/// ATR period length.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	/// <summary>
	/// ATR multiplier for confirmation.
	/// </summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

	/// <summary>
	/// Use volume confirmation.
	/// </summary>
	public bool UseVolumeConfirmation { get => _useVolumeConfirmation.Value; set => _useVolumeConfirmation.Value = value; }

	/// <summary>
	/// Volume SMA period.
	/// </summary>
	public int VolumeLength { get => _volumeLength.Value; set => _volumeLength.Value = value; }

	/// <summary>
	/// Volume threshold multiplier.
	/// </summary>
	public decimal VolumeThresholdMultiplier { get => _volumeThresholdMultiplier.Value; set => _volumeThresholdMultiplier.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public SpeedBullishStrategyConfirmV62Strategy()
	{
		_macdFast = Param(nameof(MacdFast), 8)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast Length", "Fast EMA period", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(4, 20, 2);

		_macdSlow = Param(nameof(MacdSlow), 21)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow Length", "Slow EMA period", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(14, 34, 2);

		_macdSignal = Param(nameof(MacdSignal), 6)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal Length", "Signal line period", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(3, 12, 1);

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 7);

		_rsiEntryLevel = Param(nameof(RsiEntryLevel), 50m)
			.SetRange(0m, 100m)
			.SetDisplay("RSI Entry Level", "RSI threshold", "RSI");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit Points", "Take profit distance in ticks", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20m, 100m, 10m);

		_stopLossPoints = Param(nameof(StopLossPoints), 30m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss Points", "Stop loss distance in ticks", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 60m, 10m);

		_trailingDistancePoints = Param(nameof(TrailingDistancePoints), 300m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Distance Points", "Trailing stop distance in ticks", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(100m, 500m, 50m);

		_useAtrConfirmation = Param(nameof(UseAtrConfirmation), true)
			.SetDisplay("Use ATR Confirmation", "Enable ATR based filter", "Filters");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 7);

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Multiplier for ATR SMA", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_useVolumeConfirmation = Param(nameof(UseVolumeConfirmation), true)
			.SetDisplay("Use Volume Confirmation", "Enable volume filter", "Filters");

		_volumeLength = Param(nameof(VolumeLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume SMA Length", "Volume moving average period", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_volumeThresholdMultiplier = Param(nameof(VolumeThresholdMultiplier), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Multiplier", "Multiplier for volume threshold", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 2m, 0.25m);

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
		_prevHist = 0m;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema10 = new ExponentialMovingAverage { Length = 10 };
		_ema15 = new ExponentialMovingAverage { Length = 15 };
		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};
		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_atr = new AverageTrueRange { Length = AtrLength };
		_atrSma = new SimpleMovingAverage { Length = AtrLength };
		_volumeSma = new SimpleMovingAverage { Length = VolumeLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, _rsi, _atr, ProcessCandle)
			.Start();

		var step = Security?.PriceStep ?? 1m;
		StartProtection(
			takeProfit: new Unit(TakeProfitPoints * step, UnitTypes.Absolute),
			stopLoss: new Unit(TrailingDistancePoints * step, UnitTypes.Absolute),
			isStopTrailing: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema10);
			DrawIndicator(area, _ema15);
			DrawIndicator(area, _macd);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue, IIndicatorValue rsiValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var hlcc4 = (candle.HighPrice + candle.LowPrice + candle.ClosePrice + candle.ClosePrice) / 4m;
		var ema10Val = _ema10.Process(hlcc4);
		var ema15Val = _ema15.Process(hlcc4);
		var volumeVal = _volumeSma.Process(candle.Volume);
		var atrSmaVal = _atrSma.Process((decimal)atrValue);

		if (!ema10Val.IsFinal || !ema15Val.IsFinal || !macdValue.IsFinal || !rsiValue.IsFinal)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var ema10 = (decimal)ema10Val;
		var ema15 = (decimal)ema15Val;
		var rsi = (decimal)rsiValue;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
		return;
		var hist = macd - signal;

		var macdCrossUp = hist > 0m && _prevHist <= 0m;
		var macdCrossDown = hist < 0m && _prevHist >= 0m;
		_prevHist = hist;

		var rsiOkBuy = rsi > RsiEntryLevel;
		var rsiOkSell = rsi < RsiEntryLevel;
		var emaOkBuy = candle.ClosePrice > ema10 || candle.ClosePrice > ema15;
		var emaOkSell = candle.ClosePrice < ema10 || candle.ClosePrice < ema15;

		var buyCondition = emaOkBuy && macdCrossUp && rsiOkBuy;
		var sellCondition = emaOkSell && macdCrossDown && rsiOkSell;

		if (UseAtrConfirmation)
		{
		if (!atrValue.IsFinal || !atrSmaVal.IsFinal)
		return;
		var atr = (decimal)atrValue;
		var atrAvg = (decimal)atrSmaVal;
		var highVolatility = atr > AtrMultiplier * atrAvg;
		buyCondition &= highVolatility;
		sellCondition &= highVolatility;
		}

		if (UseVolumeConfirmation)
		{
		if (!volumeVal.IsFinal)
		return;
		var avgVolume = (decimal)volumeVal;
		var highVolume = candle.Volume > avgVolume * VolumeThresholdMultiplier;
		buyCondition &= highVolume;
		sellCondition &= highVolume;
		}

		var step = Security?.PriceStep ?? 1m;
		var stopLossPrice = StopLossPoints * step;

		if (buyCondition && Position <= 0)
		{
		BuyMarket();
		_entryPrice = candle.ClosePrice;
		}
		else if (sellCondition && Position >= 0)
		{
		SellMarket();
		_entryPrice = candle.ClosePrice;
		}

		if (Position > 0 && candle.ClosePrice <= _entryPrice - stopLossPrice)
		SellMarket(Position);
		else if (Position < 0 && candle.ClosePrice >= _entryPrice + stopLossPrice)
		BuyMarket(Math.Abs(Position));
	}
}
