using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long-only strategy combining trend, volatility and volume filters.
/// </summary>
public class BonkLongVolatilityStrategy : Strategy
{
	private readonly StrategyParam<decimal> _profitTargetPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiOverbought;
	private readonly StrategyParam<int> _rsiOversold;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _volumeSmaLength;
	private readonly StrategyParam<decimal> _volumeThreshold;
	private readonly StrategyParam<int> _maFastLength;
	private readonly StrategyParam<int> _maSlowLength;
	private readonly StrategyParam<int> _lookbackDays;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _trailingStop;
	private DateTimeOffset _timeFilter;

	/// <summary>
	/// Profit target in percent.
	/// </summary>
	public decimal ProfitTargetPercent { get => _profitTargetPercent.Value; set => _profitTargetPercent.Value = value; }

	/// <summary>
	/// Stop loss in percent.
	/// </summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	/// <summary>
	/// ATR multiplier for volatility threshold.
	/// </summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public int RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public int RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }

	/// <summary>
	/// MACD fast period.
	/// </summary>
	public int MacdFast { get => _macdFast.Value; set => _macdFast.Value = value; }

	/// <summary>
	/// MACD slow period.
	/// </summary>
	public int MacdSlow { get => _macdSlow.Value; set => _macdSlow.Value = value; }

	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int MacdSignal { get => _macdSignal.Value; set => _macdSignal.Value = value; }

	/// <summary>
	/// Volume SMA length.
	/// </summary>
	public int VolumeSmaLength { get => _volumeSmaLength.Value; set => _volumeSmaLength.Value = value; }

	/// <summary>
	/// Volume spike threshold.
	/// </summary>
	public decimal VolumeThreshold { get => _volumeThreshold.Value; set => _volumeThreshold.Value = value; }

	/// <summary>
	/// Fast MA length.
	/// </summary>
	public int MaFastLength { get => _maFastLength.Value; set => _maFastLength.Value = value; }

	/// <summary>
	/// Slow MA length.
	/// </summary>
	public int MaSlowLength { get => _maSlowLength.Value; set => _maSlowLength.Value = value; }

	/// <summary>
	/// Number of days to look back for trading.
	/// </summary>
	public int LookbackDays { get => _lookbackDays.Value; set => _lookbackDays.Value = value; }

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public BonkLongVolatilityStrategy()
	{
		_profitTargetPercent = Param(nameof(ProfitTargetPercent), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Target %", "Take profit percentage", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk");

		_atrLength = Param(nameof(AtrLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR calculation period", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Volatility threshold multiplier", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI calculation period", "Indicators");

		_rsiOverbought = Param(nameof(RsiOverbought), 65)
			.SetDisplay("RSI Overbought", "Overbought level", "Indicators");

		_rsiOversold = Param(nameof(RsiOversold), 35)
			.SetDisplay("RSI Oversold", "Oversold level", "Indicators");

		_macdFast = Param(nameof(MacdFast), 12)
			.SetDisplay("MACD Fast", "MACD fast length", "Indicators");

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetDisplay("MACD Slow", "MACD slow length", "Indicators");

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetDisplay("MACD Signal", "MACD signal length", "Indicators");

		_volumeSmaLength = Param(nameof(VolumeSmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume SMA Length", "Volume average period", "Indicators");

		_volumeThreshold = Param(nameof(VolumeThreshold), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Threshold", "Volume spike multiplier", "Indicators");

		_maFastLength = Param(nameof(MaFastLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Length", "Fast moving average", "Indicators");

		_maSlowLength = Param(nameof(MaSlowLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Length", "Slow moving average", "Indicators");

		_lookbackDays = Param(nameof(LookbackDays), 30)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Days", "Only trade recent candles", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
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
		_trailingStop = 0m;
		_timeFilter = DateTimeOffset.MinValue;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_timeFilter = DateTimeOffset.Now - TimeSpan.FromDays(LookbackDays);

		var fastMa = new SimpleMovingAverage { Length = MaFastLength };
		var slowMa = new SimpleMovingAverage { Length = MaSlowLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow }
			},
			SignalMa = { Length = MacdSignal }
		};
		var volumeSma = new SimpleMovingAverage { Length = VolumeSmaLength };

		StartProtection(
			new Unit(ProfitTargetPercent, UnitTypes.Percent),
			new Unit(StopLossPercent, UnitTypes.Percent)
		);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fastMa, slowMa, atr, rsi, macd, volumeSma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(
		ICandleMessage candle,
		IIndicatorValue fastMaValue,
		IIndicatorValue slowMaValue,
		IIndicatorValue atrValue,
		IIndicatorValue rsiValue,
		IIndicatorValue macdValue,
		IIndicatorValue volumeSmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (candle.OpenTime < _timeFilter)
			return;

		var fast = fastMaValue.ToDecimal();
		var slow = slowMaValue.ToDecimal();
		var atr = atrValue.ToDecimal();
		var rsi = rsiValue.ToDecimal();
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macdLine = macdTyped.Macd;
		var signalLine = macdTyped.Signal;
		var volumeAvg = volumeSmaValue.ToDecimal();

		var bullishTrend = fast > slow;
		var priceRange = candle.HighPrice - candle.LowPrice;
		var volatileCondition = priceRange > atr * AtrMultiplier;
		var macdBullish = macdLine > signalLine && macdLine > 0;
		var volumeSpike = candle.TotalVolume > volumeAvg * VolumeThreshold;
		var rsiOk = rsi > RsiOversold && rsi < RsiOverbought;

		if (bullishTrend && volatileCondition && macdBullish && volumeSpike && rsiOk && candle.ClosePrice > fast && Position <= 0)
		{
			CancelActiveOrders();
			BuyMarket(Volume + Math.Abs(Position));
			_trailingStop = candle.ClosePrice - atr * 100m;
			return;
		}

		if (Position > 0)
		{
			var trailDistance = atr * 100m;
			var newStop = candle.ClosePrice - trailDistance;
			if (_trailingStop == 0m || newStop > _trailingStop)
				_trailingStop = newStop;

			if (candle.LowPrice <= _trailingStop)
			{
				SellMarket(Position);
				_trailingStop = 0m;
			}
		}
	}
}
