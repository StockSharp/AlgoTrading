using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long and Short strategy with RSI, ROC, selectable moving average and ATR trailing stop.
/// </summary>
public class LongAndShortWithMultiIndicatorsStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _rsiOverbought;
	private readonly StrategyParam<int> _rsiOversold;
	private readonly StrategyParam<int> _rocLength;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<MaType> _maType;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _bearishMaLength;
	private readonly StrategyParam<int> _bearishTrendDuration;
	private readonly StrategyParam<TimeSpan> _tradeStart;
	private readonly StrategyParam<TimeSpan> _tradeEnd;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopLossLong;
	private decimal _trailingStopLong;
	private decimal _stopLossShort;
	private decimal _trailingStopShort;
	private int _bearishCount;

	/// <summary>
	/// RSI length.
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
	/// ROC length.
	/// </summary>
	public int RocLength { get => _rocLength.Value; set => _rocLength.Value = value; }

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }

	/// <summary>
	/// Moving average type.
	/// </summary>
	public MaType MaTypeParam { get => _maType.Value; set => _maType.Value = value; }

	/// <summary>
	/// ATR length.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	/// <summary>
	/// ATR multiplier for stops.
	/// </summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

	/// <summary>
	/// Length of MA used to detect bear trend.
	/// </summary>
	public int BearishMaLength { get => _bearishMaLength.Value; set => _bearishMaLength.Value = value; }

	/// <summary>
	/// Number of consecutive bearish bars to confirm trend.
	/// </summary>
	public int BearishTrendDuration { get => _bearishTrendDuration.Value; set => _bearishTrendDuration.Value = value; }

	/// <summary>
	/// Trading start time of day.
	/// </summary>
	public TimeSpan TradeStart { get => _tradeStart.Value; set => _tradeStart.Value = value; }

	/// <summary>
	/// Trading end time of day.
	/// </summary>
	public TimeSpan TradeEnd { get => _tradeEnd.Value; set => _tradeEnd.Value = value; }

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes <see cref="LongAndShortWithMultiIndicatorsStrategy"/>.
	/// </summary>
	public LongAndShortWithMultiIndicatorsStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 5)
			.SetDisplay("RSI Length", "Length of RSI", "Indicators")
			.SetCanOptimize(true)
			.SetGreaterThanZero()
			.SetOptimize(2, 20, 1);

		_rsiOverbought = Param(nameof(RsiOverbought), 70)
			.SetDisplay("RSI Overbought", "RSI overbought level", "Indicators");

		_rsiOversold = Param(nameof(RsiOversold), 44)
			.SetDisplay("RSI Oversold", "RSI oversold level", "Indicators");

		_rocLength = Param(nameof(RocLength), 4)
			.SetDisplay("ROC Length", "Length of ROC", "Indicators")
			.SetGreaterThanZero();

		_maLength = Param(nameof(MaLength), 24)
			.SetDisplay("MA Length", "Length of moving average", "Indicators")
			.SetGreaterThanZero();

		_maType = Param(nameof(MaTypeParam), MaType.Tema)
			.SetDisplay("MA Type", "Type of moving average", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "Length of ATR", "Risk")
			.SetGreaterThanZero();

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetDisplay("ATR Multiplier", "ATR multiplier", "Risk");

		_bearishMaLength = Param(nameof(BearishMaLength), 200)
			.SetDisplay("Bear MA Length", "Length for bearish MA", "Bear Market")
			.SetGreaterThanZero();

		_bearishTrendDuration = Param(nameof(BearishTrendDuration), 5)
			.SetDisplay("Bear Trend Bars", "Bars to confirm bearish trend", "Bear Market")
			.SetGreaterThanZero();

		_tradeStart = Param(nameof(TradeStart), TimeSpan.Zero)
			.SetDisplay("Trade Start", "Start time", "Trading Hours");

		_tradeEnd = Param(nameof(TradeEnd), new TimeSpan(23, 59, 59))
			.SetDisplay("Trade End", "End time", "Trading Hours");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var roc = new RateOfChange { Length = RocLength };
		var ma = CreateMa(MaTypeParam, MaLength);
		var bearishMa = new SimpleMovingAverage { Length = BearishMaLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, roc, ma, bearishMa, atr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawIndicator(area, bearishMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal rocValue, decimal maValue, decimal bearishMaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var time = candle.CloseTime.TimeOfDay;
		var inTradeTime = time >= TradeStart && time <= TradeEnd;

		var bearishTrend = close < bearishMaValue;
		_bearishCount = bearishTrend ? _bearishCount + 1 : 0;
		var bearishTrendConfirmed = _bearishCount >= BearishTrendDuration;

		var rsiCondition = rsiValue < RsiOverbought && rsiValue > RsiOversold;
		var rocCondition = rocValue > 0m;
		var maCondition = close > maValue;

		var longCondition = rsiCondition && rocCondition && maCondition;
		var shortCondition = bearishTrendConfirmed && rocValue < 0m && close < maValue;
		var stopConditionLong = rsiValue < RsiOversold && rocValue < 0m && close < maValue;
		var stopConditionShort = rsiValue > RsiOverbought && rocValue > 0m && close > maValue;

		if (!inTradeTime)
			return;

		if (Position == 0)
		{
			if (longCondition)
			{
				BuyMarket();
				_stopLossLong = close - atrValue * AtrMultiplier;
				_trailingStopLong = _stopLossLong;
			}
			else if (shortCondition)
			{
				SellMarket();
				_stopLossShort = close + atrValue * AtrMultiplier;
				_trailingStopShort = _stopLossShort;
			}
		}
		else if (Position > 0)
		{
			_trailingStopLong = Math.Max(_trailingStopLong, close - atrValue * AtrMultiplier);

			if (stopConditionLong || close <= _trailingStopLong)
				SellMarket(Position);
		}
		else
		{
			_trailingStopShort = Math.Min(_trailingStopShort, close + atrValue * AtrMultiplier);

			if (stopConditionShort || close >= _trailingStopShort)
				BuyMarket(Math.Abs(Position));
		}
	}

	private static IIndicator CreateMa(MaType type, int length)
	{
		return type switch
		{
			MaType.Sma => new SimpleMovingAverage { Length = length },
			MaType.Ema => new ExponentialMovingAverage { Length = length },
			MaType.Wma => new WeightedMovingAverage { Length = length },
			MaType.Hma => new HullMovingAverage { Length = length },
			MaType.Vwma => new VolumeWeightedMovingAverage { Length = length },
			MaType.Rma => new SmoothedMovingAverage { Length = length },
			MaType.Tema => new TripleExponentialMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length }
		};
	}

	/// <summary>
	/// Moving average types.
	/// </summary>
	public enum MaType
	{
		Sma = 1,
		Ema,
		Wma,
		Hma,
		Vwma,
		Rma,
		Tema
	}
}
