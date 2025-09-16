using System;
using System.Collections.Generic;

using StockSharp;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class ERegressionChannelStrategy : Strategy
{
	private static readonly DataType DailyTimeFrame = TimeSpan.FromDays(1).TimeFrame();

	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<TimeSpan> _tradeStartTime;
	private readonly StrategyParam<TimeSpan> _tradeEndTime;
	private readonly StrategyParam<int> _regressionLength;
	private readonly StrategyParam<int> _degree;
	private readonly StrategyParam<decimal> _stdMultiplier;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<decimal> _trailingActivationPoints;
	private readonly StrategyParam<decimal> _trailingDistancePoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _dailyRangePoints;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _closes = new();

	private decimal? _previousMid;
	private decimal? _previousUpper;
	private decimal? _previousLower;
	private DateTimeOffset _previousTime;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;
	private DateTimeOffset? _lastTradeBarTime;
	private decimal _priceStep;
	private decimal? _previousDailyRange;

	/// <summary>
	/// Initializes a new instance of the <see cref="ERegressionChannelStrategy"/> class.
	/// </summary>
	public ERegressionChannelStrategy()
	{
		_volume = Param(nameof(Volume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Trading volume per entry", "General");

		_tradeStartTime = Param(nameof(TradeStartTime), new TimeSpan(3, 0, 0))
			.SetDisplay("Trade Start", "Start time of the trading window", "Time");

		_tradeEndTime = Param(nameof(TradeEndTime), new TimeSpan(21, 20, 0))
			.SetDisplay("Trade End", "End time of the trading window", "Time");

		_regressionLength = Param(nameof(RegressionLength), 250)
			.SetGreaterThanZero()
			.SetDisplay("Regression Length", "Number of bars used for regression", "Regression")
			.SetCanOptimize();

		_degree = Param(nameof(Degree), 3)
			.SetRange(1, 6)
			.SetDisplay("Degree", "Polynomial degree for the regression", "Regression")
			.SetCanOptimize();

		_stdMultiplier = Param(nameof(StdDevMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Std Dev Multiplier", "Width multiplier for the regression bands", "Regression")
			.SetCanOptimize();

		_enableTrailing = Param(nameof(EnableTrailing), false)
			.SetDisplay("Enable Trailing", "Enable trailing stop management", "Risk Management");

		_trailingActivationPoints = Param(nameof(TrailingActivationPoints), 30m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Activation", "Points of profit required before trailing starts", "Risk Management");

		_trailingDistancePoints = Param(nameof(TrailingDistancePoints), 30m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Distance", "Distance in points maintained by trailing", "Risk Management");

		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss", "Protective stop in points (0 disables)", "Risk Management");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit", "Protective target in points (0 disables)", "Risk Management");

		_dailyRangePoints = Param(nameof(DailyRangePoints), 150m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Daily Range Filter", "Maximum previous day range in points", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle type used for trading", "General");
	}

	/// <summary>
	/// Trading volume per entry.
	/// </summary>
	public new decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Start time of the trading window.
	/// </summary>
	public TimeSpan TradeStartTime
	{
		get => _tradeStartTime.Value;
		set => _tradeStartTime.Value = value;
	}

	/// <summary>
	/// End time of the trading window.
	/// </summary>
	public TimeSpan TradeEndTime
	{
		get => _tradeEndTime.Value;
		set => _tradeEndTime.Value = value;
	}

	/// <summary>
	/// Number of bars used for regression.
	/// </summary>
	public int RegressionLength
	{
		get => _regressionLength.Value;
		set => _regressionLength.Value = value;
	}

	/// <summary>
	/// Polynomial degree for the regression.
	/// </summary>
	public int Degree
	{
		get => _degree.Value;
		set => _degree.Value = value;
	}

	/// <summary>
	/// Width multiplier for the regression bands.
	/// </summary>
	public decimal StdDevMultiplier
	{
		get => _stdMultiplier.Value;
		set => _stdMultiplier.Value = value;
	}

	/// <summary>
	/// Enable trailing stop management.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Profit in points required before trailing starts.
	/// </summary>
	public decimal TrailingActivationPoints
	{
		get => _trailingActivationPoints.Value;
		set => _trailingActivationPoints.Value = value;
	}

	/// <summary>
	/// Distance in points maintained by trailing.
	/// </summary>
	public decimal TrailingDistancePoints
	{
		get => _trailingDistancePoints.Value;
		set => _trailingDistancePoints.Value = value;
	}

	/// <summary>
	/// Protective stop in points (0 disables).
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Protective target in points (0 disables).
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Maximum allowed previous day range in points.
	/// </summary>
	public decimal DailyRangePoints
	{
		get => _dailyRangePoints.Value;
		set => _dailyRangePoints.Value = value;
	}

	/// <summary>
	/// Primary candle type used for trading.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, DailyTimeFrame)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_closes.Clear();
		_previousMid = null;
		_previousUpper = null;
		_previousLower = null;
		_previousTime = default;
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_lastTradeBarTime = null;
		_previousDailyRange = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = Security?.PriceStep ?? 0m;
		if (_priceStep <= 0m)
			_priceStep = 1m;

		var tradingSubscription = SubscribeCandles(CandleType);
		tradingSubscription
			.Bind(ProcessCandle)
			.Start();

		var dailySubscription = SubscribeCandles(DailyTimeFrame);
		dailySubscription
			.Bind(ProcessDailyCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, tradingSubscription);
			DrawOwnTrades(area);
		}

		var stopLoss = StopLossPoints > 0m ? new Unit(StopLossPoints, UnitTypes.Point) : null;
		var takeProfit = TakeProfitPoints > 0m ? new Unit(TakeProfitPoints, UnitTypes.Point) : null;
		StartProtection(stopLoss: stopLoss, takeProfit: takeProfit);
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_previousDailyRange = candle.HighPrice - candle.LowPrice;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closes.Enqueue(candle.ClosePrice);
		if (_closes.Count > RegressionLength)
			_closes.Dequeue();

		if (_closes.Count < RegressionLength)
			return;

		var prices = new List<decimal>(_closes);
		var coeffs = PolyFit(prices, Degree);
		var currentIndex = prices.Count - 1;
		var mid = PolyEval(coeffs, currentIndex);
		var std = CalcStd(prices, coeffs) * StdDevMultiplier;
		var upper = mid + std;
		var lower = mid - std;

		if (_previousMid.HasValue)
		{
			DrawLine(_previousTime, _previousMid.Value, candle.OpenTime, mid);
			DrawLine(_previousTime, _previousUpper!.Value, candle.OpenTime, upper);
			DrawLine(_previousTime, _previousLower!.Value, candle.OpenTime, lower);
		}

		_previousMid = mid;
		_previousUpper = upper;
		_previousLower = lower;
		_previousTime = candle.OpenTime;

		if (EnableTrailing && UpdateTrailing(candle))
			return;

		if (HandleMidlineExit(candle, mid))
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!IsWithinTradeTime(candle.CloseTime))
			return;

		var dailyThreshold = GetPriceOffset(DailyRangePoints);
		if (dailyThreshold > 0m && _previousDailyRange is decimal prevRange && prevRange > dailyThreshold)
		{
			CloseAllPositions(candle.OpenTime);
			return;
		}

		if (_lastTradeBarTime == candle.OpenTime)
			return;

		if (candle.LowPrice <= lower && Position <= 0m)
		{
			OpenLong(candle.OpenTime);
			return;
		}

		if (candle.HighPrice >= upper && Position >= 0m)
		{
			OpenShort(candle.OpenTime);
		}
	}

	private bool UpdateTrailing(ICandleMessage candle)
	{
		if (TrailingDistancePoints <= 0m)
		{
			_longTrailingStop = null;
			_shortTrailingStop = null;
			return false;
		}

		var activation = GetPriceOffset(TrailingActivationPoints);
		var distance = GetPriceOffset(TrailingDistancePoints);

		if (Position > 0m)
		{
			var entryPrice = Position.AveragePrice;
			if (entryPrice <= 0m)
			return false;

			if (candle.HighPrice - entryPrice >= activation)
			{
				var candidate = candle.ClosePrice - distance;
				if (!_longTrailingStop.HasValue || candidate - _longTrailingStop.Value >= _priceStep)
				_longTrailingStop = candidate;
			}

			if (_longTrailingStop.HasValue && candle.LowPrice <= _longTrailingStop.Value)
			{
			SellMarket(Position);
			_longTrailingStop = null;
			_shortTrailingStop = null;
			_lastTradeBarTime = candle.OpenTime;
			return true;
			}
		}
		else if (Position < 0m)
		{
			var entryPrice = Position.AveragePrice;
			if (entryPrice <= 0m)
			return false;

			if (entryPrice - candle.LowPrice >= activation)
			{
				var candidate = candle.ClosePrice + distance;
				if (!_shortTrailingStop.HasValue || _shortTrailingStop.Value - candidate >= _priceStep)
				_shortTrailingStop = candidate;
			}

			if (_shortTrailingStop.HasValue && candle.HighPrice >= _shortTrailingStop.Value)
			{
			BuyMarket(Math.Abs(Position));
			_longTrailingStop = null;
			_shortTrailingStop = null;
			_lastTradeBarTime = candle.OpenTime;
			return true;
			}
		}
		else
		{
			_longTrailingStop = null;
			_shortTrailingStop = null;
		}

		return false;
	}

	private bool HandleMidlineExit(ICandleMessage candle, decimal mid)
	{
		if (Position > 0m && candle.ClosePrice >= mid)
		{
			SellMarket(Position);
			_longTrailingStop = null;
			_lastTradeBarTime = candle.OpenTime;
			return true;
		}

		if (Position < 0m && candle.ClosePrice <= mid)
		{
			BuyMarket(Math.Abs(Position));
			_shortTrailingStop = null;
			_lastTradeBarTime = candle.OpenTime;
			return true;
		}

		return false;
	}

	private void OpenLong(DateTimeOffset barTime)
	{
		var volume = Volume;
		if (volume <= 0m)
			return;

		if (Position < 0m)
			BuyMarket(Math.Abs(Position));

		BuyMarket(volume);
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_lastTradeBarTime = barTime;
	}

	private void OpenShort(DateTimeOffset barTime)
	{
		var volume = Volume;
		if (volume <= 0m)
			return;

		if (Position > 0m)
			SellMarket(Position);

		SellMarket(volume);
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_lastTradeBarTime = barTime;
	}

	private void CloseAllPositions(DateTimeOffset barTime)
	{
		if (Position > 0m)
			SellMarket(Position);
		else if (Position < 0m)
			BuyMarket(Math.Abs(Position));

		_longTrailingStop = null;
		_shortTrailingStop = null;
		_lastTradeBarTime = barTime;
	}

	private bool IsWithinTradeTime(DateTimeOffset time)
	{
		var start = TradeStartTime;
		var end = TradeEndTime;
		var current = time.TimeOfDay;

		return start <= end
			? current >= start && current < end
			: current >= start || current < end;
	}

	private decimal GetPriceOffset(decimal points)
	{
		if (points <= 0m)
			return 0m;

		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			step = 1m;

		return points * step;
	}

	private static decimal[] PolyFit(IReadOnlyList<decimal> y, int degree)
	{
		var n = y.Count;
		var actualDegree = Math.Min(degree, Math.Max(1, n - 1));
		var size = actualDegree + 1;
		var matrix = new decimal[size, size + 1];

		for (var row = 0; row < size; row++)
		{
			for (var col = 0; col < size; col++)
			{
				decimal sum = 0m;
				for (var i = 0; i < n; i++)
					sum += (decimal)Math.Pow(i, row + col);

				matrix[row, col] = sum;
			}

			decimal sumY = 0m;
			for (var i = 0; i < n; i++)
				sumY += y[i] * (decimal)Math.Pow(i, row);

			matrix[row, size] = sumY;
		}

		for (var i = 0; i < size; i++)
		{
			var pivot = matrix[i, i];
			if (pivot == 0m)
			{
				for (var k = i + 1; k < size; k++)
				{
					if (matrix[k, i] == 0m)
						continue;

					for (var j = i; j < size + 1; j++)
						(matrix[i, j], matrix[k, j]) = (matrix[k, j], matrix[i, j]);

					pivot = matrix[i, i];
					break;
				}
			}

			if (pivot == 0m)
				continue;

			for (var j = i; j < size + 1; j++)
				matrix[i, j] /= pivot;

			for (var row = 0; row < size; row++)
			{
				if (row == i)
					continue;

				var factor = matrix[row, i];
				if (factor == 0m)
					continue;

				for (var col = i; col < size + 1; col++)
					matrix[row, col] -= factor * matrix[i, col];
			}
		}

		var coeffs = new decimal[size];
		for (var i = 0; i < size; i++)
			coeffs[i] = matrix[i, size];

		return coeffs;
	}

	private static decimal PolyEval(IReadOnlyList<decimal> coeffs, decimal x)
	{
		decimal result = 0m;
		decimal power = 1m;
		for (var i = 0; i < coeffs.Count; i++)
		{
			result += coeffs[i] * power;
			power *= x;
		}

		return result;
	}

	private static decimal CalcStd(IReadOnlyList<decimal> values, decimal[] coeffs)
	{
		var n = values.Count;
		if (n == 0)
			return 0m;

		decimal sum = 0m;
		for (var i = 0; i < n; i++)
		{
			var fitted = PolyEval(coeffs, i);
			var diff = values[i] - fitted;
			sum += diff * diff;
		}

		return (decimal)Math.Sqrt((double)(sum / n));
	}
}
