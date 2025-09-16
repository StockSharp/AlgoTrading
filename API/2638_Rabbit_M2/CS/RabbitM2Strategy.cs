using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Rabbit M2 strategy converted from the MetaTrader 5 expert advisor.
/// Combines EMA trend gating, Williams %R momentum and adaptive position sizing.
/// </summary>
public class RabbitM2Strategy : Strategy
{
	private readonly StrategyParam<int> _cciSellLevel;
	private readonly StrategyParam<int> _cciBuyLevel;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _donchianPeriod;
	private readonly StrategyParam<int> _maxOpenPositions;
	private readonly StrategyParam<decimal> _bigWinTarget;
	private readonly StrategyParam<decimal> _volumeStep;
	private readonly StrategyParam<int> _wprPeriod;
	private readonly StrategyParam<int> _fastEmaPeriod;
	private readonly StrategyParam<int> _slowEmaPeriod;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _tradeVolume;
	private decimal _bigWinThreshold;
	private decimal _stopLossDistance;
	private decimal _takeProfitDistance;
	private decimal? _previousWpr;
	private decimal? _previousDonchianUpper;
	private decimal? _previousDonchianLower;
	private bool _buyAllowed;
	private bool _sellAllowed;
	private decimal _lastRealizedPnL;
	private decimal _currentStop;
	private decimal _currentTake;

	/// <summary>
	/// Initializes a new instance of the <see cref="RabbitM2Strategy"/> class.
	/// </summary>
	public RabbitM2Strategy()
	{
		_cciSellLevel = Param(nameof(CciSellLevel), 101)
			.SetDisplay("CCI Sell Level", "CCI threshold confirming short signals", "CCI")
			.SetCanOptimize(true);

		_cciBuyLevel = Param(nameof(CciBuyLevel), 99)
			.SetDisplay("CCI Buy Level", "CCI threshold confirming long signals", "CCI")
			.SetCanOptimize(true);

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Lookback for the Commodity Channel Index", "CCI")
			.SetCanOptimize(true);

		_donchianPeriod = Param(nameof(DonchianPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("Donchian Period", "Lookback used for breakout exits", "Donchian")
			.SetCanOptimize(true);

		_maxOpenPositions = Param(nameof(MaxOpenPositions), 1)
			.SetGreaterThanZero()
			.SetDisplay("Max Open Positions", "Maximum net exposure in base volume multiples", "Risk")
			.SetCanOptimize(true);

		_bigWinTarget = Param(nameof(BigWinTarget), 1.50m)
			.SetGreaterThanZero()
			.SetDisplay("Big Win Target", "Profit needed before increasing position size", "Money Management")
			.SetCanOptimize(true);

		_volumeStep = Param(nameof(VolumeStep), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Volume Step", "Increment applied to the base volume after a big win", "Money Management")
			.SetCanOptimize(true);

		_wprPeriod = Param(nameof(WprPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Williams %R Period", "Length of the Williams %R oscillator", "Momentum")
			.SetCanOptimize(true);

		_fastEmaPeriod = Param(nameof(FastEmaPeriod), 40)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Period", "Fast EMA period on the hourly trend feed", "Trend Filter")
			.SetCanOptimize(true);

		_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 80)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Period", "Slow EMA period on the hourly trend feed", "Trend Filter")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit (pips)", "Distance from entry to take profit", "Risk")
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (pips)", "Distance from entry to stop loss", "Risk")
			.SetCanOptimize(true);

		_initialVolume = Param(nameof(InitialVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Volume", "Starting base order size", "Money Management")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Primary Candle Type", "Timeframe for CCI, Williams %R and Donchian", "General");
	}

	/// <summary>
	/// Minimum CCI value required to confirm a short setup.
	/// </summary>
	public int CciSellLevel
	{
		get => _cciSellLevel.Value;
		set => _cciSellLevel.Value = value;
	}

	/// <summary>
	/// Maximum CCI value required to confirm a long setup.
	/// </summary>
	public int CciBuyLevel
	{
		get => _cciBuyLevel.Value;
		set => _cciBuyLevel.Value = value;
	}

	/// <summary>
	/// CCI calculation period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Donchian channel lookback length.
	/// </summary>
	public int DonchianPeriod
	{
		get => _donchianPeriod.Value;
		set => _donchianPeriod.Value = value;
	}

	/// <summary>
	/// Maximum number of net position multiples that can be opened.
	/// </summary>
	public int MaxOpenPositions
	{
		get => _maxOpenPositions.Value;
		set => _maxOpenPositions.Value = value;
	}

	/// <summary>
	/// Profit threshold that triggers a volume increase.
	/// </summary>
	public decimal BigWinTarget
	{
		get => _bigWinTarget.Value;
		set => _bigWinTarget.Value = value;
	}

	/// <summary>
	/// Volume increment applied after a qualifying win.
	/// </summary>
	public decimal VolumeStep
	{
		get => _volumeStep.Value;
		set => _volumeStep.Value = value;
	}

	/// <summary>
	/// Williams %R period.
	/// </summary>
	public int WprPeriod
	{
		get => _wprPeriod.Value;
		set => _wprPeriod.Value = value;
	}

	/// <summary>
	/// Fast EMA period used for the hourly trend filter.
	/// </summary>
	public int FastEmaPeriod
	{
		get => _fastEmaPeriod.Value;
		set => _fastEmaPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period used for the hourly trend filter.
	/// </summary>
	public int SlowEmaPeriod
	{
		get => _slowEmaPeriod.Value;
		set => _slowEmaPeriod.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Base order size before scaling logic is applied.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Primary candle type used for CCI, Williams %R and Donchian calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
		yield return (Security, TimeSpan.FromHours(1).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_tradeVolume = 0m;
		_bigWinThreshold = 0m;
		_stopLossDistance = 0m;
		_takeProfitDistance = 0m;
		_previousWpr = null;
		_previousDonchianUpper = null;
		_previousDonchianLower = null;
		_buyAllowed = false;
		_sellAllowed = false;
		_lastRealizedPnL = 0m;
		_currentStop = 0m;
		_currentTake = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tradeVolume = InitialVolume;
		_bigWinThreshold = BigWinTarget;
		EnsureVolumeBoundaries();
		_lastRealizedPnL = PnL;

		var pipSize = GetPipSize();
		_stopLossDistance = StopLossPips * pipSize;
		_takeProfitDistance = TakeProfitPips * pipSize;

		// Initialize indicators that operate on the primary timeframe.
		var wpr = new WilliamsR { Length = WprPeriod };
		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var donchian = new DonchianChannels { Length = DonchianPeriod };

		// Initialize hourly EMA indicators for trend gating.
		var emaFast = new ExponentialMovingAverage { Length = FastEmaPeriod };
		var emaSlow = new ExponentialMovingAverage { Length = SlowEmaPeriod };

		var trendSubscription = SubscribeCandles(TimeSpan.FromHours(1).TimeFrame());
		trendSubscription
			.Bind(emaFast, emaSlow, ProcessTrend)
			.Start();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(wpr, cci, donchian, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaFast);
			DrawIndicator(area, emaSlow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessTrend(ICandleMessage candle, decimal emaFast, decimal emaSlow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (emaFast < emaSlow)
		{
			_sellAllowed = true;
			_buyAllowed = false;
			CloseLongPosition("EMA trend flipped to bearish mode");
		}
		else if (emaFast > emaSlow)
		{
			_buyAllowed = true;
			_sellAllowed = false;
			CloseShortPosition("EMA trend flipped to bullish mode");
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue wprValue, IIndicatorValue cciValue, IIndicatorValue donchianValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!wprValue.IsFinal || !cciValue.IsFinal || !donchianValue.IsFinal)
			return;

		var donchian = (DonchianChannelsValue)donchianValue;
		if (donchian.UpperBand is not decimal upperBand || donchian.LowerBand is not decimal lowerBand)
			return;

		// Always evaluate protective exits before considering new entries.
		ManageExistingPosition(candle, upperBand, lowerBand);

		var wprCurrent = wprValue.ToDecimal();
		var wprPrevious = _previousWpr;
		var cciCurrent = cciValue.ToDecimal();

		if (wprCurrent == 0m)
			wprCurrent = -1m;

		_previousWpr = wprCurrent;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_tradeVolume <= 0m)
			return;

		if (wprPrevious is null)
			return;

		var wprLag = wprPrevious.Value;
		if (wprLag == 0m)
			wprLag = -1m;

		// Check for short entries when the short regime is active.
		var canAddShort = Position <= 0m && Math.Abs(Position) < _tradeVolume * MaxOpenPositions;
		if (_sellAllowed && canAddShort && wprCurrent < -20m && wprLag > -20m && wprLag < 0m && cciCurrent > CciSellLevel)
		{
			var volume = _tradeVolume + Math.Max(0m, Position);
			if (volume > 0m)
			{
				SellMarket(volume);
				_currentStop = candle.ClosePrice + _stopLossDistance;
				_currentTake = candle.ClosePrice - _takeProfitDistance;
			}
			return;
		}

		// Check for long entries when the long regime is active.
		var canAddLong = Position >= 0m && Math.Abs(Position) < _tradeVolume * MaxOpenPositions;
		if (_buyAllowed && canAddLong && wprCurrent > -80m && wprLag < -80m && wprLag < 0m && cciCurrent < CciBuyLevel)
		{
			var volume = _tradeVolume + Math.Max(0m, -Position);
			if (volume > 0m)
			{
				BuyMarket(volume);
				_currentStop = candle.ClosePrice - _stopLossDistance;
				_currentTake = candle.ClosePrice + _takeProfitDistance;
			}
		}
	}

	private void ManageExistingPosition(ICandleMessage candle, decimal currentUpper, decimal currentLower)
	{
		if (Position > 0m)
		{
			// Protect long positions with take profit, stop loss and Donchian breakout checks.
			if (_currentTake > 0m && candle.HighPrice >= _currentTake)
			{
				CloseLongPosition("Take profit reached");
			}
			else if (_currentStop > 0m && candle.LowPrice <= _currentStop)
			{
				CloseLongPosition("Stop loss reached");
			}
			else if (_previousDonchianLower is decimal previousLower && candle.ClosePrice < previousLower)
			{
				CloseLongPosition("Donchian breakout against long position");
			}
		}
		else if (Position < 0m)
		{
			// Protect short positions using the same logic mirrored for shorts.
			if (_currentTake > 0m && candle.LowPrice <= _currentTake)
			{
				CloseShortPosition("Take profit reached");
			}
			else if (_currentStop > 0m && candle.HighPrice >= _currentStop)
			{
				CloseShortPosition("Stop loss reached");
			}
			else if (_previousDonchianUpper is decimal previousUpper && candle.ClosePrice > previousUpper)
			{
				CloseShortPosition("Donchian breakout against short position");
			}
		}

		_previousDonchianUpper = currentUpper;
		_previousDonchianLower = currentLower;
	}

	private void CloseLongPosition(string reason)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		SellMarket(volume);
		_currentStop = 0m;
		_currentTake = 0m;
		LogInfo($"Closing long position: {reason}.");
	}

	private void CloseShortPosition(string reason)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		BuyMarket(volume);
		_currentStop = 0m;
		_currentTake = 0m;
		LogInfo($"Closing short position: {reason}.");
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var realizedChange = PnL - _lastRealizedPnL;
		_lastRealizedPnL = PnL;

		// Increase the base volume after sufficiently profitable exits.
		if (realizedChange > _bigWinThreshold)
		{
			_tradeVolume += VolumeStep;
			EnsureVolumeBoundaries();
			_bigWinThreshold *= 2m;
		}

		if (Math.Abs(Position) == 0m)
		{
			_currentStop = 0m;
			_currentTake = 0m;
		}
	}

	private void EnsureVolumeBoundaries()
	{
		var step = Security?.VolumeStep;
		if (step.HasValue && step.Value > 0m)
		{
			var steps = Math.Floor(_tradeVolume / step.Value);
			_tradeVolume = steps * step.Value;
		}

		var max = Security?.VolumeMax;
		if (max.HasValue && max.Value > 0m && _tradeVolume > max.Value)
			_tradeVolume = max.Value;

		var min = Security?.VolumeMin;
		if (min.HasValue && min.Value > 0m && _tradeVolume < min.Value)
			_tradeVolume = 0m;

		Volume = _tradeVolume;
	}

	private decimal GetPipSize()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
			priceStep = 0.0001m;

		var decimals = Security?.Decimals;
		if (decimals == 3 || decimals == 5)
			priceStep *= 10m;

		return priceStep;
	}
}
