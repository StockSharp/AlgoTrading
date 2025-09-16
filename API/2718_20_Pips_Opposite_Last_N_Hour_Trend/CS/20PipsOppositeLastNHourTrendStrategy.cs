using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades against the last N hours trend with a fixed take profit.
/// </summary>
public class TwentyPipsOppositeLastNHourTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<int> _tradingHour;
	private readonly StrategyParam<int> _hoursToCheckTrend;
	private readonly StrategyParam<int> _firstMultiplier;
	private readonly StrategyParam<int> _secondMultiplier;
	private readonly StrategyParam<int> _thirdMultiplier;
	private readonly StrategyParam<int> _fourthMultiplier;
	private readonly StrategyParam<int> _fifthMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closeHistory = new();

	private decimal? _entryPrice;
	private decimal? _takeProfitLevel;
	private decimal _entryVolume;
	private int _positionDirection;
	private int _consecutiveLosses;
	private DateTime? _currentDay;
	private int _tradesToday;

	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public int TradingHour
	{
		get => _tradingHour.Value;
		set => _tradingHour.Value = value;
	}

	public int HoursToCheckTrend
	{
		get => _hoursToCheckTrend.Value;
		set => _hoursToCheckTrend.Value = value;
	}

	public int FirstMultiplier
	{
		get => _firstMultiplier.Value;
		set => _firstMultiplier.Value = value;
	}

	public int SecondMultiplier
	{
		get => _secondMultiplier.Value;
		set => _secondMultiplier.Value = value;
	}

	public int ThirdMultiplier
	{
		get => _thirdMultiplier.Value;
		set => _thirdMultiplier.Value = value;
	}

	public int FourthMultiplier
	{
		get => _fourthMultiplier.Value;
		set => _fourthMultiplier.Value = value;
	}

	public int FifthMultiplier
	{
		get => _fifthMultiplier.Value;
		set => _fifthMultiplier.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public TwentyPipsOppositeLastNHourTrendStrategy()
	{
		_maxPositions = Param(nameof(MaxPositions), 9)
			.SetGreaterThanZero()
			.SetDisplay("Max Positions", "Maximum trades per day", "Trading");

		_volume = Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Base order volume", "Trading");

		_maxVolume = Param(nameof(MaxVolume), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Max Volume", "Maximum allowed volume", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Trading");

		_tradingHour = Param(nameof(TradingHour), 7)
			.SetRange(0, 23)
			.SetDisplay("Trading Hour", "Hour (0-23) when entries are allowed", "Timing");

		_hoursToCheckTrend = Param(nameof(HoursToCheckTrend), 24)
			.SetRange(2, 240)
			.SetDisplay("Hours To Check", "Lookback hours for trend calculation", "Signals");

		_firstMultiplier = Param(nameof(FirstMultiplier), 2)
			.SetGreaterThanZero()
			.SetDisplay("First Multiplier", "Multiplier after first loss", "Money Management");

		_secondMultiplier = Param(nameof(SecondMultiplier), 4)
			.SetGreaterThanZero()
			.SetDisplay("Second Multiplier", "Multiplier after second loss", "Money Management");

		_thirdMultiplier = Param(nameof(ThirdMultiplier), 8)
			.SetGreaterThanZero()
			.SetDisplay("Third Multiplier", "Multiplier after third loss", "Money Management");

		_fourthMultiplier = Param(nameof(FourthMultiplier), 16)
			.SetGreaterThanZero()
			.SetDisplay("Fourth Multiplier", "Multiplier after fourth loss", "Money Management");

		_fifthMultiplier = Param(nameof(FifthMultiplier), 32)
			.SetGreaterThanZero()
			.SetDisplay("Fifth Multiplier", "Multiplier after fifth loss", "Money Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe to process", "Market Data");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_closeHistory.Clear();
		_entryPrice = null;
		_takeProfitLevel = null;
		_entryVolume = 0m;
		_positionDirection = 0;
		_consecutiveLosses = 0;
		_currentDay = null;
		_tradesToday = 0;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var candleDay = candle.OpenTime.Date;
		if (_currentDay != candleDay)
		{
			_currentDay = candleDay;
			_tradesToday = 0;
		}

		if (_positionDirection != 0)
		{
			if (_takeProfitLevel is decimal target)
			{
				// Take profit when the candle range touches the desired level.
				var hitTarget = _positionDirection > 0
					? candle.HighPrice >= target
					: candle.LowPrice <= target;

				if (hitTarget)
				{
					ClosePosition(target);
				}
			}

			if (_positionDirection != 0 && candle.OpenTime.Hour != TradingHour)
			{
				// Close remaining exposure when the configured session hour has passed.
				ClosePosition(candle.ClosePrice);
			}
		}

		if (_positionDirection != 0)
		{
			UpdateHistory(candle.ClosePrice);
			return;
		}

		if (candle.OpenTime.Hour != TradingHour)
		{
			UpdateHistory(candle.ClosePrice);
			return;
		}

		if (MaxPositions <= 0 || _tradesToday >= MaxPositions)
		{
			UpdateHistory(candle.ClosePrice);
			return;
		}

		var requiredHistory = Math.Max(HoursToCheckTrend, 2);
		if (_closeHistory.Count < requiredHistory)
		{
			UpdateHistory(candle.ClosePrice);
			return;
		}

		var referenceClose = _closeHistory[_closeHistory.Count - HoursToCheckTrend];
		var previousClose = _closeHistory[_closeHistory.Count - 1];

		if (previousClose == referenceClose)
		{
			UpdateHistory(candle.ClosePrice);
			return;
		}

		// Opposite trend logic: buy after bearish drift, sell after bullish drift.
		var goLong = previousClose < referenceClose;
		var orderVolume = CalculateOrderVolume();
		if (orderVolume <= 0)
		{
			UpdateHistory(candle.ClosePrice);
			return;
		}

		if (goLong)
		{
			BuyMarket(orderVolume);
			_positionDirection = 1;
		}
		else
		{
			SellMarket(orderVolume);
			_positionDirection = -1;
		}

		_entryPrice = candle.ClosePrice;
		_entryVolume = orderVolume;

		var distance = GetTakeProfitDistance();

		if (distance > 0m)
		{
			_takeProfitLevel = _positionDirection > 0
				? _entryPrice + distance
				: _entryPrice - distance;
		}
		else
		{
			_takeProfitLevel = null;
		}

		_tradesToday++;

		UpdateHistory(candle.ClosePrice);
	}

	private void ClosePosition(decimal exitPrice)
	{
		var direction = _positionDirection;
		var entryPrice = _entryPrice;
		var volume = Math.Abs(Position);

		if (volume <= 0m && _entryVolume > 0m)
		{
			volume = _entryVolume;
		}

		if (volume <= 0m)
		{
			_positionDirection = 0;
			_takeProfitLevel = null;
			_entryPrice = null;
			_entryVolume = 0m;
			return;
		}

		if (direction > 0)
		{
			SellMarket(volume);
		}
		else if (direction < 0)
		{
			BuyMarket(volume);
		}

		if (entryPrice is decimal price)
		{
			var isLoss = direction > 0
				? exitPrice < price
				: exitPrice > price;

			_consecutiveLosses = isLoss
				? Math.Min(_consecutiveLosses + 1, 5)
				: 0;
		}

		_positionDirection = 0;
		_takeProfitLevel = null;
		_entryPrice = null;
		_entryVolume = 0m;
	}

	private void UpdateHistory(decimal closePrice)
	{
		_closeHistory.Add(closePrice);

		var maxHistory = Math.Max(HoursToCheckTrend, 2);
		if (_closeHistory.Count > maxHistory)
		{
			_closeHistory.RemoveRange(0, _closeHistory.Count - maxHistory);
		}
	}

	private decimal CalculateOrderVolume()
	{
		if (Volume <= 0m)
		{
			return 0m;
		}

		var multiplier = _consecutiveLosses switch
		{
			>= 5 => (decimal)FifthMultiplier,
			4 => (decimal)FourthMultiplier,
			3 => (decimal)ThirdMultiplier,
			2 => (decimal)SecondMultiplier,
			1 => (decimal)FirstMultiplier,
			_ => 1m
		};

		var desiredVolume = Volume * multiplier;

		if (MaxVolume > 0m && desiredVolume > MaxVolume)
		{
			desiredVolume = MaxVolume;
		}

		return desiredVolume;
	}

	private decimal GetTakeProfitDistance()
	{
		var pipSize = GetPipSize();
		return pipSize > 0m
			? TakeProfitPips * pipSize
			: 0m;
	}

	private decimal GetPipSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		{
			priceStep = 0.0001m;
		}

		var decimals = Security?.Decimals ?? 0;
		if (decimals == 3 || decimals == 5)
		{
			return priceStep * 10m;
		}

		return priceStep;
	}
}
