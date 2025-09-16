using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Zone recovery hedging strategy converted from MetaTrader expert advisor.
/// The strategy alternates buy and sell positions around a base price to recover drawdowns.
/// </summary>
public class ZoneRecoveryAreaStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _monthlyCandleType;
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _zoneRecoveryPips;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<bool> _useVolumeMultiplier;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<decimal> _volumeIncrement;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<bool> _useMoneyTakeProfit;
	private readonly StrategyParam<decimal> _moneyTakeProfit;
	private readonly StrategyParam<bool> _usePercentTakeProfit;
	private readonly StrategyParam<decimal> _percentTakeProfit;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<decimal> _trailingStartProfit;
	private readonly StrategyParam<decimal> _trailingDrawdown;

	private SimpleMovingAverage _fastMa = null!;
	private SimpleMovingAverage _slowMa = null!;
	private MovingAverageConvergenceDivergenceSignal _monthlyMacd = null!;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _maInitialized;
	private bool _macdReady;
	private decimal _macdMain;
	private decimal _macdSignal;
	private bool _isLongCycle;
	private decimal _cycleBasePrice;
	private int _nextStepIndex;
	private decimal _peakCycleProfit;

	private readonly List<TradeStep> _steps = new();

	/// <summary>
	/// Initializes a new instance of <see cref="ZoneRecoveryAreaStrategy"/>.
	/// </summary>
	public ZoneRecoveryAreaStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Entry Candle", "Timeframe used for entries", "General");

		_monthlyCandleType = Param(nameof(MonthlyCandleType), TimeSpan.FromDays(30).TimeFrame())
			.SetDisplay("Monthly Candle", "Timeframe used for MACD filter", "General");

		_fastMaLength = Param(nameof(FastMaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast moving average period", "Trend Filter");

		_slowMaLength = Param(nameof(SlowMaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow moving average period", "Trend Filter");

		_takeProfitPips = Param(nameof(TakeProfitPips), 150m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Distance to close the cycle in profit", "Risk Management");

		_zoneRecoveryPips = Param(nameof(ZoneRecoveryPips), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Zone Width (pips)", "Distance that triggers hedging trades", "Risk Management");

		_initialVolume = Param(nameof(InitialVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Volume", "Volume of the first trade", "Position Sizing");

		_useVolumeMultiplier = Param(nameof(UseVolumeMultiplier), true)
			.SetDisplay("Use Multiplier", "If true the next trades multiply the previous volume", "Position Sizing");

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Multiplier", "Factor applied when increasing volume", "Position Sizing");

		_volumeIncrement = Param(nameof(VolumeIncrement), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Increment", "Additional volume when multiplier is disabled", "Position Sizing");

		_maxTrades = Param(nameof(MaxTrades), 6)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Maximum number of trades in one cycle", "Risk Management");

		_useMoneyTakeProfit = Param(nameof(UseMoneyTakeProfit), false)
			.SetDisplay("Money Take Profit", "Enable profit target in account currency", "Risk Management");

		_moneyTakeProfit = Param(nameof(MoneyTakeProfit), 40m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit $", "Target profit in account currency", "Risk Management");

		_usePercentTakeProfit = Param(nameof(UsePercentTakeProfit), false)
			.SetDisplay("Percent Take Profit", "Enable profit target based on account balance", "Risk Management");

		_percentTakeProfit = Param(nameof(PercentTakeProfit), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Target profit as a percentage of balance", "Risk Management");

		_enableTrailing = Param(nameof(EnableTrailing), true)
			.SetDisplay("Trailing", "Enable trailing profit lock", "Risk Management");

		_trailingStartProfit = Param(nameof(TrailingStartProfit), 40m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Start", "Profit required before trailing starts", "Risk Management");

		_trailingDrawdown = Param(nameof(TrailingDrawdown), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Step", "Maximum profit giveback before exit", "Risk Management");
	}

	/// <summary>
	/// Working candle type for entries.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Monthly candle type used for the MACD filter.
	/// </summary>
	public DataType MonthlyCandleType
	{
		get => _monthlyCandleType.Value;
		set => _monthlyCandleType.Value = value;
	}

	/// <summary>
	/// Fast moving average period.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Slow moving average period.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Zone width in pips for opening hedging trades.
	/// </summary>
	public decimal ZoneRecoveryPips
	{
		get => _zoneRecoveryPips.Value;
		set => _zoneRecoveryPips.Value = value;
	}

	/// <summary>
	/// Volume of the first trade in a cycle.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Use multiplicative volume scaling.
	/// </summary>
	public bool UseVolumeMultiplier
	{
		get => _useVolumeMultiplier.Value;
		set => _useVolumeMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the previous volume.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	/// <summary>
	/// Additional volume added when multiplier is disabled.
	/// </summary>
	public decimal VolumeIncrement
	{
		get => _volumeIncrement.Value;
		set => _volumeIncrement.Value = value;
	}

	/// <summary>
	/// Maximum number of trades per recovery cycle.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Enable profit target in account currency.
	/// </summary>
	public bool UseMoneyTakeProfit
	{
		get => _useMoneyTakeProfit.Value;
		set => _useMoneyTakeProfit.Value = value;
	}

	/// <summary>
	/// Profit target in account currency.
	/// </summary>
	public decimal MoneyTakeProfit
	{
		get => _moneyTakeProfit.Value;
		set => _moneyTakeProfit.Value = value;
	}

	/// <summary>
	/// Enable profit target based on account percentage.
	/// </summary>
	public bool UsePercentTakeProfit
	{
		get => _usePercentTakeProfit.Value;
		set => _usePercentTakeProfit.Value = value;
	}

	/// <summary>
	/// Profit target as a percentage of account balance.
	/// </summary>
	public decimal PercentTakeProfit
	{
		get => _percentTakeProfit.Value;
		set => _percentTakeProfit.Value = value;
	}

	/// <summary>
	/// Enable trailing profit lock.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Profit level where trailing begins.
	/// </summary>
	public decimal TrailingStartProfit
	{
		get => _trailingStartProfit.Value;
		set => _trailingStartProfit.Value = value;
	}

	/// <summary>
	/// Allowed drawdown from the peak profit before closing.
	/// </summary>
	public decimal TrailingDrawdown
	{
		get => _trailingDrawdown.Value;
		set => _trailingDrawdown.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security != null)
		{
			yield return (Security, CandleType);
			yield return (Security, MonthlyCandleType);
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_steps.Clear();
		_prevFast = 0m;
		_prevSlow = 0m;
		_maInitialized = false;
		_macdReady = false;
		_macdMain = 0m;
		_macdSignal = 0m;
		_isLongCycle = false;
		_cycleBasePrice = 0m;
		_nextStepIndex = 0;
		_peakCycleProfit = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastMa = new SimpleMovingAverage { Length = FastMaLength };
		_slowMa = new SimpleMovingAverage { Length = SlowMaLength };
		_monthlyMacd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 }
			},
			SignalMa = { Length = 9 }
		};

		var mainSubscription = SubscribeCandles(CandleType);
		mainSubscription
			.Bind(_fastMa, _slowMa, ProcessMainCandle)
			.Start();

		var monthlySubscription = SubscribeCandles(MonthlyCandleType, allowBuildFromSmallerTimeFrame: true);
		monthlySubscription
			.BindEx(_monthlyMacd, ProcessMonthlyCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSubscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);

			var macdArea = CreateChartArea();
			if (macdArea != null)
			{
				DrawIndicator(macdArea, _monthlyMacd);
			}
		}
	}

	private void ProcessMonthlyCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFinal)
			return;

		var macd = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macd.Macd is not decimal macdLine || macd.Signal is not decimal signalLine)
			return;

		_macdMain = macdLine;
		_macdSignal = signalLine;
		_macdReady = true;
	}

	private void ProcessMainCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastMa.IsFormed || !_slowMa.IsFormed || !_macdReady)
		{
			_prevFast = fastValue;
			_prevSlow = slowValue;
			return;
		}

		if (!_maInitialized)
		{
			_prevFast = fastValue;
			_prevSlow = slowValue;
			_maInitialized = true;
			return;
		}

		if (_steps.Count > 0)
		{
			HandleExistingCycle(candle.ClosePrice);
		}
		else
		{
			TryStartCycle(candle.ClosePrice);
		}

		_prevFast = fastValue;
		_prevSlow = slowValue;
	}

	private void TryStartCycle(decimal price)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var macdBullish = _macdMain > _macdSignal;
		var macdBearish = _macdMain < _macdSignal;

		var bullishSetup = _prevFast < _prevSlow && macdBullish;
		var bearishSetup = _prevFast > _prevSlow && macdBearish;

		if (bullishSetup)
		{
			StartCycle(true, price);
		}
		else if (bearishSetup)
		{
			StartCycle(false, price);
		}
	}

	private void StartCycle(bool isLong, decimal price)
	{
		if (InitialVolume <= 0m)
			return;

		_steps.Clear();
		_isLongCycle = isLong;
		_cycleBasePrice = price;
		_nextStepIndex = 1;
		_peakCycleProfit = 0m;

		ExecuteOrder(isLong, InitialVolume, price);
	}

	private void HandleExistingCycle(decimal price)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var takeProfitOffset = GetPriceOffset(TakeProfitPips);
		if (takeProfitOffset > 0m)
		{
			if (_isLongCycle && price >= _cycleBasePrice + takeProfitOffset)
			{
				CloseCycle();
				return;
			}

			if (!_isLongCycle && price <= _cycleBasePrice - takeProfitOffset)
			{
				CloseCycle();
				return;
			}
		}

		var cycleProfit = CalculateCycleProfit(price);

		if (UseMoneyTakeProfit && MoneyTakeProfit > 0m && cycleProfit >= MoneyTakeProfit)
		{
			CloseCycle();
			return;
		}

		if (UsePercentTakeProfit && PercentTakeProfit > 0m && TryGetPercentTarget(out var percentTarget) && cycleProfit >= percentTarget)
		{
			CloseCycle();
			return;
		}

		if (EnableTrailing && TrailingStartProfit > 0m && TrailingDrawdown > 0m)
		{
			if (cycleProfit >= TrailingStartProfit)
			{
				_peakCycleProfit = Math.Max(_peakCycleProfit, cycleProfit);
			}

			if (_peakCycleProfit > 0m && cycleProfit <= _peakCycleProfit - TrailingDrawdown)
			{
				CloseCycle();
				return;
			}
		}
		else
		{
			_peakCycleProfit = 0m;
		}

		if (_steps.Count >= MaxTrades)
			return;

		if (!ShouldOpenNextTrade(price))
			return;

		var nextIsBuy = GetNextDirection();
		var volume = GetNextVolume();

		ExecuteOrder(nextIsBuy, volume, price);
		_nextStepIndex++;
	}

	private bool ShouldOpenNextTrade(decimal price)
	{
		var zoneOffset = GetPriceOffset(ZoneRecoveryPips);
		if (zoneOffset <= 0m)
			return false;

		var nextIsBuy = GetNextDirection();

		if (_isLongCycle)
		{
			if (nextIsBuy)
				return price >= _cycleBasePrice;

			return price <= _cycleBasePrice - zoneOffset;
		}

		if (nextIsBuy)
			return price >= _cycleBasePrice + zoneOffset;

		return price <= _cycleBasePrice;
	}

	private bool GetNextDirection()
	{
		var isOddStep = _nextStepIndex % 2 == 1;
		if (_isLongCycle)
			return !isOddStep;

		return isOddStep;
	}

	private decimal GetNextVolume()
	{
		if (_steps.Count == 0)
			return InitialVolume;

		var lastVolume = _steps[^1].Volume;
		decimal nextVolume;

		if (UseVolumeMultiplier)
		{
			nextVolume = lastVolume * VolumeMultiplier;
		}
		else
		{
			nextVolume = lastVolume + VolumeIncrement;
		}

		return nextVolume <= 0m ? InitialVolume : decimal.Round(nextVolume, 6);
	}

	private decimal CalculateCycleProfit(decimal price)
	{
		if (_steps.Count == 0 || Security == null)
			return 0m;

		var priceStep = Security.PriceStep ?? 0m;
		var stepPrice = Security.StepPrice ?? 0m;

		if (priceStep <= 0m || stepPrice <= 0m)
			return 0m;

		decimal pnl = 0m;
		foreach (var step in _steps)
		{
			var diff = price - step.Price;
			var stepsCount = diff / priceStep;
			var direction = step.IsBuy ? 1m : -1m;
			pnl += stepsCount * stepPrice * step.Volume * direction;
		}

		return pnl;
	}

	private bool TryGetPercentTarget(out decimal target)
	{
		target = 0m;
		if (Portfolio == null)
			return false;

		var balance = Portfolio.CurrentValue ?? Portfolio.BeginValue ?? 0m;
		if (balance <= 0m)
			return false;

		target = balance * PercentTakeProfit / 100m;
		return true;
	}

	private decimal GetPriceOffset(decimal pips)
	{
		if (Security == null)
			return 0m;

		var priceStep = Security.PriceStep ?? 0m;
		return priceStep <= 0m ? 0m : pips * priceStep;
	}

	private void ExecuteOrder(bool isBuy, decimal volume, decimal price)
	{
		if (volume <= 0m)
			return;

		if (isBuy)
		{
			BuyMarket(volume);
		}
		else
		{
			SellMarket(volume);
		}

		_steps.Add(new TradeStep(isBuy, price, volume));
	}

	private void CloseCycle()
	{
		if (Position > 0m)
		{
			SellMarket(Position);
		}
		else if (Position < 0m)
		{
			BuyMarket(Math.Abs(Position));
		}

		CancelActiveOrders();
		_steps.Clear();
		_nextStepIndex = 0;
		_cycleBasePrice = 0m;
		_peakCycleProfit = 0m;
	}

	private sealed record TradeStep(bool IsBuy, decimal Price, decimal Volume);
}
