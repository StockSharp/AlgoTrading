namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that recreates the Galactic Explosion grid behavior using a moving average bias and distance based scaling.
/// </summary>
public class GalacticExplosionStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<decimal> _minimalProfit;
	private readonly StrategyParam<decimal> _indentAfterEighth;
	private readonly StrategyParam<decimal> _skipThreeCandlesMin;
	private readonly StrategyParam<decimal> _skipThreeCandlesMax;
	private readonly StrategyParam<decimal> _skipSixCandlesMax;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _movingAverage;

	private int _longEntries;
	private int _shortEntries;
	private decimal _firstLongPrice;
	private decimal _lastLongPrice;
	private decimal _firstShortPrice;
	private decimal _lastShortPrice;
	private int _missedBarsLong;
	private int _missedBarsShort;
	private decimal _longPositionVolume;
	private decimal _shortPositionVolume;
	private decimal? _longAveragePrice;
	private decimal? _shortAveragePrice;
	private bool _invalidHoursLogged;

	/// <summary>
	/// Order volume used for every new entry.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Trading window start hour in 24h format.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Trading window end hour in 24h format.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Profit threshold combining realized and open PnL at which all positions are closed.
	/// </summary>
	public decimal MinimalProfit
	{
		get => _minimalProfit.Value;
		set => _minimalProfit.Value = value;
	}

	/// <summary>
	/// Minimum distance from the most recent entry (expressed in price steps) required after the eighth trade.
	/// </summary>
	public decimal IndentAfterEighth
	{
		get => _indentAfterEighth.Value;
		set => _indentAfterEighth.Value = value;
	}

	/// <summary>
	/// Minimum distance from the first entry to trigger the skip three candles logic (in price steps).
	/// </summary>
	public decimal SkipThreeCandlesMin
	{
		get => _skipThreeCandlesMin.Value;
		set => _skipThreeCandlesMin.Value = value;
	}

	/// <summary>
	/// Maximum distance from the first entry that still keeps the skip three candles logic active (in price steps).
	/// </summary>
	public decimal SkipThreeCandlesMax
	{
		get => _skipThreeCandlesMax.Value;
		set => _skipThreeCandlesMax.Value = value;
	}

	/// <summary>
	/// Maximum distance from the first entry that keeps the skip six candles logic active (in price steps).
	/// </summary>
	public decimal SkipSixCandlesMax
	{
		get => _skipSixCandlesMax.Value;
		set => _skipSixCandlesMax.Value = value;
	}

	/// <summary>
	/// Length of the moving average filter.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="GalacticExplosionStrategy"/>.
	/// </summary>
	public GalacticExplosionStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Volume for each new entry", "Trading");

		_startHour = Param(nameof(StartHour), 8)
		.SetDisplay("Start Hour", "Trading session start hour", "Trading");

		_endHour = Param(nameof(EndHour), 17)
		.SetDisplay("End Hour", "Trading session end hour", "Trading");

		_minimalProfit = Param(nameof(MinimalProfit), 1m)
		.SetDisplay("Minimal Profit", "Target profit to close the grid", "Risk");

		_indentAfterEighth = Param(nameof(IndentAfterEighth), 10m)
		.SetDisplay("Indent After 8th", "Distance from last entry after eight trades (price steps)", "Grid");

		_skipThreeCandlesMin = Param(nameof(SkipThreeCandlesMin), 500m)
		.SetDisplay("Skip 3 Min", "Lower distance to start skipping three candles", "Grid");

		_skipThreeCandlesMax = Param(nameof(SkipThreeCandlesMax), 999m)
		.SetDisplay("Skip 3 Max", "Upper distance to keep skipping three candles", "Grid");

		_skipSixCandlesMax = Param(nameof(SkipSixCandlesMax), 2000m)
		.SetDisplay("Skip 6 Max", "Upper distance to keep skipping six candles", "Grid");

		_maLength = Param(nameof(MaLength), 200)
		.SetGreaterThanZero()
		.SetDisplay("MA Length", "Length of the moving average", "Filter");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle series", "General");
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

		_longEntries = 0;
		_shortEntries = 0;
		_firstLongPrice = 0m;
		_lastLongPrice = 0m;
		_firstShortPrice = 0m;
		_lastShortPrice = 0m;
		_missedBarsLong = 0;
		_missedBarsShort = 0;
		_longPositionVolume = 0m;
		_shortPositionVolume = 0m;
		_longAveragePrice = null;
		_shortAveragePrice = null;
		_invalidHoursLogged = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_movingAverage = new SimpleMovingAverage
		{
			Length = MaLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_movingAverage, ProcessCandle)
		.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!_movingAverage.IsFormed)
		return;

		var totalProfit = PnL + GetOpenProfit(candle.ClosePrice);
		if (MinimalProfit > 0m && totalProfit >= MinimalProfit && Position != 0m)
		{
			ClosePosition();
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!IsWithinTradingWindow(candle.OpenTime))
		return;

		var close = candle.ClosePrice;
		var needBuy = close < maValue;
		var needSell = close > maValue;

		var entries = GetCurrentEntries();

		if (entries <= 8)
		{
			if (needBuy)
			{
				EnterLong();
			}
			else if (needSell)
			{
				EnterShort();
			}

			return;
		}

		var priceStep = GetPriceStep();
		var indentAfterEighth = priceStep * IndentAfterEighth;
		var skipThreeMin = priceStep * SkipThreeCandlesMin;
		var skipThreeMax = priceStep * SkipThreeCandlesMax;
		var skipSixMax = priceStep * SkipSixCandlesMax;

		if (Position > 0m)
		{
			ProcessLongGrid(close, needBuy, indentAfterEighth, skipThreeMin, skipThreeMax, skipSixMax);
		}
		else if (Position < 0m)
		{
			ProcessShortGrid(close, needSell, indentAfterEighth, skipThreeMin, skipThreeMax, skipSixMax);
		}
	}

	private void ProcessLongGrid(decimal price, bool needBuy, decimal indentAfterEighth, decimal skipThreeMin, decimal skipThreeMax, decimal skipSixMax)
	{
		if (_lastLongPrice <= 0m || _firstLongPrice <= 0m)
		return;

		var lastDistance = Math.Abs(price - _lastLongPrice);
		if (lastDistance <= indentAfterEighth)
		return;

		var firstDistance = Math.Abs(price - _firstLongPrice);

		if (firstDistance < skipThreeMin)
		{
			_missedBarsLong = 0;

			if (needBuy)
			EnterLong();
		}
		else if (firstDistance <= skipThreeMax)
		{
			_missedBarsLong++;

			if (_missedBarsLong > 3)
			{
				if (needBuy)
				EnterLong();

				_missedBarsLong = 0;
			}
		}
		else if (firstDistance <= skipSixMax)
		{
			_missedBarsLong++;

			if (_missedBarsLong > 6)
			{
				if (needBuy)
				EnterLong();

				_missedBarsLong = 0;
			}
		}
	}

	private void ProcessShortGrid(decimal price, bool needSell, decimal indentAfterEighth, decimal skipThreeMin, decimal skipThreeMax, decimal skipSixMax)
	{
		if (_lastShortPrice <= 0m || _firstShortPrice <= 0m)
		return;

		var lastDistance = Math.Abs(price - _lastShortPrice);
		if (lastDistance <= indentAfterEighth)
		return;

		var firstDistance = Math.Abs(price - _firstShortPrice);

		if (firstDistance < skipThreeMin)
		{
			_missedBarsShort = 0;

			if (needSell)
			EnterShort();
		}
		else if (firstDistance <= skipThreeMax)
		{
			_missedBarsShort++;

			if (_missedBarsShort > 3)
			{
				if (needSell)
				EnterShort();

				_missedBarsShort = 0;
			}
		}
		else if (firstDistance <= skipSixMax)
		{
			_missedBarsShort++;

			if (_missedBarsShort > 6)
			{
				if (needSell)
				EnterShort();

				_missedBarsShort = 0;
			}
		}
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Trade == null)
		return;

		if (trade.Trade.Security != Security)
		return;

		var price = trade.Trade.Price;
		var volume = trade.Trade.Volume;

		if (volume <= 0m)
		return;

		if (trade.Order.Direction == Sides.Buy)
		{
			HandleBuyTrade(volume, price);
		}
		else if (trade.Order.Direction == Sides.Sell)
		{
			HandleSellTrade(volume, price);
		}

		if (Position == 0m)
		{
			ResetLongState();
			ResetShortState();
		}
	}

	private void HandleBuyTrade(decimal volume, decimal price)
	{
		if (_shortPositionVolume > 0m)
		{
			var closingVolume = Math.Min(volume, _shortPositionVolume);
			_shortPositionVolume -= closingVolume;
			ReduceShortEntries(closingVolume);

			if (_shortPositionVolume <= 0m)
			{
				ResetShortState();
			}

			var remaining = volume - closingVolume;
			if (remaining > 0m)
			{
				AddLong(remaining, price);
			}
		}
		else
		{
			AddLong(volume, price);
		}
	}

	private void HandleSellTrade(decimal volume, decimal price)
	{
		if (_longPositionVolume > 0m)
		{
			var closingVolume = Math.Min(volume, _longPositionVolume);
			_longPositionVolume -= closingVolume;
			ReduceLongEntries(closingVolume);

			if (_longPositionVolume <= 0m)
			{
				ResetLongState();
			}

			var remaining = volume - closingVolume;
			if (remaining > 0m)
			{
				AddShort(remaining, price);
			}
		}
		else
		{
			AddShort(volume, price);
		}
	}

	private void AddLong(decimal volume, decimal price)
	{
		if (volume <= 0m)
		return;

		var previousVolume = _longPositionVolume;
		var newVolume = previousVolume + volume;

		if (newVolume <= 0m)
		return;

		if (previousVolume <= 0m)
		{
			_firstLongPrice = price;
			_missedBarsLong = 0;
		}

		_longEntries += GetEntryCountFromVolume(volume);
		_lastLongPrice = price;
		_longPositionVolume = newVolume;

		if (_longAveragePrice is decimal avg && previousVolume > 0m)
		{
			_longAveragePrice = ((avg * previousVolume) + (price * volume)) / newVolume;
		}
		else
		{
			_longAveragePrice = price;
		}
	}

	private void AddShort(decimal volume, decimal price)
	{
		if (volume <= 0m)
		return;

		var previousVolume = _shortPositionVolume;
		var newVolume = previousVolume + volume;

		if (newVolume <= 0m)
		return;

		if (previousVolume <= 0m)
		{
			_firstShortPrice = price;
			_missedBarsShort = 0;
		}

		_shortEntries += GetEntryCountFromVolume(volume);
		_lastShortPrice = price;
		_shortPositionVolume = newVolume;

		if (_shortAveragePrice is decimal avg && previousVolume > 0m)
		{
			_shortAveragePrice = ((avg * previousVolume) + (price * volume)) / newVolume;
		}
		else
		{
			_shortAveragePrice = price;
		}
	}

	private void ReduceLongEntries(decimal volume)
	{
		if (volume <= 0m || _longEntries <= 0)
		return;

		_longEntries = Math.Max(0, _longEntries - GetEntryCountFromVolume(volume));
	}

	private void ReduceShortEntries(decimal volume)
	{
		if (volume <= 0m || _shortEntries <= 0)
		return;

		_shortEntries = Math.Max(0, _shortEntries - GetEntryCountFromVolume(volume));
	}

	private void ResetLongState()
	{
		_longEntries = 0;
		_firstLongPrice = 0m;
		_lastLongPrice = 0m;
		_missedBarsLong = 0;
		_longPositionVolume = 0m;
		_longAveragePrice = null;
	}

	private void ResetShortState()
	{
		_shortEntries = 0;
		_firstShortPrice = 0m;
		_lastShortPrice = 0m;
		_missedBarsShort = 0;
		_shortPositionVolume = 0m;
		_shortAveragePrice = null;
	}

	private void EnterLong()
	{
		if (OrderVolume <= 0m)
		return;

		var volume = OrderVolume;

		if (Position < 0m)
		volume += Math.Abs(Position);

		if (volume > 0m)
		BuyMarket(volume);
	}

	private void EnterShort()
	{
		if (OrderVolume <= 0m)
		return;

		var volume = OrderVolume;

		if (Position > 0m)
		volume += Math.Abs(Position);

		if (volume > 0m)
		SellMarket(volume);
	}

	private decimal GetOpenProfit(decimal price)
	{
		if (Position > 0m && _longAveragePrice is decimal longAvg)
		return Position * (price - longAvg);

		if (Position < 0m && _shortAveragePrice is decimal shortAvg)
		return Math.Abs(Position) * (shortAvg - price);

		return 0m;
	}

	private int GetCurrentEntries()
	{
		if (Position > 0m)
		return _longEntries;

		if (Position < 0m)
		return _shortEntries;

		return 0;
	}

	private int GetEntryCountFromVolume(decimal volume)
	{
		if (volume <= 0m)
		return 0;

		if (OrderVolume <= 0m)
		return 1;

		var ratio = volume / OrderVolume;
		if (ratio <= 0m)
		return 0;

		var count = (int)Math.Round(ratio, MidpointRounding.AwayFromZero);
		return Math.Max(1, count);
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep;
		return step is > 0m ? step.Value : 1m;
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var start = StartHour;
		var end = EndHour;

		if (start < 0 || start > 23 || end < 0 || end > 23 || start >= end)
		{
			if (!_invalidHoursLogged)
			{
				LogWarning($"Invalid trading hours configuration. Start={start}, End={end}.");
				_invalidHoursLogged = true;
			}

			return false;
		}

		_invalidHoursLogged = false;

		var hour = time.Hour;
		return hour >= start && hour < end;
	}
}
