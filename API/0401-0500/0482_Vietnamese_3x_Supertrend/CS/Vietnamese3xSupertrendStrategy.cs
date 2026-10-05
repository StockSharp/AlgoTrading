using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Vietnamese 3x SuperTrend strategy.
/// Three SuperTrends (fast, medium, slow) drive a long-only scaling scheme. While the slow SuperTrend is down the strategy adds up to
/// three entries: Long 1 when the medium trend is up and the fast one down, Long 2 when the medium trend is down and the close is above
/// the fast line, Long 3 when the fast trend is down and the close breaks the highest high of that fast downtrend (or of the last two
/// red candles). Positions close when all trends are up on a bearish candle, when the average entry price is above the close, or by a
/// break-even stop once price has moved above the average entry price.
/// </summary>
public class Vietnamese3xSupertrendStrategy : Strategy
{
	private readonly StrategyParam<int> _fastAtrLength;
	private readonly StrategyParam<decimal> _fastMultiplier;
	private readonly StrategyParam<int> _mediumAtrLength;
	private readonly StrategyParam<decimal> _mediumMultiplier;
	private readonly StrategyParam<int> _slowAtrLength;
	private readonly StrategyParam<decimal> _slowMultiplier;
	private readonly StrategyParam<bool> _useHighestOfTwoRedCandles;
	private readonly StrategyParam<bool> _useEntryStopLoss;
	private readonly StrategyParam<bool> _useAllDowntrendExit;
	private readonly StrategyParam<bool> _useAvgPriceInLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _downtrendHigh;
	private decimal? _lastRedHigh;
	private decimal? _prevRedHigh;
	private bool _long1Done;
	private bool _long2Done;
	private bool _long3Done;
	private int _entryCount;
	private decimal _avgEntryPrice;
	private bool _breakEvenArmed;

	/// <summary>
	/// ATR length of the fast SuperTrend.
	/// </summary>
	public int FastAtrLength
	{
		get => _fastAtrLength.Value;
		set => _fastAtrLength.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the fast SuperTrend.
	/// </summary>
	public decimal FastMultiplier
	{
		get => _fastMultiplier.Value;
		set => _fastMultiplier.Value = value;
	}

	/// <summary>
	/// ATR length of the medium SuperTrend.
	/// </summary>
	public int MediumAtrLength
	{
		get => _mediumAtrLength.Value;
		set => _mediumAtrLength.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the medium SuperTrend.
	/// </summary>
	public decimal MediumMultiplier
	{
		get => _mediumMultiplier.Value;
		set => _mediumMultiplier.Value = value;
	}

	/// <summary>
	/// ATR length of the slow SuperTrend.
	/// </summary>
	public int SlowAtrLength
	{
		get => _slowAtrLength.Value;
		set => _slowAtrLength.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the slow SuperTrend.
	/// </summary>
	public decimal SlowMultiplier
	{
		get => _slowMultiplier.Value;
		set => _slowMultiplier.Value = value;
	}

	/// <summary>
	/// Long 3 breaks the highest high of the last two red candles instead of the fast downtrend high.
	/// </summary>
	public bool UseHighestOfTwoRedCandles
	{
		get => _useHighestOfTwoRedCandles.Value;
		set => _useHighestOfTwoRedCandles.Value = value;
	}

	/// <summary>
	/// Enable the break-even stop.
	/// </summary>
	public bool UseEntryStopLoss
	{
		get => _useEntryStopLoss.Value;
		set => _useEntryStopLoss.Value = value;
	}

	/// <summary>
	/// Exit when all SuperTrends are up and the candle closes bearish.
	/// </summary>
	public bool UseAllDowntrendExit
	{
		get => _useAllDowntrendExit.Value;
		set => _useAllDowntrendExit.Value = value;
	}

	/// <summary>
	/// Exit when the average entry price is above the close.
	/// </summary>
	public bool UseAvgPriceInLoss
	{
		get => _useAvgPriceInLoss.Value;
		set => _useAvgPriceInLoss.Value = value;
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
	public Vietnamese3xSupertrendStrategy()
	{
		_fastAtrLength = Param(nameof(FastAtrLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast ATR Length", "ATR length of the fast SuperTrend", "SuperTrend");

		_fastMultiplier = Param(nameof(FastMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Fast Multiplier", "ATR multiplier of the fast SuperTrend", "SuperTrend");

		_mediumAtrLength = Param(nameof(MediumAtrLength), 11)
			.SetGreaterThanZero()
			.SetDisplay("Medium ATR Length", "ATR length of the medium SuperTrend", "SuperTrend");

		_mediumMultiplier = Param(nameof(MediumMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Medium Multiplier", "ATR multiplier of the medium SuperTrend", "SuperTrend");

		_slowAtrLength = Param(nameof(SlowAtrLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Slow ATR Length", "ATR length of the slow SuperTrend", "SuperTrend");

		_slowMultiplier = Param(nameof(SlowMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Slow Multiplier", "ATR multiplier of the slow SuperTrend", "SuperTrend");

		_useHighestOfTwoRedCandles = Param(nameof(UseHighestOfTwoRedCandles), false)
			.SetDisplay("Highest Of Two Red Candles", "Long 3 breaks the highest high of the last two red candles", "Entries");

		_useEntryStopLoss = Param(nameof(UseEntryStopLoss), true)
			.SetDisplay("Break-Even Stop", "Enable the break-even stop", "Exits");

		_useAllDowntrendExit = Param(nameof(UseAllDowntrendExit), true)
			.SetDisplay("All Trends Exit", "Exit when all SuperTrends are up and the candle closes bearish", "Exits");

		_useAvgPriceInLoss = Param(nameof(UseAvgPriceInLoss), true)
			.SetDisplay("Average Price In Loss", "Exit when the average entry price is above the close", "Exits");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
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
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var fast = new SuperTrend { Length = FastAtrLength, Multiplier = FastMultiplier };
		var medium = new SuperTrend { Length = MediumAtrLength, Multiplier = MediumMultiplier };
		var slow = new SuperTrend { Length = SlowAtrLength, Multiplier = SlowMultiplier };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fast, medium, slow, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, medium);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}

	private void ResetState()
	{
		_downtrendHigh = null;
		_lastRedHigh = null;
		_prevRedHigh = null;
		ResetEntries();
	}

	private void ResetEntries()
	{
		_long1Done = false;
		_long2Done = false;
		_long3Done = false;
		_entryCount = 0;
		_avgEntryPrice = 0;
		_breakEvenArmed = false;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue mediumValue, IIndicatorValue slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!fastValue.IsFormed || !mediumValue.IsFormed || !slowValue.IsFormed)
			return;

		var fast = (SuperTrendIndicatorValue)fastValue;
		var fastUp = fast.IsUpTrend;
		var mediumUp = ((SuperTrendIndicatorValue)mediumValue).IsUpTrend;
		var slowUp = ((SuperTrendIndicatorValue)slowValue).IsUpTrend;
		var fastLine = fast.ToDecimal();
		var close = candle.ClosePrice;
		var bearish = close < candle.OpenPrice;

		// Breakout references come from earlier candles only.
		var breakoutLevel = UseHighestOfTwoRedCandles
			? (_lastRedHigh is decimal a && _prevRedHigh is decimal b ? Math.Max(a, b) : (decimal?)null)
			: _downtrendHigh;

		_downtrendHigh = fastUp ? null : Math.Max(_downtrendHigh ?? candle.HighPrice, candle.HighPrice);

		if (bearish)
		{
			_prevRedHigh = _lastRedHigh;
			_lastRedHigh = candle.HighPrice;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			var exit = false;

			if (UseEntryStopLoss)
			{
				if (_breakEvenArmed && candle.LowPrice <= _avgEntryPrice)
					exit = true;
				else if (candle.LowPrice > _avgEntryPrice)
					_breakEvenArmed = true;
			}

			if (UseAllDowntrendExit && fastUp && mediumUp && slowUp && bearish)
				exit = true;

			if (UseAvgPriceInLoss && _avgEntryPrice > close)
				exit = true;

			if (exit)
			{
				SellMarket(Position);
				ResetEntries();
				return;
			}
		}
		else if (_entryCount > 0)
		{
			ResetEntries();
		}

		if (slowUp)
			return;

		if (!_long1Done && mediumUp && !fastUp)
		{
			_long1Done = true;
			Enter(close);
		}

		if (!_long2Done && !mediumUp && close > fastLine)
		{
			_long2Done = true;
			Enter(close);
		}

		if (!_long3Done && !fastUp && breakoutLevel is decimal level && close > level)
		{
			_long3Done = true;
			Enter(close);
		}
	}

	private void Enter(decimal price)
	{
		BuyMarket(Volume);
		_avgEntryPrice = (_avgEntryPrice * _entryCount + price) / (_entryCount + 1);
		_entryCount++;
	}
}
