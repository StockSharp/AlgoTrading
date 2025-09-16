using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Implements the Maximus vX Lite consolidation breakout strategy.
/// </summary>
public class MaximusVXLiteStrategy : Strategy
{
	private readonly StrategyParam<int> _delayOpen;
	private readonly StrategyParam<int> _distancePoints;
	private readonly StrategyParam<int> _rangePoints;
	private readonly StrategyParam<int> _historyDepth;
	private readonly StrategyParam<int> _rangeLookback;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<decimal> _minProfitPercent;

	private readonly List<CandleInfo> _history = new();

	private decimal _upperMax;
	private decimal _upperMin;
	private decimal _lowerMax;
	private decimal _lowerMin;

	private decimal _priceStep = 1m;
	private decimal _extDistance;
	private decimal _extRange;
	private decimal _extStopLoss;

	private DateTimeOffset? _lastBuyTime;
	private DateTimeOffset? _lastSellTime;
	private decimal _lastKnownBalance;

	private decimal? _activeStop;
	private decimal? _activeTake;

	private readonly struct CandleInfo
	{
		public CandleInfo(decimal high, decimal low)
		{
			High = high;
			Low = low;
		}

		public decimal High { get; }

		public decimal Low { get; }
	}

	/// <summary>
	/// Number of timeframe intervals allowed before averaging into the same direction.
	/// </summary>
	public int DelayOpen
	{
		get => _delayOpen.Value;
		set => _delayOpen.Value = value;
	}

	/// <summary>
	/// Minimum distance in points from the consolidation band before entering a trade.
	/// </summary>
	public int DistancePoints
	{
		get => _distancePoints.Value;
		set => _distancePoints.Value = value;
	}

	/// <summary>
	/// Size of the consolidation range in points.
	/// </summary>
	public int RangePoints
	{
		get => _rangePoints.Value;
		set => _rangePoints.Value = value;
	}

	/// <summary>
	/// Number of historical candles considered when searching for ranges.
	/// </summary>
	public int HistoryDepth
	{
		get => _historyDepth.Value;
		set => _historyDepth.Value = value;
	}

	/// <summary>
	/// Window length used to calculate local highs and lows.
	/// </summary>
	public int RangeLookback
	{
		get => _rangeLookback.Value;
		set => _rangeLookback.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations and signals.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Risk budget per trade expressed as percent of portfolio value.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Stop loss distance in points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Floating profit percentage that triggers forced position close.
	/// </summary>
	public decimal MinProfitPercent
	{
		get => _minProfitPercent.Value;
		set => _minProfitPercent.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MaximusVXLiteStrategy"/> class.
	/// </summary>
	public MaximusVXLiteStrategy()
	{
		_delayOpen = Param(nameof(DelayOpen), 2)
			.SetGreaterOrEqualZero()
			.SetDisplay("Delay Open", "How many timeframe periods allow averaging in the same direction", "Trading Rules");

		_distancePoints = Param(nameof(DistancePoints), 850)
			.SetGreaterThanZero()
			.SetDisplay("Distance", "Minimum distance from consolidation band before trading", "Trading Rules");

		_rangePoints = Param(nameof(RangePoints), 500)
			.SetGreaterThanZero()
			.SetDisplay("Range", "Width of consolidation channel in points", "Trading Rules");

		_historyDepth = Param(nameof(HistoryDepth), 1000)
			.SetGreaterThanZero()
			.SetDisplay("History Depth", "Number of candles inspected for consolidation zones", "Data");

		_rangeLookback = Param(nameof(RangeLookback), 40)
			.SetGreaterThanZero()
			.SetDisplay("Range Lookback", "Candles used to calculate local maxima and minima", "Data");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used by the strategy", "General");

		_riskPercent = Param(nameof(RiskPercent), 5m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Risk Percent", "Portfolio percent risked per trade", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss", "Stop loss distance in points", "Risk");

		_minProfitPercent = Param(nameof(MinProfitPercent), 1m)
			.SetGreaterOrEqual(0m)
			.SetDisplay("Min Profit Percent", "Floating profit percent required to close all positions", "Risk");
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
		_history.Clear();
		_upperMax = _upperMin = _lowerMax = _lowerMin = 0m;
		_lastBuyTime = null;
		_lastSellTime = null;
		_activeStop = null;
		_activeTake = null;
		_lastKnownBalance = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		UpdateDerivedValues();
		_lastKnownBalance = Portfolio?.CurrentValue ?? 0m;

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateDerivedValues();

		UpdateHistory(candle);

		if (Position == 0m)
		{
			var balance = Portfolio?.CurrentValue;
			if (balance.HasValue && balance.Value > 0m)
				_lastKnownBalance = balance.Value;

			_activeStop = null;
			_activeTake = null;
		}

		FindHighLow(candle.ClosePrice);

		if (HandleStopsAndTargets(candle))
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		TryEnterPositions(candle);
		TryLockInProfit();
	}

	private void UpdateHistory(ICandleMessage candle)
	{
		// Store the latest completed candle at the front of the list.
		_history.Insert(0, new CandleInfo(candle.HighPrice, candle.LowPrice));
		while (_history.Count > HistoryDepth)
			_history.RemoveAt(_history.Count - 1);
	}

	private void UpdateDerivedValues()
	{
		// Convert point-based parameters to actual price distances using the security step.
		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		_priceStep = step;
		_extDistance = DistancePoints * step;
		_extRange = RangePoints * step;
		_extStopLoss = StopLossPoints * step;
	}

	private void FindHighLow(decimal currentClose)
	{
		if (_history.Count == 0)
			return;

		var recalc = currentClose - 100m * _priceStep > _lowerMax
			|| currentClose + 100m * _priceStep < _lowerMin
			|| currentClose - 100m * _priceStep > _upperMax
			|| currentClose + 100m * _priceStep < _upperMin;

		if (!recalc)
			return;

		var foundUpper = false;
		for (var i = 0; i < _history.Count; i++)
		{
			var high = _history[i].High;
			if (currentClose - _extRange <= high)
				continue;

			var (windowMax, windowMin) = GetRangeWindow(i);
			if (windowMax == 0m && windowMin == 0m)
				continue;

			if (windowMax - windowMin <= _extRange && currentClose + _extRange > windowMax && currentClose + _extRange > windowMin)
			{
				foundUpper = true;
				break;
			}
		}

		var halfRange = RangePoints * 0.5m * _priceStep;
		if (!foundUpper)
		{
			var baseValue = Math.Floor(currentClose + 100m * _priceStep);
			_upperMax = baseValue + halfRange;
			_upperMin = baseValue - halfRange;
		}
		else
		{
			var baseValue = Math.Floor((currentClose + 100m * _priceStep) * 100m) / 100m;
			_upperMax = baseValue + halfRange;
			_upperMin = baseValue - halfRange;
		}

		var lowerFound = false;
		decimal lowerMax = 0m;
		decimal lowerMin = 0m;

		for (var i = 0; i < _history.Count; i++)
		{
			var high = _history[i].High;
			if (currentClose - _extRange <= high)
				continue;

			var (windowMax, windowMin) = GetRangeWindow(i);
			if (windowMax == 0m && windowMin == 0m)
				continue;

			if (windowMax - windowMin <= _extRange && currentClose - _extRange > windowMax && currentClose - _extRange > windowMin)
			{
				lowerMax = windowMax;
				lowerMin = windowMin;
				lowerFound = true;
				break;
			}
		}

		if (!lowerFound)
		{
			var baseValue = Math.Floor((currentClose - 100m * _priceStep) * 100m) / 100m;
			lowerMax = baseValue + halfRange;
			lowerMin = baseValue - halfRange;
		}

		_lowerMax = lowerMax;
		_lowerMin = lowerMin;
	}

	private (decimal max, decimal min) GetRangeWindow(int startIndex)
	{
		// Replicates ArrayMaximum/ArrayMinimum over a sliding window.
		var count = Math.Min(RangeLookback, _history.Count - startIndex);
		if (count <= 0)
			return (0m, 0m);

		var max = decimal.MinValue;
		var min = decimal.MaxValue;

		for (var j = 0; j < count; j++)
		{
			var item = _history[startIndex + j];
			if (item.High > max)
				max = item.High;
			if (item.Low < min)
				min = item.Low;
		}

		return (max, min);
	}

	private bool HandleStopsAndTargets(ICandleMessage candle)
	{
		// Manually exit positions when candle extremes touch the stored stop or target.
		if (Position > 0m)
		{
			if (_activeStop.HasValue && candle.LowPrice <= _activeStop.Value)
			{
				SellMarket(Position);
				ResetAfterExit();
				return true;
			}

			if (_activeTake.HasValue && candle.HighPrice >= _activeTake.Value)
			{
				SellMarket(Position);
				ResetAfterExit();
				return true;
			}
		}
		else if (Position < 0m)
		{
			var volume = Math.Abs(Position);
			if (_activeStop.HasValue && candle.HighPrice >= _activeStop.Value)
			{
				BuyMarket(volume);
				ResetAfterExit();
				return true;
			}

			if (_activeTake.HasValue && candle.LowPrice <= _activeTake.Value)
			{
				BuyMarket(volume);
				ResetAfterExit();
				return true;
			}
		}

		return false;
	}

	private void TryEnterPositions(ICandleMessage candle)
	{
		var price = candle.ClosePrice;
		if (price <= 0m)
			return;

		var timeFrame = CandleType.Arg is TimeSpan span ? span : TimeSpan.Zero;
		var delayDuration = TimeSpan.FromTicks(timeFrame.Ticks * DelayOpen);
		var now = candle.CloseTime;

		var hasLong = Position > 0m;
		var hasShort = Position < 0m;

		var allowAdditionalBuy = !hasLong;
		if (DelayOpen > 0 && hasLong)
			allowAdditionalBuy = _lastBuyTime.HasValue && (_lastBuyTime.Value + delayDuration) > now;
		else if (DelayOpen == 0 && hasLong)
			allowAdditionalBuy = false;

		var allowAdditionalSell = !hasShort;
		if (DelayOpen > 0 && hasShort)
			allowAdditionalSell = _lastSellTime.HasValue && (_lastSellTime.Value + delayDuration) > now;
		else if (DelayOpen == 0 && hasShort)
			allowAdditionalSell = false;

		var volume = CalculateOrderVolume();
		if (volume <= 0m)
			return;

		var buyPrimary = _lowerMax != 0m && _upperMin != 0m && price - _extDistance > _lowerMax;
		var buySecondary = _upperMax != 0m && price - _extDistance > _upperMax;

		if (allowAdditionalBuy && (buyPrimary || buySecondary) && Position <= 0m)
		{
			var stopPrice = StopLossPoints == 0 ? (decimal?)null : price - _extStopLoss;
			decimal? takePrice;

			if (buyPrimary)
			{
				var diff = _upperMin - _lowerMax;
				var tempTp = diff / 3m * 2m * _priceStep;
				if (tempTp < _extRange)
					tempTp = _extRange;
				takePrice = price + tempTp;
			}
			else
			{
				takePrice = price + 2m * _extRange;
			}

			var orderVolume = volume + (Position < 0m ? Math.Abs(Position) : 0m);
			BuyMarket(orderVolume);
			_activeStop = stopPrice;
			_activeTake = takePrice;
			_lastBuyTime = now;
			return;
		}

		var sellPrimary = _upperMin != 0m && price + _extDistance < _upperMin;
		var sellSecondary = _lowerMin != 0m && price + _extDistance < _lowerMin;

		if (allowAdditionalSell && (sellPrimary || sellSecondary) && Position >= 0m)
		{
			var stopPrice = StopLossPoints == 0 ? (decimal?)null : price + _extStopLoss;
			decimal? takePrice;

			if (sellPrimary)
			{
				var diff = _upperMin - _lowerMax;
				var tempTp = diff / 3m * 2m * _priceStep;
				if (tempTp < _extRange)
					tempTp = _extRange;
				takePrice = price - tempTp;
			}
			else
			{
				takePrice = price - 2m * _extRange;
			}

			var orderVolume = volume + (Position > 0m ? Math.Abs(Position) : 0m);
			SellMarket(orderVolume);
			_activeStop = stopPrice;
			_activeTake = takePrice;
			_lastSellTime = now;
		}
	}

	private decimal CalculateOrderVolume()
	{
		// Use risk budget when a stop loss is available, otherwise fall back to base volume.
		var baseVolume = Volume;
		if (RiskPercent > 0m && _extStopLoss > 0m)
		{
			var capital = Portfolio?.CurrentValue ?? 0m;
			if (capital > 0m)
			{
				var riskBudget = capital * (RiskPercent / 100m);
				if (riskBudget > 0m)
				{
					var rawVolume = riskBudget / _extStopLoss;
					if (rawVolume > 0m)
						baseVolume = rawVolume;
				}
			}
		}

		var step = Security?.VolumeStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		var adjusted = Math.Floor(baseVolume / step) * step;
		if (adjusted <= 0m)
			adjusted = step;

		return adjusted;
	}

	private void TryLockInProfit()
	{
		if (Position == 0m)
			return;

		var balance = _lastKnownBalance;
		if (balance <= 0m)
			return;

		var equity = Portfolio?.CurrentValue ?? balance;
		var profitPercent = (equity - balance) / balance * 100m;
		if (profitPercent > MinProfitPercent)
		{
			ClosePosition();
			ResetAfterExit();
		}
	}

	private void ClosePosition()
	{
		if (Position > 0m)
			SellMarket(Position);
		else if (Position < 0m)
			BuyMarket(Math.Abs(Position));
	}

	private void ResetAfterExit()
	{
		// Clear timers and protective orders so the next setup starts clean.
		_activeStop = null;
		_activeTake = null;
		_lastBuyTime = null;
		_lastSellTime = null;
	}
}
