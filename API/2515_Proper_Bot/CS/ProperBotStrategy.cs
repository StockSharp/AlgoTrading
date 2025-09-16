namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Globalization;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Grid strategy converted from the "Proper Bot" MQL expert advisor.
/// </summary>
public class ProperBotStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _midPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<bool> _disableMaFilter;
	private readonly StrategyParam<int> _volumePeriod;
	private readonly StrategyParam<decimal> _volumeMinimum;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<decimal> _firstVolume;
	private readonly StrategyParam<string> _gridMap;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _trailStartPoints;
	private readonly StrategyParam<int> _trailDistancePoints;
	private readonly StrategyParam<int> _trailStepPoints;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _finishHour;
	private readonly StrategyParam<int> _finishMinute;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<GridLevel> _gridLevels = new();
	private readonly List<GridOrder> _activeOrders = new();

	private readonly EMA _fastEma = new();
	private readonly EMA _midEma = new();
	private readonly EMA _slowEma = new();
	private readonly SimpleMovingAverage _volumeAverage = new();

	private decimal _priceStep;
	private Sides? _currentDirection;
	private int _nextGridIndex;
	private decimal _lastEntryPrice;
	private decimal _maxTrailingPoints;
	private bool _hasPreviousCandle;
	private decimal _previousOpen;
	private decimal _previousClose;

	public int FastMaPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int MidMaPeriod
	{
		get => _midPeriod.Value;
		set => _midPeriod.Value = value;
	}

	public int SlowMaPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public bool DisableMaFilter
	{
		get => _disableMaFilter.Value;
		set => _disableMaFilter.Value = value;
	}

	public int VolumePeriod
	{
		get => _volumePeriod.Value;
		set => _volumePeriod.Value = value;
	}

	public decimal VolumeMinimum
	{
		get => _volumeMinimum.Value;
		set => _volumeMinimum.Value = value;
	}

	public decimal HighLevel
	{
		get => _highLevel.Value;
		set => _highLevel.Value = value;
	}

	public decimal LowLevel
	{
		get => _lowLevel.Value;
		set => _lowLevel.Value = value;
	}

	public decimal FirstVolume
	{
		get => _firstVolume.Value;
		set => _firstVolume.Value = value;
	}

	public string GridMap
	{
		get => _gridMap.Value;
		set => _gridMap.Value = value;
	}

	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public int TrailStartPoints
	{
		get => _trailStartPoints.Value;
		set => _trailStartPoints.Value = value;
	}

	public int TrailDistancePoints
	{
		get => _trailDistancePoints.Value;
		set => _trailDistancePoints.Value = value;
	}

	public int TrailStepPoints
	{
		get => _trailStepPoints.Value;
		set => _trailStepPoints.Value = value;
	}

	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	public int StartMinute
	{
		get => _startMinute.Value;
		set => _startMinute.Value = value;
	}

	public int FinishHour
	{
		get => _finishHour.Value;
		set => _finishHour.Value = value;
	}

	public int FinishMinute
	{
		get => _finishMinute.Value;
		set => _finishMinute.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public ProperBotStrategy()
	{
		_fastPeriod = Param(nameof(FastMaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Period of the fast EMA filter", "Signals")
			.SetCanOptimize();

		_midPeriod = Param(nameof(MidMaPeriod), 25)
			.SetDisplay("Mid EMA", "Optional middle EMA period", "Signals")
			.SetCanOptimize();

		_slowPeriod = Param(nameof(SlowMaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Period of the slow EMA filter", "Signals")
			.SetCanOptimize();

		_disableMaFilter = Param(nameof(DisableMaFilter), true)
			.SetDisplay("Disable EMA Filter", "Use previous candle direction instead of EMAs", "Signals")
			.SetCanOptimize();

		_volumePeriod = Param(nameof(VolumePeriod), 1)
			.SetDisplay("Volume Period", "Number of candles for the volume filter", "Filters")
			.SetCanOptimize();

		_volumeMinimum = Param(nameof(VolumeMinimum), 69m)
			.SetDisplay("Volume Minimum", "Minimal average volume to allow entries", "Filters")
			.SetCanOptimize();

		_highLevel = Param(nameof(HighLevel), 1.50001m)
			.SetDisplay("High Level", "Do not buy above this price", "Filters")
			.SetCanOptimize();

		_lowLevel = Param(nameof(LowLevel), 1.40001m)
			.SetDisplay("Low Level", "Do not sell below this price", "Filters")
			.SetCanOptimize();

		_firstVolume = Param(nameof(FirstVolume), 0.08m)
			.SetDisplay("First Order Volume", "Volume for the first order in a grid cycle", "Risk")
			.SetCanOptimize();

		_gridMap = Param(nameof(GridMap), "120/0.1 120/0.11 120/0.12 120/0.13 120/0.14 120/0.15 120/0.16 120/0.17 120/0.18 120/0.19")
			.SetDisplay("Grid Map", "Distance/volume pairs separated by spaces", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 10000)
			.SetDisplay("Take Profit Points", "Profit distance in price steps", "Risk")
			.SetCanOptimize();

		_stopLossPoints = Param(nameof(StopLossPoints), 30000)
			.SetDisplay("Stop Loss Points", "Loss distance in price steps", "Risk")
			.SetCanOptimize();

		_trailStartPoints = Param(nameof(TrailStartPoints), 52)
			.SetDisplay("Trail Start Points", "Minimal profit to arm the trailing exit", "Risk")
			.SetCanOptimize();

		_trailDistancePoints = Param(nameof(TrailDistancePoints), 52)
			.SetDisplay("Trail Distance Points", "Profit distance required to enable trailing", "Risk")
			.SetCanOptimize();

		_trailStepPoints = Param(nameof(TrailStepPoints), 2)
			.SetDisplay("Trail Step Points", "Allowed profit retracement before exit", "Risk")
			.SetCanOptimize();

		_startHour = Param(nameof(StartHour), 6)
			.SetDisplay("Start Hour", "Trading session start hour", "Session")
			.SetCanOptimize();

		_startMinute = Param(nameof(StartMinute), 0)
			.SetDisplay("Start Minute", "Trading session start minute", "Session")
			.SetCanOptimize();

		_finishHour = Param(nameof(FinishHour), 21)
			.SetDisplay("Finish Hour", "Trading session end hour", "Session")
			.SetCanOptimize();

		_finishMinute = Param(nameof(FinishMinute), 0)
			.SetDisplay("Finish Minute", "Trading session end minute", "Session")
			.SetCanOptimize();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();

		_gridLevels.Clear();
		_activeOrders.Clear();
		_fastEma.Reset();
		_midEma.Reset();
		_slowEma.Reset();
		_volumeAverage.Reset();
		_priceStep = 0m;
		_currentDirection = null;
		_nextGridIndex = 0;
		_lastEntryPrice = 0m;
		_maxTrailingPoints = decimal.MinValue;
		_hasPreviousCandle = false;
		_previousOpen = 0m;
		_previousClose = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ParseGridMap();

		_fastEma.Length = Math.Max(1, FastMaPeriod);
		_midEma.Length = Math.Max(1, MidMaPeriod);
		_slowEma.Length = Math.Max(1, SlowMaPeriod);
		_volumeAverage.Length = Math.Max(1, VolumePeriod);

		_priceStep = Security?.PriceStep ?? 0.0001m;
		if (_priceStep <= 0m)
			_priceStep = 0.0001m;

		_maxTrailingPoints = decimal.MinValue;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastEma, _midEma, _slowEma, ProcessCandle)
			.Start();
	}

	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade == null)
			return;

		var direction = trade.Order.Direction;
		var volume = trade.Trade.Volume;

		if (volume <= 0m)
			return;

		if (_currentDirection is null)
			_currentDirection = direction;

		if (_currentDirection == direction)
		{
			_activeOrders.Add(new GridOrder(trade.Trade.Price, volume));
			_lastEntryPrice = trade.Trade.Price;

			if (_activeOrders.Count <= 1)
			{
				_nextGridIndex = 0;
				_maxTrailingPoints = decimal.MinValue;
			}
			else
			{
				_nextGridIndex = Math.Min(_activeOrders.Count - 1, Math.Max(0, _gridLevels.Count - 1));
			}
		}
		else
		{
			ReducePosition(volume);

			if (_activeOrders.Count == 0)
			{
				_currentDirection = null;
				_lastEntryPrice = 0m;
				_nextGridIndex = 0;
				_maxTrailingPoints = decimal.MinValue;
			}
			else
			{
				_lastEntryPrice = _activeOrders[^1].Price;
				_nextGridIndex = Math.Min(_activeOrders.Count - 1, Math.Max(0, _gridLevels.Count - 1));
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal midValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volumeIsOk = CheckVolume(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdatePreviousCandle(candle);
			return;
		}

		if (!volumeIsOk)
		{
			UpdatePreviousCandle(candle);
			return;
		}

		var signal = CalculateSignal(fastValue, midValue, slowValue);

		ApplyBoundaryFilters(ref signal, candle.ClosePrice);

		if (!IsWithinTradingHours(candle.CloseTime))
			signal = 0;

		if (!HasActiveCycle() && signal != 0)
			StartNewCycle(signal);

		if (HasActiveCycle())
		{
			if (ManageRisk(candle))
			{
				UpdatePreviousCandle(candle);
				return;
			}

			ProcessGridExpansion(candle);
		}

		UpdatePreviousCandle(candle);
	}

	private int CalculateSignal(decimal fastValue, decimal midValue, decimal slowValue)
	{
		if (DisableMaFilter)
		{
			if (!_hasPreviousCandle)
				return 0;

			if (_previousClose > _previousOpen)
				return 1;

			if (_previousClose < _previousOpen)
				return -1;

			return 0;
		}

		if (!_slowEma.IsFormed)
			return 0;

		var fastSignal = fastValue.CompareTo(slowValue);

		if (fastSignal == 0)
			return 0;

		var signal = fastSignal > 0 ? 1 : -1;

		if (MidMaPeriod > 0)
		{
			if (!_midEma.IsFormed)
				return 0;

			if ((midValue >= fastValue && fastValue > slowValue) || (midValue <= fastValue && fastValue < slowValue))
				return 0;
		}

		return signal;
	}

	private bool CheckVolume(ICandleMessage candle)
	{
		if (VolumePeriod < 1)
			return true;

		var average = _volumeAverage.Process(candle.TotalVolume, candle.CloseTime, true).ToDecimal();

		if (!_volumeAverage.IsFormed)
			return false;

		return average >= VolumeMinimum;
	}

	private void ApplyBoundaryFilters(ref int signal, decimal price)
	{
		var ask = price;
		var bid = price;

		if (ask > HighLevel)
			signal = 0;

		if (bid < LowLevel)
			signal = 0;

		if (ask < LowLevel)
			signal = 1;

		if (bid > HighLevel)
			signal = -1;
	}

	private bool IsWithinTradingHours(DateTimeOffset time)
	{
		var start = new TimeSpan(StartHour, StartMinute, 0);
		var end = new TimeSpan(FinishHour, FinishMinute, 0);
		var current = time.TimeOfDay;

		if (start == end)
			return true;

		if (start < end)
			return current >= start && current <= end;

		return current >= start || current <= end;
	}

	private void StartNewCycle(int signal)
	{
		if (FirstVolume <= 0m)
			return;

		if (signal > 0)
		{
			BuyMarket(FirstVolume);
		}
		else if (signal < 0)
		{
			SellMarket(FirstVolume);
		}
	}

	private bool ManageRisk(ICandleMessage candle)
	{
		if (_currentDirection is null || _activeOrders.Count == 0)
			return false;

		var averagePrice = CalculateAveragePrice();
		var close = candle.ClosePrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var direction = _currentDirection == Sides.Buy ? 1m : -1m;
		var takeProfitDistance = ConvertPointsToPrice(TakeProfitPoints);
		var stopLossDistance = ConvertPointsToPrice(StopLossPoints);

		if (direction > 0)
		{
			if (TakeProfitPoints > 0 && high - averagePrice >= takeProfitDistance)
			{
				ClosePosition();
				return true;
			}

			if (StopLossPoints > 0 && averagePrice - low >= stopLossDistance)
			{
				ClosePosition();
				return true;
			}
		}
		else
		{
			if (TakeProfitPoints > 0 && averagePrice - low >= takeProfitDistance)
			{
				ClosePosition();
				return true;
			}

			if (StopLossPoints > 0 && high - averagePrice >= stopLossDistance)
			{
				ClosePosition();
				return true;
			}
		}

		if (TrailDistancePoints > 0 && TrailStepPoints > 0)
		{
			var points = CalculateFloatingPoints(close);
			var activation = (decimal)Math.Max(TrailDistancePoints - TrailStepPoints, TrailStartPoints);

			if (points >= activation)
			{
				if (_maxTrailingPoints == decimal.MinValue || points > _maxTrailingPoints)
				{
					_maxTrailingPoints = points;
				}
				else if (_maxTrailingPoints - points >= TrailStepPoints)
				{
					ClosePosition();
					return true;
				}
			}
		}

		return false;
	}

	private void ProcessGridExpansion(ICandleMessage candle)
	{
		if (_currentDirection is null || _activeOrders.Count == 0 || _gridLevels.Count == 0)
			return;

		var index = Math.Min(_nextGridIndex, _gridLevels.Count - 1);
		var level = _gridLevels[index];
		var distance = level.Distance * _priceStep;

		if (distance <= 0m)
			return;

		if (_currentDirection == Sides.Buy)
		{
			if (_lastEntryPrice - candle.LowPrice >= distance)
			{
				if (level.Volume > 0m)
					BuyMarket(level.Volume);
			}
		}
		else
		{
			if (candle.HighPrice - _lastEntryPrice >= distance)
			{
				if (level.Volume > 0m)
					SellMarket(level.Volume);
			}
		}
	}

	private void UpdatePreviousCandle(ICandleMessage candle)
	{
		_previousOpen = candle.OpenPrice;
		_previousClose = candle.ClosePrice;
		_hasPreviousCandle = true;
	}

	private decimal CalculateAveragePrice()
	{
		if (_activeOrders.Count == 0)
			return 0m;

		decimal sum = 0m;
		decimal volume = 0m;

		foreach (var order in _activeOrders)
		{
			sum += order.Price * order.Volume;
			volume += order.Volume;
		}

		return volume > 0m ? sum / volume : 0m;
	}

	private decimal CalculateFloatingPoints(decimal price)
	{
		if (_activeOrders.Count == 0 || _priceStep <= 0m || _currentDirection is null)
			return 0m;

		var direction = _currentDirection == Sides.Buy ? 1m : -1m;
		decimal sum = 0m;

		foreach (var order in _activeOrders)
		{
			sum += (price - order.Price) * direction / _priceStep;
		}

		return sum;
	}

	private decimal ConvertPointsToPrice(int points)
		=> points <= 0 ? 0m : points * _priceStep;

	private bool HasActiveCycle()
		=> _activeOrders.Count > 0;

	private void ReducePosition(decimal volume)
	{
		var remaining = volume;

		for (var i = _activeOrders.Count - 1; i >= 0 && remaining > 0m; i--)
		{
			var order = _activeOrders[i];

			if (order.Volume > remaining)
			{
				order.Volume -= remaining;
				remaining = 0m;
			}
			else
			{
				remaining -= order.Volume;
				_activeOrders.RemoveAt(i);
			}
		}
	}

	private void ParseGridMap()
	{
		_gridLevels.Clear();

		if (string.IsNullOrWhiteSpace(GridMap))
			return;

		var parts = GridMap.Split(new[] { ' ', ';', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

		foreach (var part in parts)
		{
			var tokens = part.Split('/');

			if (tokens.Length != 2)
				continue;

			if (!decimal.TryParse(tokens[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var distance))
				continue;

			if (!decimal.TryParse(tokens[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var volume))
				continue;

			if (distance <= 0m || volume <= 0m)
				continue;

			_gridLevels.Add(new GridLevel(distance, volume));
		}
	}

	private sealed class GridOrder
	{
		public GridOrder(decimal price, decimal volume)
		{
			Price = price;
			Volume = volume;
		}

		public decimal Price { get; set; }

		public decimal Volume { get; set; }
	}

	private readonly record struct GridLevel(decimal Distance, decimal Volume);
}
