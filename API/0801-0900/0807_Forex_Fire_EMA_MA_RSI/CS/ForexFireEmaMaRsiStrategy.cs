using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Forex Fire EMA MA RSI strategy.
/// On entry candles it goes long when the short EMA is above the long EMA, the close is above the moving average, the fast RSI is
/// above the slow RSI and above 50, volume rose against the previous candle and on the confluence timeframe the short EMA is above the
/// long EMA; the short entry mirrors every condition. An opposite entry reverses the position. A long closes when the short EMA drops
/// below the long EMA or the fast RSI reaches RsiOverbought, a short when the short EMA rises above the long EMA or the fast RSI
/// reaches RsiOversold. Optional percent stop loss, take profit and trailing stop, and an ATR exit AtrMultiplier ATRs against the
/// entry, close the position as well.
/// </summary>
public class ForexFireEmaMaRsiStrategy : Strategy
{
	/// <summary>
	/// Moving average types.
	/// </summary>
	public enum MovingAverageTypeEnum
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

		/// <summary>
		/// Smoothed moving average.
		/// </summary>
		Smoothed,
	}

	private const decimal _rsiMiddle = 50m;

	private readonly StrategyParam<int> _emaShortLength;
	private readonly StrategyParam<int> _emaLongLength;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<MovingAverageTypeEnum> _maType;
	private readonly StrategyParam<int> _rsiSlowLength;
	private readonly StrategyParam<int> _rsiFastLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingPercent;
	private readonly StrategyParam<bool> _useAtrExits;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<DataType> _entryCandleType;
	private readonly StrategyParam<DataType> _confluenceCandleType;

	private int _htfTrend;
	private decimal? _prevVolume;
	private decimal _entryPrice;
	private decimal _bestPrice;

	/// <summary>
	/// Short EMA length.
	/// </summary>
	public int EmaShortLength
	{
		get => _emaShortLength.Value;
		set => _emaShortLength.Value = value;
	}

	/// <summary>
	/// Long EMA length.
	/// </summary>
	public int EmaLongLength
	{
		get => _emaLongLength.Value;
		set => _emaLongLength.Value = value;
	}

	/// <summary>
	/// Trend moving average length.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Trend moving average type.
	/// </summary>
	public MovingAverageTypeEnum MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Slow RSI length.
	/// </summary>
	public int RsiSlowLength
	{
		get => _rsiSlowLength.Value;
		set => _rsiSlowLength.Value = value;
	}

	/// <summary>
	/// Fast RSI length.
	/// </summary>
	public int RsiFastLength
	{
		get => _rsiFastLength.Value;
		set => _rsiFastLength.Value = value;
	}

	/// <summary>
	/// Fast RSI level that closes a long.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// Fast RSI level that closes a short.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// Use the percent stop loss.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Use the percent take profit.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// Take profit percentage from entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Use the percent trailing stop.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Trailing stop percentage from the best price.
	/// </summary>
	public decimal TrailingPercent
	{
		get => _trailingPercent.Value;
		set => _trailingPercent.Value = value;
	}

	/// <summary>
	/// Use the ATR exit.
	/// </summary>
	public bool UseAtrExits
	{
		get => _useAtrExits.Value;
		set => _useAtrExits.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the exit distance.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// ATR length.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Entry candle type.
	/// </summary>
	public DataType EntryCandleType
	{
		get => _entryCandleType.Value;
		set => _entryCandleType.Value = value;
	}

	/// <summary>
	/// Confluence candle type.
	/// </summary>
	public DataType ConfluenceCandleType
	{
		get => _confluenceCandleType.Value;
		set => _confluenceCandleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ForexFireEmaMaRsiStrategy()
	{
		_emaShortLength = Param(nameof(EmaShortLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("EMA Short", "Short EMA length", "Indicators");

		_emaLongLength = Param(nameof(EmaLongLength), 62)
			.SetGreaterThanZero()
			.SetDisplay("EMA Long", "Long EMA length", "Indicators");

		_maLength = Param(nameof(MaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Trend moving average length", "Indicators");

		_maType = Param(nameof(MaType), MovingAverageTypeEnum.Simple)
			.SetDisplay("MA Type", "Trend moving average type", "Indicators");

		_rsiSlowLength = Param(nameof(RsiSlowLength), 28)
			.SetGreaterThanZero()
			.SetDisplay("RSI Slow", "Slow RSI length", "Indicators");

		_rsiFastLength = Param(nameof(RsiFastLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("RSI Fast", "Fast RSI length", "Indicators");

		_rsiOverbought = Param(nameof(RsiOverbought), 70m)
			.SetDisplay("RSI Overbought", "Fast RSI level that closes a long", "Indicators");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "Fast RSI level that closes a short", "Indicators");

		_useStopLoss = Param(nameof(UseStopLoss), true)
			.SetDisplay("Use Stop Loss", "Use the percent stop loss", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
			.SetDisplay("Use Take Profit", "Use the percent take profit", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take profit percentage from entry price", "Risk");

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
			.SetDisplay("Use Trailing Stop", "Use the percent trailing stop", "Risk");

		_trailingPercent = Param(nameof(TrailingPercent), 1.5m)
			.SetNotNegative()
			.SetDisplay("Trailing %", "Trailing stop percentage from the best price", "Risk");

		_useAtrExits = Param(nameof(UseAtrExits), true)
			.SetDisplay("Use ATR Exits", "Use the ATR exit", "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetNotNegative()
			.SetDisplay("ATR Multiplier", "ATR multiplier of the exit distance", "Risk");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR length", "Risk");

		_entryCandleType = Param(nameof(EntryCandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Entry Candles", "Candles for entries", "General");

		_confluenceCandleType = Param(nameof(ConfluenceCandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Confluence Candles", "Higher timeframe candles for confluence", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, EntryCandleType), (Security, ConfluenceCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	private void ResetState()
	{
		_htfTrend = 0;
		_prevVolume = null;
		_entryPrice = 0m;
		_bestPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var emaShort = new ExponentialMovingAverage { Length = EmaShortLength };
		var emaLong = new ExponentialMovingAverage { Length = EmaLongLength };
		var ma = CreateMovingAverage(MaType, MaLength);
		var rsiFast = new RelativeStrengthIndex { Length = RsiFastLength };
		var rsiSlow = new RelativeStrengthIndex { Length = RsiSlowLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var htfEmaShort = new ExponentialMovingAverage { Length = EmaShortLength };
		var htfEmaLong = new ExponentialMovingAverage { Length = EmaLongLength };

		SubscribeCandles(ConfluenceCandleType)
			.Bind(htfEmaShort, htfEmaLong, ProcessConfluence)
			.Start();

		var subscription = SubscribeCandles(EntryCandleType);
		subscription
			.Bind(emaShort, emaLong, ma, rsiFast, rsiSlow, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaShort);
			DrawIndicator(area, emaLong);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private static DecimalLengthIndicator CreateMovingAverage(MovingAverageTypeEnum type, int length)
	{
		return type switch
		{
			MovingAverageTypeEnum.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageTypeEnum.Weighted => new WeightedMovingAverage { Length = length },
			MovingAverageTypeEnum.Smoothed => new SmoothedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	private void ProcessConfluence(ICandleMessage candle, decimal emaShort, decimal emaLong)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_htfTrend = emaShort > emaLong ? 1 : emaShort < emaLong ? -1 : 0;
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaShort, decimal emaLong, decimal ma, decimal rsiFast, decimal rsiSlow, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevVolume = _prevVolume;
		_prevVolume = candle.TotalVolume;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (Position > 0)
		{
			_bestPrice = Math.Max(_bestPrice, candle.HighPrice);

			if (emaShort < emaLong || rsiFast >= RsiOverbought || LongStopHit(candle, atr))
			{
				SellMarket(Position);
				return;
			}
		}
		else if (Position < 0)
		{
			_bestPrice = _bestPrice == 0 ? candle.LowPrice : Math.Min(_bestPrice, candle.LowPrice);

			if (emaShort > emaLong || rsiFast <= RsiOversold || ShortStopHit(candle, atr))
			{
				BuyMarket(-Position);
				return;
			}
		}

		if (prevVolume is not decimal lastVolume)
			return;

		var volumeRising = candle.TotalVolume > lastVolume;

		var longSignal = emaShort > emaLong && close > ma && rsiFast > rsiSlow && rsiFast > _rsiMiddle && volumeRising && _htfTrend > 0;
		var shortSignal = emaShort < emaLong && close < ma && rsiFast < rsiSlow && rsiFast < _rsiMiddle && volumeRising && _htfTrend < 0;

		if (longSignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryPrice = close;
			_bestPrice = close;
		}
		else if (shortSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_entryPrice = close;
			_bestPrice = close;
		}
	}

	private bool LongStopHit(ICandleMessage candle, decimal atr)
	{
		if (_entryPrice <= 0)
			return false;

		if (UseStopLoss && StopLossPercent > 0 && candle.LowPrice <= _entryPrice * (1 - StopLossPercent / 100m))
			return true;

		if (UseTakeProfit && TakeProfitPercent > 0 && candle.HighPrice >= _entryPrice * (1 + TakeProfitPercent / 100m))
			return true;

		if (UseTrailingStop && TrailingPercent > 0 && candle.LowPrice <= _bestPrice * (1 - TrailingPercent / 100m))
			return true;

		return UseAtrExits && AtrMultiplier > 0 && candle.ClosePrice <= _entryPrice - AtrMultiplier * atr;
	}

	private bool ShortStopHit(ICandleMessage candle, decimal atr)
	{
		if (_entryPrice <= 0)
			return false;

		if (UseStopLoss && StopLossPercent > 0 && candle.HighPrice >= _entryPrice * (1 + StopLossPercent / 100m))
			return true;

		if (UseTakeProfit && TakeProfitPercent > 0 && candle.LowPrice <= _entryPrice * (1 - TakeProfitPercent / 100m))
			return true;

		if (UseTrailingStop && TrailingPercent > 0 && candle.HighPrice >= _bestPrice * (1 + TrailingPercent / 100m))
			return true;

		return UseAtrExits && AtrMultiplier > 0 && candle.ClosePrice >= _entryPrice + AtrMultiplier * atr;
	}
}
