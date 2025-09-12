namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

public enum LineSelection
{
	Filter,
	Upper,
	Lower
}

public enum CrossDirection
{
	CrossUp,
	CrossDown
}

/// <summary>
/// Gaussian Channel Strategy
/// </summary>
public class GaussianChannelStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<LineSelection> _longLine;
	private readonly StrategyParam<LineSelection> _shortLine;
	private readonly StrategyParam<CrossDirection> _longDirection;
	private readonly StrategyParam<CrossDirection> _shortDirection;
	private readonly StrategyParam<bool> _tradeLong;
	private readonly StrategyParam<bool> _tradeShort;
	private readonly StrategyParam<bool> _reverseOnOpp;
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<DateTimeOffset> _startDate;

	private ExponentialMovingAverage _ema;
	private AverageTrueRange _atr;

	private decimal? _prevPrice;
	private decimal? _prevLongLine;
	private decimal? _prevShortLine;

	private int _barsSinceLongUp = int.MaxValue;
	private int _barsSinceLongDown = int.MaxValue;
	private int _barsSinceShortUp = int.MaxValue;
	private int _barsSinceShortDown = int.MaxValue;

	public GaussianChannelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_period = Param(nameof(Period), 144)
		.SetDisplay("Period", "Smoothing period", "Channel");
		_multiplier = Param(nameof(Multiplier), 1.414m)
		.SetDisplay("ATR Mult", "ATR multiplier", "Channel");

		_longLine = Param(nameof(LongLine), LineSelection.Filter)
		.SetDisplay("Long line", "Line used for long entries", "Signals");
		_shortLine = Param(nameof(ShortLine), LineSelection.Filter)
		.SetDisplay("Short line", "Line used for short entries", "Signals");

		_longDirection = Param(nameof(LongDirection), CrossDirection.CrossUp)
		.SetDisplay("Long direction", "Price cross direction for long entry", "Signals");
		_shortDirection = Param(nameof(ShortDirection), CrossDirection.CrossDown)
		.SetDisplay("Short direction", "Price cross direction for short entry", "Signals");

		_tradeLong = Param(nameof(TradeLong), true)
		.SetDisplay("Enable long", "Allow long trades", "Trade");
		_tradeShort = Param(nameof(TradeShort), true)
		.SetDisplay("Enable short", "Allow short trades", "Trade");
		_reverseOnOpp = Param(nameof(ReverseOnOpp), true)
		.SetDisplay("Reverse on opposite", "Reverse position on opposite signal", "Trade");
		_lookback = Param(nameof(Lookback), 3)
		.SetDisplay("Lookback", "Bars for late entry", "Trade");
		_startDate = Param(nameof(StartDate), DateTimeOffset.MinValue)
		.SetDisplay("Start date", "Start trading date", "Trade");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	public LineSelection LongLine
	{
		get => _longLine.Value;
		set => _longLine.Value = value;
	}

	public LineSelection ShortLine
	{
		get => _shortLine.Value;
		set => _shortLine.Value = value;
	}

	public CrossDirection LongDirection
	{
		get => _longDirection.Value;
		set => _longDirection.Value = value;
	}

	public CrossDirection ShortDirection
	{
		get => _shortDirection.Value;
		set => _shortDirection.Value = value;
	}

	public bool TradeLong
	{
		get => _tradeLong.Value;
		set => _tradeLong.Value = value;
	}

	public bool TradeShort
	{
		get => _tradeShort.Value;
		set => _tradeShort.Value = value;
	}

	public bool ReverseOnOpp
	{
		get => _reverseOnOpp.Value;
		set => _reverseOnOpp.Value = value;
	}

	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	public DateTimeOffset StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema = new ExponentialMovingAverage { Length = Period };
		_atr = new AverageTrueRange { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_ema, _atr, ProcessCandle).Start();

		StartProtection();
	}

	private static decimal SelectLine(LineSelection type, decimal mid, decimal upper, decimal lower)
	{
		return type switch
		{
			LineSelection.Upper => upper,
			LineSelection.Lower => lower,
			_ => mid,
		};
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (candle.OpenTime < StartDate)
			return;

		if (!_ema.IsFormed || !_atr.IsFormed)
			return;

		var upper = emaValue + atrValue * Multiplier;
		var lower = emaValue - atrValue * Multiplier;
		var price = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;

		var longLine = SelectLine(LongLine, emaValue, upper, lower);
		var shortLine = SelectLine(ShortLine, emaValue, upper, lower);

		var longUp = _prevPrice < _prevLongLine && price > longLine;
		var longDown = _prevPrice > _prevLongLine && price < longLine;
		var shortUp = _prevPrice < _prevShortLine && price > shortLine;
		var shortDown = _prevPrice > _prevShortLine && price < shortLine;

		if (longUp)
			_barsSinceLongUp = 0;
		else if (_barsSinceLongUp != int.MaxValue)
			_barsSinceLongUp++;

		if (longDown)
			_barsSinceLongDown = 0;
		else if (_barsSinceLongDown != int.MaxValue)
			_barsSinceLongDown++;

		if (shortUp)
			_barsSinceShortUp = 0;
		else if (_barsSinceShortUp != int.MaxValue)
			_barsSinceShortUp++;

		if (shortDown)
			_barsSinceShortDown = 0;
		else if (_barsSinceShortDown != int.MaxValue)
			_barsSinceShortDown++;

		var longCond = LongDirection == CrossDirection.CrossUp
		? longUp || (price > longLine && _barsSinceLongUp <= Lookback)
		: longDown || (price < longLine && _barsSinceLongDown <= Lookback);

		var shortCond = ShortDirection == CrossDirection.CrossUp
		? shortUp || (price > shortLine && _barsSinceShortUp <= Lookback)
		: shortDown || (price < shortLine && _barsSinceShortDown <= Lookback);

		if (longCond)
		{
			if (Position < 0)
				ClosePosition();

			if (TradeLong && (Position == 0 || (ReverseOnOpp && Position <= 0)))
				BuyMarket();
		}

		if (shortCond)
		{
			if (Position > 0)
				ClosePosition();

			if (TradeShort && (Position == 0 || (ReverseOnOpp && Position >= 0)))
				SellMarket();
		}

		_prevPrice = price;
		_prevLongLine = longLine;
		_prevShortLine = shortLine;
	}
}
