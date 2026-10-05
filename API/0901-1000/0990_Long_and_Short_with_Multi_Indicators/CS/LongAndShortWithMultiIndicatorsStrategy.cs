using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Long and short strategy with RSI, ROC and a selectable moving average.
/// Goes long when RSI is between the oversold and overbought levels, ROC is positive and price is above the MA. Goes short when a bearish
/// trend is confirmed (close below the bearish SMA for BearishTrendDuration bars), ROC is negative and price is below the MA.
/// Positions exit on an ATR trailing stop, a long when RSI rises above overbought and a short when RSI falls below oversold.
/// </summary>
public class LongAndShortWithMultiIndicatorsStrategy : Strategy
{
	/// <summary>
	/// Moving average types.
	/// </summary>
	public enum MaTypes
	{
		/// <summary>
		/// Simple moving average.
		/// </summary>
		SMA,

		/// <summary>
		/// Exponential moving average.
		/// </summary>
		EMA,

		/// <summary>
		/// Weighted moving average.
		/// </summary>
		WMA,

		/// <summary>
		/// Double exponential moving average.
		/// </summary>
		DEMA,

		/// <summary>
		/// Triple exponential moving average.
		/// </summary>
		TEMA,

		/// <summary>
		/// Hull moving average.
		/// </summary>
		HMA,

		/// <summary>
		/// Smoothed moving average.
		/// </summary>
		SMMA,
	}

	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<int> _rocLength;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<MaTypes> _maTypeParam;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _bearishMaLength;
	private readonly StrategyParam<int> _bearishTrendDuration;
	private readonly StrategyParam<DataType> _candleType;

	private int _bearishBars;
	private decimal? _trailPrice;

	/// <summary>
	/// RSI period.
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
	/// ROC period.
	/// </summary>
	public int RocLength
	{
		get => _rocLength.Value;
		set => _rocLength.Value = value;
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
	public MaTypes MaTypeParam
	{
		get => _maTypeParam.Value;
		set => _maTypeParam.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the trailing stop.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// SMA period that defines the bearish trend.
	/// </summary>
	public int BearishMaLength
	{
		get => _bearishMaLength.Value;
		set => _bearishMaLength.Value = value;
	}

	/// <summary>
	/// Consecutive closes below the bearish SMA that confirm the bearish trend.
	/// </summary>
	public int BearishTrendDuration
	{
		get => _bearishTrendDuration.Value;
		set => _bearishTrendDuration.Value = value;
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
	public LongAndShortWithMultiIndicatorsStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "RSI overbought level", "Indicators");

		_rsiOversold = Param(nameof(RsiOversold), 44m)
			.SetDisplay("RSI Oversold", "RSI oversold level", "Indicators");

		_rocLength = Param(nameof(RocLength), 4)
			.SetGreaterThanZero()
			.SetDisplay("ROC Length", "ROC period", "Indicators");

		_maLength = Param(nameof(MaLength), 24)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average period", "Indicators");

		_maTypeParam = Param(nameof(MaTypeParam), MaTypes.TEMA)
			.SetDisplay("MA Type", "Moving average type", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetNotNegative()
			.SetDisplay("ATR Multiplier", "ATR multiplier of the trailing stop", "Risk");

		_bearishMaLength = Param(nameof(BearishMaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("Bearish MA Length", "SMA period that defines the bearish trend", "Trend");

		_bearishTrendDuration = Param(nameof(BearishTrendDuration), 5)
			.SetGreaterThanZero()
			.SetDisplay("Bearish Duration", "Consecutive closes below the bearish SMA", "Trend");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_bearishBars = 0;
		_trailPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_bearishBars = 0;
		_trailPrice = null;

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var roc = new RateOfChange { Length = RocLength };
		var ma = CreateMa(MaTypeParam, MaLength);
		var atr = new AverageTrueRange { Length = AtrLength };
		var bearishMa = new SimpleMovingAverage { Length = BearishMaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rsi, roc, ma, atr, bearishMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawIndicator(area, bearishMa);
			DrawOwnTrades(area);
		}
	}

	private static IIndicator CreateMa(MaTypes type, int length)
	{
		return type switch
		{
			MaTypes.SMA => new SimpleMovingAverage { Length = length },
			MaTypes.EMA => new ExponentialMovingAverage { Length = length },
			MaTypes.WMA => new WeightedMovingAverage { Length = length },
			MaTypes.DEMA => new DoubleExponentialMovingAverage { Length = length },
			MaTypes.HMA => new HullMovingAverage { Length = length },
			MaTypes.SMMA => new SmoothedMovingAverage { Length = length },
			_ => new TripleExponentialMovingAverage { Length = length },
		};
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue, IIndicatorValue rocValue, IIndicatorValue maValue, IIndicatorValue atrValue, IIndicatorValue bearishMaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		if (bearishMaValue.IsFormed)
			_bearishBars = close < bearishMaValue.GetValue<decimal>() ? _bearishBars + 1 : 0;

		if (!rsiValue.IsFormed || !rocValue.IsFormed || !maValue.IsFormed || !atrValue.IsFormed)
			return;

		var rsi = rsiValue.GetValue<decimal>();
		var roc = rocValue.GetValue<decimal>();
		var ma = maValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();

		if (Position > 0)
		{
			if (AtrMultiplier > 0)
				_trailPrice = Math.Max(_trailPrice ?? decimal.MinValue, close - atr * AtrMultiplier);

			if ((_trailPrice is decimal stop && candle.LowPrice <= stop) || rsi > RsiOverbought)
			{
				SellMarket(Position);
				_trailPrice = null;
				return;
			}
		}
		else if (Position < 0)
		{
			if (AtrMultiplier > 0)
				_trailPrice = Math.Min(_trailPrice ?? decimal.MaxValue, close + atr * AtrMultiplier);

			if ((_trailPrice is decimal stop && candle.HighPrice >= stop) || rsi < RsiOversold)
			{
				BuyMarket(-Position);
				_trailPrice = null;
				return;
			}
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var longSignal = rsi > RsiOversold && rsi < RsiOverbought && roc > 0 && close > ma;
		var shortSignal = _bearishBars >= BearishTrendDuration && roc < 0 && close < ma;

		if (longSignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_trailPrice = null;
		}
		else if (shortSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_trailPrice = null;
		}
	}
}
