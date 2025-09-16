using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// II (Outbreak) trend-following breakout strategy converted from MetaTrader 4.
/// Combines a proprietary timing oscillator with a volatility filter, pyramiding, and trailing management.
/// </summary>
public class IiOutbreakStrategy : Strategy
{
	private const decimal _epsilon = 0.0000000001m;

	private readonly StrategyParam<decimal> _commission;
	private readonly StrategyParam<decimal> _spreadThreshold;
	private readonly StrategyParam<decimal> _trailStopPoints;
	private readonly StrategyParam<decimal> _totalEquityRisk;
	private readonly StrategyParam<decimal> _maximumRisk;
	private readonly StrategyParam<decimal> _stdDevLimit;
	private readonly StrategyParam<decimal> _volatilityThreshold;
	private readonly StrategyParam<decimal> _accountLeverage;
	private readonly StrategyParam<bool> _warningAlerts;
	private readonly StrategyParam<DataType> _candleType;

	private StandardDeviation _stdDev = null!;

	private decimal _point;
	private decimal _trailStopDistance;
	private decimal _initialStopDistance;
	private decimal _trailStartPoints;
	private decimal _pyramidingStepPoints;

	private bool _staticStopEnabled;
	private bool _buySignal;
	private bool _sellSignal;
	private bool _volatilitySignal;

	private decimal _buyPyramidLevel;
	private decimal _sellPyramidLevel;
	private decimal _currentVolatilityThreshold;
	private decimal _currentSpreadLimit;
	private decimal _lastVolatilityValue;

	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;
	private decimal? _longInitialStop;
	private decimal? _shortInitialStop;

	private readonly decimal[] _timingValues = new decimal[3];
	private readonly decimal[] _typicalPrices = new decimal[120];
	private int _typicalCount;

	private ICandleMessage? _previousCandle;

	/// <summary>
	/// Commission used in the trailing start calculation (round lot cost in points).
	/// </summary>
	public decimal Commission
	{
		get => _commission.Value;
		set => _commission.Value = value;
	}

	/// <summary>
	/// Maximum acceptable spread expressed in points.
	/// </summary>
	public decimal SpreadThreshold
	{
		get => _spreadThreshold.Value;
		set => _spreadThreshold.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in points.
	/// </summary>
	public decimal TrailStopPoints
	{
		get => _trailStopPoints.Value;
		set => _trailStopPoints.Value = value;
	}

	/// <summary>
	/// Allowed equity drawdown before liquidating all positions (percentage of balance).
	/// </summary>
	public decimal TotalEquityRisk
	{
		get => _totalEquityRisk.Value;
		set => _totalEquityRisk.Value = value;
	}

	/// <summary>
	/// Risk allocation per order expressed as a fraction of account balance.
	/// </summary>
	public decimal MaximumRisk
	{
		get => _maximumRisk.Value;
		set => _maximumRisk.Value = value;
	}

	/// <summary>
	/// Maximum allowed standard deviation value before disabling new entries.
	/// </summary>
	public decimal StdDevLimit
	{
		get => _stdDevLimit.Value;
		set => _stdDevLimit.Value = value;
	}

	/// <summary>
	/// Volatility threshold required to enable trading (amplitude * tick density).
	/// </summary>
	public decimal VolatilityThreshold
	{
		get => _volatilityThreshold.Value;
		set => _volatilityThreshold.Value = value;
	}

	/// <summary>
	/// Account leverage used in margin approximations.
	/// </summary>
	public decimal AccountLeverage
	{
		get => _accountLeverage.Value;
		set => _accountLeverage.Value = value;
	}

	/// <summary>
	/// Enables logging when volatility filter blocks new trades.
	/// </summary>
	public bool WarningAlerts
	{
		get => _warningAlerts.Value;
		set => _warningAlerts.Value = value;
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
	/// Initializes a new instance of the <see cref="IiOutbreakStrategy"/> class.
	/// </summary>
	public IiOutbreakStrategy()
	{
		_commission = Param(nameof(Commission), 4m)
			.SetGreaterThanOrEqual(0m)
			.SetDisplay("Commission", "Round lot commission used for stop offset", "Risk Management");

		_spreadThreshold = Param(nameof(SpreadThreshold), 6m)
			.SetGreaterThanOrEqual(0m)
			.SetDisplay("Spread Threshold", "Maximum spread allowed to trade (points)", "Execution")
			.SetCanOptimize(true)
			.SetOptimize(2m, 15m, 1m);

		_trailStopPoints = Param(nameof(TrailStopPoints), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Trail Stop Points", "Trailing stop distance in points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10m, 40m, 5m);

		_totalEquityRisk = Param(nameof(TotalEquityRisk), 0.5m)
			.SetGreaterThanOrEqual(0m)
			.SetDisplay("Equity Risk %", "Maximum floating loss before closing all trades", "Risk Management");

		_maximumRisk = Param(nameof(MaximumRisk), 0.1m)
			.SetGreaterThanOrEqual(0m)
			.SetDisplay("Risk Fraction", "Fraction of balance allocated per order", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 0.2m, 0.01m);

		_stdDevLimit = Param(nameof(StdDevLimit), 0.002m)
			.SetGreaterThanOrEqual(0m)
			.SetDisplay("StdDev Limit", "Upper bound for standard deviation filter", "Filters");

		_volatilityThreshold = Param(nameof(VolatilityThreshold), 800m)
			.SetGreaterThanOrEqual(0m)
			.SetDisplay("Volatility Threshold", "Minimum volatility score required for entries", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(400m, 1600m, 100m);

		_accountLeverage = Param(nameof(AccountLeverage), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Account Leverage", "Used to approximate required margin", "Execution");

		_warningAlerts = Param(nameof(WarningAlerts), true)
			.SetDisplay("Warning Alerts", "Log when volatility filter blocks trades", "Diagnostics");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for calculations", "General");
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

		_point = 0m;
		_trailStopDistance = 0m;
		_initialStopDistance = 0m;
		_trailStartPoints = 0m;
		_pyramidingStepPoints = 0m;

		_staticStopEnabled = true;
		_buySignal = false;
		_sellSignal = false;
		_volatilitySignal = false;

		_buyPyramidLevel = 0m;
		_sellPyramidLevel = 0m;
		_currentVolatilityThreshold = 0m;
		_currentSpreadLimit = 0m;
		_lastVolatilityValue = 0m;

		_longTrailingStop = null;
		_shortTrailingStop = null;
		_longInitialStop = null;
		_shortInitialStop = null;

		Array.Fill(_timingValues, 50m);
		Array.Clear(_typicalPrices, 0, _typicalPrices.Length);
		_typicalCount = 0;

		_previousCandle = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_point = Security.PriceStep ?? 0.0001m;
		_trailStopDistance = TrailStopPoints * _point;
		_initialStopDistance = _trailStopDistance * 2m;
		_trailStartPoints = TrailStopPoints + Math.Truncate(Commission) + SpreadThreshold;
		_pyramidingStepPoints = Math.Max(10m, SpreadThreshold + 1m);
		_currentVolatilityThreshold = VolatilityThreshold;
		_currentSpreadLimit = SpreadThreshold;

		_stdDev = new StandardDeviation { Length = 10 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stdDev);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateTiming(candle);
		var stdValue = _stdDev.Process(candle.ClosePrice, candle.ServerTime, true).ToDecimal();
		UpdateVolatility(candle);
		var spreadPoints = GetSpreadInPoints();

		var canTrade = IsFormedAndOnlineAndAllowTrading();

		if (_previousCandle is not null && !_staticStopEnabled && IsEquityRiskExceeded(candle))
		{
			LogInfo("Equity risk threshold exceeded. Closing all positions.");
			CloseAll();
			ResetAfterClose();
			_previousCandle = candle;
			return;
		}

		if (!canTrade)
		{
			_previousCandle = candle;
			return;
		}

		if (Position == 0)
		{
			ResetStateBeforeEntry();

			if (IsTradingBlockedByCalendar(candle.OpenTime))
			{
				_previousCandle = candle;
				return;
			}

			if (stdValue > StdDevLimit)
			{
				if (WarningAlerts)
					LogInfo($"Volatility conditions are not met. StdDev={stdValue:F6} > {StdDevLimit:F6}.");

				_previousCandle = candle;
				return;
			}

			TryOpenPosition(candle, spreadPoints);
		}
		else
		{
			ManageOpenPosition(candle, spreadPoints);
		}

		_previousCandle = candle;
	}

	private void ResetStateBeforeEntry()
	{
		_staticStopEnabled = true;
		_buyPyramidLevel = 0m;
		_sellPyramidLevel = 0m;
		_currentVolatilityThreshold = VolatilityThreshold;
		_currentSpreadLimit = SpreadThreshold;
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_longInitialStop = null;
		_shortInitialStop = null;
	}

	private void ResetAfterClose()
	{
		_staticStopEnabled = true;
		_buyPyramidLevel = 0m;
		_sellPyramidLevel = 0m;
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_longInitialStop = null;
		_shortInitialStop = null;
		_currentVolatilityThreshold = VolatilityThreshold;
		_currentSpreadLimit = SpreadThreshold;
	}

	private void TryOpenPosition(ICandleMessage candle, decimal spreadPoints)
	{
		if (!_volatilitySignal)
			return;

		if (_currentSpreadLimit > 0m && spreadPoints > _currentSpreadLimit)
			return;

		var volume = CalculateOrderVolume();

		if (volume <= 0m)
			return;

		if (!HasSufficientMargin(candle.ClosePrice, volume))
			return;

		if (_buySignal)
		{
			BuyMarket(volume);
			_longInitialStop = candle.ClosePrice - _initialStopDistance;
			LogInfo($"Opened long at {candle.ClosePrice} with volume {volume}.");
		}
		else if (_sellSignal)
		{
			SellMarket(volume);
			_shortInitialStop = candle.ClosePrice + _initialStopDistance;
			LogInfo($"Opened short at {candle.ClosePrice} with volume {volume}.");
		}
	}

	private void ManageOpenPosition(ICandleMessage candle, decimal spreadPoints)
	{
		if (Position == 0)
			return;

		var avgPrice = Position.AveragePrice;
		if (avgPrice is null || _point <= 0m)
			return;

		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		if (_staticStopEnabled)
		{
			if (Position > 0 && _longInitialStop.HasValue && candle.LowPrice <= _longInitialStop.Value)
			{
				SellMarket(volume);
				LogInfo("Initial long stop triggered.");
				ResetAfterClose();
				return;
			}

			if (Position < 0 && _shortInitialStop.HasValue && candle.HighPrice >= _shortInitialStop.Value)
			{
				BuyMarket(volume);
				LogInfo("Initial short stop triggered.");
				ResetAfterClose();
				return;
			}
		}

		var profitPoints = Position > 0
			? (candle.ClosePrice - avgPrice.Value) / _point
			: (avgPrice.Value - candle.ClosePrice) / _point;

		if (profitPoints < _trailStartPoints)
			return;

		if (Position > 0)
		{
			var newStop = candle.ClosePrice - _trailStopDistance;
			if (!_longTrailingStop.HasValue || newStop - _longTrailingStop.Value >= _point)
				_longTrailingStop = newStop;

			if (_longTrailingStop.HasValue && candle.LowPrice <= _longTrailingStop.Value)
			{
				SellMarket(volume);
				LogInfo($"Trailing stop hit for long at {_longTrailingStop.Value}.");
				ResetAfterClose();
				return;
			}

			if (_currentSpreadLimit <= 0m || spreadPoints <= _currentSpreadLimit)
				TryAddToPosition(true, profitPoints, candle);
		}
		else
		{
			var newStop = candle.ClosePrice + _trailStopDistance;
			if (!_shortTrailingStop.HasValue || _shortTrailingStop.Value - newStop >= _point)
				_shortTrailingStop = newStop;

			if (_shortTrailingStop.HasValue && candle.HighPrice >= _shortTrailingStop.Value)
			{
				BuyMarket(volume);
				LogInfo($"Trailing stop hit for short at {_shortTrailingStop.Value}.");
				ResetAfterClose();
				return;
			}

			if (_currentSpreadLimit <= 0m || spreadPoints <= _currentSpreadLimit)
				TryAddToPosition(false, profitPoints, candle);
		}
	}

	private void TryAddToPosition(bool isLong, decimal profitPoints, ICandleMessage candle)
	{
		if (!_volatilitySignal)
			return;

		if (isLong)
		{
			if (!_buySignal)
				return;

			if (profitPoints < _buyPyramidLevel + _pyramidingStepPoints)
				return;

			var volume = CalculateOrderVolume();
			if (volume <= 0m || !HasSufficientMargin(candle.ClosePrice, volume))
				return;

			BuyMarket(volume);
			_buyPyramidLevel = profitPoints;
			_staticStopEnabled = false;
			_longInitialStop = null;
			LogInfo($"Added to long position at {candle.ClosePrice} (profit {profitPoints:F2} pts).");
		}
		else
		{
			if (!_sellSignal)
				return;

			if (profitPoints < _sellPyramidLevel + _pyramidingStepPoints)
				return;

			var volume = CalculateOrderVolume();
			if (volume <= 0m || !HasSufficientMargin(candle.ClosePrice, volume))
				return;

			SellMarket(volume);
			_sellPyramidLevel = profitPoints;
			_staticStopEnabled = false;
			_shortInitialStop = null;
			LogInfo($"Added to short position at {candle.ClosePrice} (profit {profitPoints:F2} pts).");
		}
	}

	private bool HasSufficientMargin(decimal price, decimal volume)
	{
		var balance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue;
		if (balance is null || balance.Value <= 0m)
			return true;

		var leverage = AccountLeverage <= 0m ? 1m : AccountLeverage;
		var requiredMargin = volume * price / leverage * (1m + MaximumRisk * 190m);
		return balance.Value >= requiredMargin;
	}

	private decimal CalculateOrderVolume()
	{
		var step = Security.VolumeStep ?? 1m;
		var minVolume = Security.MinVolume ?? step;
		var maxVolume = Security.MaxVolume ?? decimal.MaxValue;

		var balance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		var leverage = AccountLeverage <= 0m ? 1m : AccountLeverage;
		var denom = leverage == 0m ? 1m : leverage;
		var riskVolume = balance > 0m
			? balance * MaximumRisk / (500000m / denom)
			: Volume;

		if (riskVolume <= 0m)
			riskVolume = Volume;

		if (riskVolume <= 0m)
			riskVolume = minVolume > 0m ? minVolume : step;

		var volume = riskVolume;

		if (step > 0m)
			volume = Math.Ceiling(volume / step) * step;

		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		if (maxVolume < decimal.MaxValue && volume > maxVolume)
			volume = maxVolume;

		return volume;
	}

	private bool IsEquityRiskExceeded(ICandleMessage candle)
	{
		var balance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue;
		if (balance is null || balance.Value <= 0m || Position == 0)
			return false;

		var avgPrice = Position.AveragePrice;
		if (avgPrice is null)
			return false;

		var volume = Math.Abs(Position);
		var currentPrice = candle.ClosePrice;
		var pnl = Position > 0
			? (currentPrice - avgPrice.Value) * volume
			: (avgPrice.Value - currentPrice) * volume;

		var drawdown = pnl < 0m ? -pnl : 0m;
		var threshold = balance.Value * TotalEquityRisk / 100m;
		return drawdown > threshold;
	}

	private decimal GetSpreadInPoints()
	{
		if (_point <= 0m)
			return 0m;

		var bid = Security.BestBid?.Price;
		var ask = Security.BestAsk?.Price;
		if (bid is null || ask is null)
			return 0m;

		return (ask.Value - bid.Value) / _point;
	}

	private void UpdateVolatility(ICandleMessage candle)
	{
		if (_previousCandle is null || _point <= 0m)
		{
			_volatilitySignal = false;
			_lastVolatilityValue = 0m;
			return;
		}

		var timeSeconds = (decimal)(candle.CloseTime - candle.OpenTime).TotalSeconds;
		if (timeSeconds <= 0m)
		{
			_volatilitySignal = false;
			_lastVolatilityValue = 0m;
			return;
		}

		var amplitude = (Math.Abs(_previousCandle.HighPrice - candle.LowPrice)
			+ Math.Abs(candle.HighPrice - _previousCandle.LowPrice)
			+ Math.Abs(_previousCandle.ClosePrice - candle.ClosePrice)) / _point;

		var totalVolume = candle.TotalVolume ?? 0m;
		var mass = totalVolume / timeSeconds;
		_lastVolatilityValue = amplitude * mass;
		_volatilitySignal = _lastVolatilityValue > _currentVolatilityThreshold;
	}

	private void UpdateTiming(ICandleMessage candle)
	{
		var cpiv = 100m * ((candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m);

		var limit = _typicalPrices.Length;
		var count = Math.Min(_typicalCount + 1, limit);

		for (var i = Math.Min(count - 1, limit - 1); i > 0; i--)
			_typicalPrices[i] = _typicalPrices[i - 1];

		_typicalPrices[0] = cpiv;
		_typicalCount = count;

		CalculateTimingSignals();
	}

	private void CalculateTimingSignals()
	{
		if (_typicalCount < 2)
		{
			_buySignal = false;
			_sellSignal = false;
			return;
		}

		Array.Fill(_timingValues, 50m);

		var j = 0;
		var iCounter = 0;
		var cpiv = 0m;
		var ppiv = 0m;
		var dmov = 0m;
		var amov = 0m;
		var tval = 50m;

		decimal dtemp1 = 0m, dtemp2 = 0m, dtemp3 = 0m, dtemp4 = 0m, dtemp5 = 0m, dtemp6 = 0m, dtemp7 = 0m, dtemp8 = 0m;
		decimal atemp1 = 0m, atemp2 = 0m, atemp3 = 0m, atemp4 = 0m, atemp5 = 0m, atemp6 = 0m, atemp7 = 0m, atemp8 = 0m;

		for (var idx = _typicalCount - 1; idx >= 0; idx--)
		{
			var typical = _typicalPrices[idx];

			if (j == 0)
			{
				j = 1;
				iCounter = 0;
				cpiv = typical;
			}
			else
			{
				if (j < 7)
					j++;

				ppiv = cpiv;
				cpiv = typical;
				var dpiv = cpiv - ppiv;

				dtemp1 = (2m / 3m) * dtemp1 + (1m / 3m) * dpiv;
				dtemp2 = (1m / 3m) * dtemp1 + (2m / 3m) * dtemp2;
				dtemp3 = 1.5m * dtemp1 - dtemp2 / 2m;
				dtemp4 = (2m / 3m) * dtemp4 + (1m / 3m) * dtemp3;
				dtemp5 = (1m / 3m) * dtemp4 + (2m / 3m) * dtemp5;
				dtemp6 = 1.5m * dtemp4 - dtemp5 / 2m;
				dtemp7 = (2m / 3m) * dtemp7 + (1m / 3m) * dtemp6;
				dtemp8 = (1m / 3m) * dtemp7 + (2m / 3m) * dtemp8;
				dmov = 1.5m * dtemp7 - dtemp8 / 2m;

				atemp1 = (2m / 3m) * atemp1 + (1m / 3m) * Math.Abs(dpiv);
				atemp2 = (1m / 3m) * atemp1 + (2m / 3m) * atemp2;
				atemp3 = 1.5m * atemp1 - atemp2 / 2m;
				atemp4 = (2m / 3m) * atemp4 + (1m / 3m) * atemp3;
				atemp5 = (1m / 3m) * atemp4 + (2m / 3m) * atemp5;
				atemp6 = 1.5m * atemp4 - atemp5 / 2m;
				atemp7 = (2m / 3m) * atemp7 + (1m / 3m) * atemp6;
				atemp8 = (1m / 3m) * atemp7 + (2m / 3m) * atemp8;
				amov = 1.5m * atemp7 - atemp8 / 2m;

				if (j <= 6 && cpiv != ppiv)
					iCounter++;

				if (j == 6 && iCounter == 0)
					j = 0;
			}

			if (j > 6 && amov > _epsilon)
			{
				tval = 50m * (dmov / amov + 1m);
				if (tval > 100m)
					tval = 100m;
				else if (tval < 0m)
					tval = 0m;
			}
			else
			{
				tval = 50m;
			}

			if (idx <= 2)
				_timingValues[idx] = tval;
		}

		_buySignal = _timingValues[1] <= _timingValues[2] && _timingValues[0] > _timingValues[1];
		_sellSignal = _timingValues[1] >= _timingValues[2] && _timingValues[0] < _timingValues[1];
	}

	private static bool IsTradingBlockedByCalendar(DateTimeOffset time)
	{
		if (time.DayOfWeek == DayOfWeek.Friday && time.Hour >= 23)
			return true;

		var dayOfYear = time.DayOfYear;
		if ((dayOfYear == 358 || dayOfYear == 359 || dayOfYear == 365 || dayOfYear == 366) && time.Hour >= 16)
			return true;

		return false;
	}
}
