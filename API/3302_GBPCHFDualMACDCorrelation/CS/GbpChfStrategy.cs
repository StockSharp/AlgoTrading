using System;

using Ecng.ComponentModel;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// GBPCHF trading strategy driven by MACD correlation between GBPUSD and USDCHF.
/// It opens positions on GBPCHF when the GBPUSD MACD line crosses the USDCHF MACD line while both remain on the same side of zero.
/// </summary>
public class GbpChfStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingFrequencySeconds;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<bool> _closeOpposite;
	private readonly StrategyParam<bool> _onlyOnePosition;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _macdShortPeriod;
	private readonly StrategyParam<int> _macdLongPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<Security> _gbpUsdSecurity;
	private readonly StrategyParam<Security> _usdChfSecurity;

	private MovingAverageConvergenceDivergence _gbpUsdMacd;
	private MovingAverageConvergenceDivergence _usdChfMacd;

	private decimal? _prevGbpUsdMacd;
	private decimal? _lastGbpUsdMacd;
	private decimal? _prevUsdChfMacd;
	private decimal? _lastUsdChfMacd;

	private DateTimeOffset? _lastGbpUsdTime;
	private DateTimeOffset? _lastUsdChfTime;
	private DateTimeOffset? _lastSignalTime;
	private DateTimeOffset? _lastTrailingUpdateTime;
	private DateTimeOffset? _lastTrailingBarTime;

	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal _pipSize;
	private Sides? _currentSide;

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing frequency expressed in seconds. Values below 10 emulate trailing only on a new bar.
	/// </summary>
	public int TrailingFrequencySeconds
	{
		get => _trailingFrequencySeconds.Value;
		set => _trailingFrequencySeconds.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Minimum pip advance before the trailing stop is adjusted.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Inverts the generated trading signals when enabled.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Defines whether opposite positions should be closed before opening a new trade.
	/// </summary>
	public bool CloseOppositePositions
	{
		get => _closeOpposite.Value;
		set => _closeOpposite.Value = value;
	}

	/// <summary>
	/// Restricts the strategy to a single open position at a time.
	/// </summary>
	public bool OnlyOnePosition
	{
		get => _onlyOnePosition.Value;
		set => _onlyOnePosition.Value = value;
	}

	/// <summary>
	/// Order volume used when opening new trades.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// MACD short period.
	/// </summary>
	public int MacdShortPeriod
	{
		get => _macdShortPeriod.Value;
		set => _macdShortPeriod.Value = value;
	}

	/// <summary>
	/// MACD long period.
	/// </summary>
	public int MacdLongPeriod
	{
		get => _macdLongPeriod.Value;
		set => _macdLongPeriod.Value = value;
	}

	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// GBPUSD security used for the leading MACD calculation.
	/// </summary>
	public Security GbpUsdSecurity
	{
		get => _gbpUsdSecurity.Value;
		set => _gbpUsdSecurity.Value = value;
	}

	/// <summary>
	/// USDCHF security used for the confirming MACD calculation.
	/// </summary>
	public Security UsdChfSecurity
	{
		get => _usdChfSecurity.Value;
		set => _usdChfSecurity.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public GbpChfStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 70)
			.SetDisplay("Stop Loss", "Stop-loss distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(30, 120, 10);

		_takeProfitPips = Param(nameof(TakeProfitPips), 45)
			.SetDisplay("Take Profit", "Take-profit distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20, 90, 5);

		_trailingFrequencySeconds = Param(nameof(TrailingFrequencySeconds), 10)
			.SetDisplay("Trailing Frequency", "Trailing refresh interval in seconds", "Risk")
			.SetCanOptimize(false);

		_trailingStopPips = Param(nameof(TrailingStopPips), 0)
			.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0, 100, 10);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetDisplay("Trailing Step", "Minimum pip increase before trailing moves", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1, 20, 1);

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Invert generated trade directions", "General");

		_closeOpposite = Param(nameof(CloseOppositePositions), false)
			.SetDisplay("Close Opposite", "Close opposite positions before entering", "General");

		_onlyOnePosition = Param(nameof(OnlyOnePosition), true)
			.SetDisplay("Only One Position", "Allow only one simultaneous open position", "General");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Trade Volume", "Order volume when opening trades", "General")
			.SetGreaterThanZero();

		_macdShortPeriod = Param(nameof(MacdShortPeriod), 12)
			.SetDisplay("MACD Fast", "Fast EMA length for MACD", "Indicator")
			.SetGreaterThanZero();

		_macdLongPeriod = Param(nameof(MacdLongPeriod), 26)
			.SetDisplay("MACD Slow", "Slow EMA length for MACD", "Indicator")
			.SetGreaterThanZero();

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetDisplay("MACD Signal", "Signal EMA length for MACD", "Indicator")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used by indicators", "Data");

		_gbpUsdSecurity = Param<Security>(nameof(GbpUsdSecurity))
			.SetDisplay("GBPUSD", "Security used for the leading MACD", "Data")
			.SetRequired();

		_usdChfSecurity = Param<Security>(nameof(UsdChfSecurity))
			.SetDisplay("USDCHF", "Security used for the confirming MACD", "Data")
			.SetRequired();
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new (Security, DataType)[]
		{
			(Security, CandleType),
			(GbpUsdSecurity!, CandleType),
			(UsdChfSecurity!, CandleType)
		};
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_gbpUsdMacd = null;
		_usdChfMacd = null;
		_prevGbpUsdMacd = null;
		_lastGbpUsdMacd = null;
		_prevUsdChfMacd = null;
		_lastUsdChfMacd = null;
		_lastGbpUsdTime = null;
		_lastUsdChfTime = null;
		_lastSignalTime = null;
		_lastTrailingUpdateTime = null;
		_lastTrailingBarTime = null;
		_stopPrice = null;
		_takePrice = null;
		_pipSize = 0m;
		_currentSide = null;

		Volume = TradeVolume;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
			throw new InvalidOperationException("Trading security is not specified.");

		if (GbpUsdSecurity == null)
			throw new InvalidOperationException("GBPUSD security parameter is not configured.");

		if (UsdChfSecurity == null)
			throw new InvalidOperationException("USDCHF security parameter is not configured.");

		if (TradeVolume <= 0m)
			throw new InvalidOperationException("Trade volume must be positive.");

		Volume = TradeVolume;
		_pipSize = GetPipSize();

		_gbpUsdMacd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = MacdShortPeriod,
			LongPeriod = MacdLongPeriod,
			SignalPeriod = MacdSignalPeriod
		};

		_usdChfMacd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = MacdShortPeriod,
			LongPeriod = MacdLongPeriod,
			SignalPeriod = MacdSignalPeriod
		};

		var tradeSubscription = SubscribeCandles(CandleType);
		tradeSubscription
			.Bind(ProcessTradeSecurityCandle)
			.Start();

		var gbpUsdSubscription = SubscribeCandles(CandleType, security: GbpUsdSecurity);
		gbpUsdSubscription
			.Bind(_gbpUsdMacd!, ProcessGbpUsdCandle)
			.Start();

		var usdChfSubscription = SubscribeCandles(CandleType, security: UsdChfSecurity);
		usdChfSubscription
			.Bind(_usdChfMacd!, ProcessUsdChfCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, tradeSubscription);
			DrawIndicator(area, _gbpUsdMacd);
			DrawIndicator(area, _usdChfMacd);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			ResetProtection();
			_currentSide = null;
			return;
		}

		var side = Position > 0m ? Sides.Buy : Sides.Sell;
		if (_currentSide != side)
		{
			ResetProtection();
			_currentSide = side;
		}
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? step : 0.0001m;
	}

	private void ProcessTradeSecurityCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		CheckProtections(candle);
	}

	private void ProcessGbpUsdCandle(ICandleMessage candle, decimal macdLine, decimal macdSignal, decimal macdHistogram)
	{
		_ = macdSignal;
		_ = macdHistogram;

		if (candle.State != CandleStates.Finished)
			return;

		UpdateMacdHistory(ref _prevGbpUsdMacd, ref _lastGbpUsdMacd, ref _lastGbpUsdTime, macdLine, candle.OpenTime);
		TryGenerateSignal();
	}

	private void ProcessUsdChfCandle(ICandleMessage candle, decimal macdLine, decimal macdSignal, decimal macdHistogram)
	{
		_ = macdSignal;
		_ = macdHistogram;

		if (candle.State != CandleStates.Finished)
			return;

		UpdateMacdHistory(ref _prevUsdChfMacd, ref _lastUsdChfMacd, ref _lastUsdChfTime, macdLine, candle.OpenTime);
		TryGenerateSignal();
	}

	private static void UpdateMacdHistory(ref decimal? previous, ref decimal? current, ref DateTimeOffset? lastTime, decimal newValue, DateTimeOffset barTime)
	{
		previous = current;
		current = newValue;
		lastTime = barTime;
	}

	private void TryGenerateSignal()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevGbpUsdMacd is null || _lastGbpUsdMacd is null || _prevUsdChfMacd is null || _lastUsdChfMacd is null)
			return;

		if (_lastGbpUsdTime is null || _lastUsdChfTime is null)
			return;

		var signalTime = _lastGbpUsdTime > _lastUsdChfTime ? _lastGbpUsdTime.Value : _lastUsdChfTime.Value;
		if (_lastSignalTime != null && _lastSignalTime.Value >= signalTime)
			return;

		var prevLead = _prevGbpUsdMacd.Value;
		var currentLead = _lastGbpUsdMacd.Value;
		var prevConfirm = _prevUsdChfMacd.Value;
		var currentConfirm = _lastUsdChfMacd.Value;

		var bullish = prevLead > 0m && prevConfirm > 0m && prevLead < prevConfirm && currentLead > currentConfirm;
		var bearish = prevLead < 0m && prevConfirm < 0m && prevLead > prevConfirm && currentLead < currentConfirm;

		if (ReverseSignals)
		{
			(bullish, bearish) = (bearish, bullish);
		}

		if (bullish)
		{
			if (TryEnter(true))
				_lastSignalTime = signalTime;
		}
		else if (bearish)
		{
			if (TryEnter(false))
				_lastSignalTime = signalTime;
		}
	}

	private bool TryEnter(bool isLong)
	{
		if (OnlyOnePosition && Position != 0m)
			return false;

		if (isLong)
		{
			if (Position < 0m)
			{
				if (!CloseOppositePositions)
					return false;

				BuyMarket(Math.Abs(Position));
			}

			BuyMarket(Volume);
		}
		else
		{
			if (Position > 0m)
			{
				if (!CloseOppositePositions)
					return false;

				SellMarket(Position);
			}

			SellMarket(Volume);
		}

		ResetProtection();
		return true;
	}

	private void CheckProtections(ICandleMessage candle)
	{
		if (Position == 0m)
			return;

		var averagePrice = Position.AveragePrice ?? candle.ClosePrice;

		if (_stopPrice is null)
		{
			_stopPrice = CalculateInitialStop(averagePrice, Position > 0m);
		}

		if (_takePrice is null)
		{
			_takePrice = CalculateInitialTake(averagePrice, Position > 0m);
		}

		var allowTrailingUpdate = CanUpdateTrailing(candle);

		if (Position > 0m)
		{
			if (allowTrailingUpdate)
				UpdateTrailingForLong(candle);

			if (_stopPrice is not null && candle.LowPrice <= _stopPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetProtection();
				return;
			}

			if (_takePrice is not null && candle.HighPrice >= _takePrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetProtection();
			}
		}
		else if (Position < 0m)
		{
			if (allowTrailingUpdate)
				UpdateTrailingForShort(candle);

			if (_stopPrice is not null && candle.HighPrice >= _stopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetProtection();
				return;
			}

			if (_takePrice is not null && candle.LowPrice <= _takePrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetProtection();
			}
		}
	}

	private decimal? CalculateInitialStop(decimal entryPrice, bool isLong)
	{
		var stopDistance = StopLossPips > 0 ? StopLossPips * _pipSize : 0m;

		if (stopDistance <= 0m && TrailingStopPips > 0)
			stopDistance = TrailingStopPips * _pipSize;

		if (stopDistance <= 0m)
			return null;

		return isLong ? entryPrice - stopDistance : entryPrice + stopDistance;
	}

	private decimal? CalculateInitialTake(decimal entryPrice, bool isLong)
	{
		if (TakeProfitPips <= 0)
			return null;

		var distance = TakeProfitPips * _pipSize;
		return isLong ? entryPrice + distance : entryPrice - distance;
	}

	private bool CanUpdateTrailing(ICandleMessage candle)
	{
		if (TrailingStopPips <= 0 || TrailingStepPips <= 0)
			return false;

		if (TrailingFrequencySeconds >= 10)
		{
			var interval = TimeSpan.FromSeconds(TrailingFrequencySeconds);
			if (_lastTrailingUpdateTime is not null && candle.CloseTime - _lastTrailingUpdateTime < interval)
				return false;
		}
		else
		{
			if (_lastTrailingBarTime is not null && _lastTrailingBarTime == candle.OpenTime)
				return false;
		}

		return true;
	}

	private void UpdateTrailingForLong(ICandleMessage candle)
	{
		var trailingDistance = TrailingStopPips * _pipSize;
		var stepDistance = TrailingStepPips * _pipSize;

		if (trailingDistance <= 0m)
			return;

		var desiredStop = candle.ClosePrice - trailingDistance;

		if (_stopPrice is null || desiredStop > _stopPrice.Value + stepDistance)
		{
			_stopPrice = desiredStop;
			_lastTrailingUpdateTime = candle.CloseTime;
			_lastTrailingBarTime = candle.OpenTime;
		}
	}

	private void UpdateTrailingForShort(ICandleMessage candle)
	{
		var trailingDistance = TrailingStopPips * _pipSize;
		var stepDistance = TrailingStepPips * _pipSize;

		if (trailingDistance <= 0m)
			return;

		var desiredStop = candle.ClosePrice + trailingDistance;

		if (_stopPrice is null || desiredStop < _stopPrice.Value - stepDistance)
		{
			_stopPrice = desiredStop;
			_lastTrailingUpdateTime = candle.CloseTime;
			_lastTrailingBarTime = candle.OpenTime;
		}
	}

	private void ResetProtection()
	{
		_stopPrice = null;
		_takePrice = null;
		_lastTrailingUpdateTime = null;
		_lastTrailingBarTime = null;
	}
}
