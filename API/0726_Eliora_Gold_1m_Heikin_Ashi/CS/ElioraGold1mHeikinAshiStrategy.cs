using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Eliora Gold 1m Heikin Ashi strategy.
/// Trades based on Heikin Ashi candle strength, trend alignment and a cooldown.
/// </summary>
public class ElioraGold1mHeikinAshiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _bodyAtrMultiplier;
	private readonly StrategyParam<decimal> _volatilityMultiplier;
	private readonly StrategyParam<decimal> _trailingMultiplier;

	private AverageTrueRange _atr;
	private SimpleMovingAverage _sma;
	private Lowest _lowest;
	private Highest _highest;

	private decimal _prevHaOpen;
	private decimal _prevHaClose;
	private decimal _prevHaHigh;
	private decimal _prevHaLow;

	private decimal? _highestSinceEntry;
	private decimal? _lowestSinceEntry;

	private int _barIndex;
	private int _lastTradeIndex = int.MinValue;

	/// <summary>
	/// Candle type and timeframe.
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
	/// Cooldown bars after trade.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Body to ATR multiplier for strong candles.
	/// </summary>
	public decimal BodyAtrMultiplier
	{
		get => _bodyAtrMultiplier.Value;
		set => _bodyAtrMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier for volatility filter.
	/// </summary>
	public decimal VolatilityMultiplier
	{
		get => _volatilityMultiplier.Value;
		set => _volatilityMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier for trailing exit.
	/// </summary>
	public decimal TrailingMultiplier
	{
		get => _trailingMultiplier.Value;
		set => _trailingMultiplier.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ElioraGold1mHeikinAshiStrategy"/>.
	/// </summary>
	public ElioraGold1mHeikinAshiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "ATR calculation period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(7, 21, 1);

		_cooldownBars = Param(nameof(CooldownBars), 5)
			.SetDisplay("Cooldown Bars", "Minimum bars between trades", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_bodyAtrMultiplier = Param(nameof(BodyAtrMultiplier), 0.4m)
			.SetDisplay("Body ATR Multiplier", "Multiplier to detect strong candles", "Parameters");

		_volatilityMultiplier = Param(nameof(VolatilityMultiplier), 1.2m)
			.SetDisplay("Volatility Multiplier", "ATR multiplier for volatility filter", "Parameters");

		_trailingMultiplier = Param(nameof(TrailingMultiplier), 0.8m)
			.SetDisplay("Trailing Multiplier", "ATR multiplier for trailing exit", "Parameters");
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
		_prevHaOpen = 0;
		_prevHaClose = 0;
		_prevHaHigh = 0;
		_prevHaLow = 0;
		_highestSinceEntry = null;
		_lowestSinceEntry = null;
		_barIndex = 0;
		_lastTradeIndex = int.MinValue;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange { Length = AtrPeriod };
		_sma = new SimpleMovingAverage { Length = 20 };
		_lowest = new Lowest { Length = 5 };
		_highest = new Highest { Length = 5 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		_barIndex++;

		var atr = atrValue.ToDecimal();

		decimal haOpen, haClose, haHigh, haLow;

		haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;

		if (_prevHaOpen == 0)
		{
			haOpen = (candle.OpenPrice + candle.ClosePrice) / 2m;
			haHigh = candle.HighPrice;
			haLow = candle.LowPrice;
		}
		else
		{
			haOpen = (_prevHaOpen + _prevHaClose) / 2m;
			haHigh = Math.Max(Math.Max(candle.HighPrice, haOpen), haClose);
			haLow = Math.Min(Math.Min(candle.LowPrice, haOpen), haClose);
		}

		var smaValue = _sma.Process(haClose, candle.ServerTime, true);
		if (!smaValue.IsFinal)
		{
			_prevHaOpen = haOpen;
			_prevHaClose = haClose;
			_prevHaHigh = haHigh;
			_prevHaLow = haLow;
			return;
		}

		var trendMa = smaValue.ToDecimal();

		var lowVal = _lowest.Process(haLow, candle.ServerTime, true);
		var highVal = _highest.Process(haHigh, candle.ServerTime, true);

		if (!lowVal.IsFinal || !highVal.IsFinal)
		{
			_prevHaOpen = haOpen;
			_prevHaClose = haClose;
			_prevHaHigh = haHigh;
			_prevHaLow = haLow;
			return;
		}

		var lowest = lowVal.ToDecimal();
		var highest = highVal.ToDecimal();

		var body = Math.Abs(haClose - haOpen);
		var candleStrong = body > atr * BodyAtrMultiplier;

		var inTrendUp = haClose > trendMa;
		var inTrendDown = haClose < trendMa;

		var consolidating = lowest > _prevHaLow && highest < _prevHaHigh;

		var bearishStrong = haOpen > haClose && body > atr * BodyAtrMultiplier;
		var allowShorts = inTrendDown && bearishStrong;

		var canTrade = _barIndex - _lastTradeIndex >= CooldownBars;

		var volatilityThreshold = atr * VolatilityMultiplier;

		var longCondition = canTrade && candleStrong && !consolidating && inTrendUp && atr < volatilityThreshold;
		var shortCondition = canTrade && candleStrong && !consolidating && allowShorts && atr < volatilityThreshold;

		var exitLong = false;
		var exitShort = false;

		if (Position > 0)
		{
			_highestSinceEntry = _highestSinceEntry.HasValue ? Math.Max(_highestSinceEntry.Value, haHigh) : haHigh;
			exitLong = haClose < _highestSinceEntry.Value - atr * TrailingMultiplier;
		}
		else
		{
			_highestSinceEntry = null;
		}

		if (Position < 0)
		{
			_lowestSinceEntry = _lowestSinceEntry.HasValue ? Math.Min(_lowestSinceEntry.Value, haLow) : haLow;
			exitShort = haClose > _lowestSinceEntry.Value + atr * TrailingMultiplier;
		}
		else
		{
			_lowestSinceEntry = null;
		}

		if (longCondition)
		{
			BuyMarket();
			_lastTradeIndex = _barIndex;
			_highestSinceEntry = null;
		}
		else if (shortCondition)
		{
			SellMarket();
			_lastTradeIndex = _barIndex;
			_lowestSinceEntry = null;
		}

		if (exitLong && Position > 0)
		{
			SellMarket(Position);
		}

		if (exitShort && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		_prevHaOpen = haOpen;
		_prevHaClose = haClose;
		_prevHaHigh = haHigh;
		_prevHaLow = haLow;
	}
}