using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Chandelier Exit strategy with 200 EMA trend filter.
/// </summary>
public class ChandelierExitWith200EmaFilterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<bool> _useClose;
	private readonly StrategyParam<int> _emaLength;

	private AverageTrueRange _atr;
	private ExponentialMovingAverage _ema;
	private Highest _highestClose;
	private Highest _highestHigh;
	private Lowest _lowestClose;
	private Lowest _lowestLow;

	private bool _initialized;
	private decimal _longStop;
	private decimal _shortStop;
	private decimal? _prevClose;
	private int _dir = 1;

	/// <summary>
	/// Candle type for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop calculation.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Use close price for extremums.
	/// </summary>
	public bool UseClose
	{
		get => _useClose.Value;
		set => _useClose.Value = value;
	}

	/// <summary>
	/// EMA length for trend filter.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public ChandelierExitWith200EmaFilterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 22)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR calculation period", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiplier", "Indicators");

		_useClose = Param(nameof(UseClose), true)
			.SetDisplay("Use Close", "Use close price for extremums", "Calculation");

		_emaLength = Param(nameof(EmaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange { Length = AtrPeriod };
		_ema = new ExponentialMovingAverage { Length = EmaLength };
		_highestClose = new Highest { Length = AtrPeriod };
		_highestHigh = new Highest { Length = AtrPeriod };
		_lowestClose = new Lowest { Length = AtrPeriod };
		_lowestLow = new Lowest { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema, decimal atr)
	{
		var highestCloseVal = _highestClose.Process(candle.ClosePrice);
		var highestHighVal = _highestHigh.Process(candle.HighPrice);
		var lowestCloseVal = _lowestClose.Process(candle.ClosePrice);
		var lowestLowVal = _lowestLow.Process(candle.LowPrice);

		if (!highestCloseVal.IsFinal || !highestHighVal.IsFinal || !lowestCloseVal.IsFinal || !lowestLowVal.IsFinal)
			return;

		if (candle.State != CandleStates.Finished)
			return;

		var highest = UseClose ? highestCloseVal.ToDecimal() : highestHighVal.ToDecimal();
		var lowest = UseClose ? lowestCloseVal.ToDecimal() : lowestLowVal.ToDecimal();

		var longStop = highest - atr * AtrMultiplier;
		var shortStop = lowest + atr * AtrMultiplier;

		if (!_initialized)
		{
			_longStop = longStop;
			_shortStop = shortStop;
			_prevClose = candle.ClosePrice;
			_initialized = true;
			return;
		}

		var longStopPrev = _longStop;
		var shortStopPrev = _shortStop;

		if (_prevClose > longStopPrev)
			longStop = Math.Max(longStop, longStopPrev);

		if (_prevClose < shortStopPrev)
			shortStop = Math.Min(shortStop, shortStopPrev);

		var prevDir = _dir;
		if (candle.ClosePrice > shortStopPrev)
			_dir = 1;
		else if (candle.ClosePrice < longStopPrev)
			_dir = -1;

		var buySignal = _dir == 1 && prevDir == -1;
		var sellSignal = _dir == -1 && prevDir == 1;

		_longStop = longStop;
		_shortStop = shortStop;
		_prevClose = candle.ClosePrice;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (buySignal)
		{
			if (ema < candle.ClosePrice)
			{
				BuyMarket(Volume + Math.Max(-Position, 0));
			}
			else if (Position < 0)
			{
				BuyMarket(-Position);
			}
		}
		else if (sellSignal)
		{
			if (ema > candle.ClosePrice)
			{
				SellMarket(Volume + Math.Max(Position, 0));
			}
			else if (Position > 0)
			{
				SellMarket(Position);
			}
		}
	}
}
