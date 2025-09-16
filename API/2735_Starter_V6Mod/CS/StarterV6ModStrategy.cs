using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the Starter_v6mod Expert Advisor using the high-level StockSharp API.
/// </summary>
public class StarterV6ModStrategy : Strategy
{
	private readonly StrategyParam<bool> _useManualVolume;
	private readonly StrategyParam<decimal> _manualVolume;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<decimal> _decreaseFactor;
	private readonly StrategyParam<int> _maxLossesPerDay;
	private readonly StrategyParam<decimal> _equityCutoff;
	private readonly StrategyParam<int> _maxOpenTrades;
	private readonly StrategyParam<int> _gridStepPips;
	private readonly StrategyParam<int> _longEmaPeriod;
	private readonly StrategyParam<int> _shortEmaPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _angleThreshold;
	private readonly StrategyParam<decimal> _levelUp;
	private readonly StrategyParam<decimal> _levelDown;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _longEma = default!;
	private ExponentialMovingAverage _shortEma = default!;
	private CommodityChannelIndex _cci = default!;
	private RelativeStrengthIndex _laguerreProxy = default!;

	private decimal? _prevLongEma;
	private decimal? _prevShortEma;

	private decimal? _lowestBuyPrice;
	private decimal? _highestSellPrice;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longHighestPrice;
	private decimal? _shortLowestPrice;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;

	private decimal _longVolume;
	private decimal _shortVolume;
	private int _longTradeCount;
	private int _shortTradeCount;

	private decimal _pipSize = 1m;
	private DateTime _currentDay;
	private int _lossesToday;

	/// <summary>
	/// Use manual volume instead of risk calculation.
	/// </summary>
	public bool UseManualVolume
	{
		get => _useManualVolume.Value;
		set => _useManualVolume.Value = value;
	}

	/// <summary>
	/// Manual volume for each new entry.
	/// </summary>
	public decimal ManualVolume
	{
		get => _manualVolume.Value;
		set => _manualVolume.Value = value;
	}

	/// <summary>
	/// Risk percentage used when position sizing is automatic.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
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
	/// Additional distance required before the trailing stop starts to follow the price.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Multiplier used to reduce the position size after losses.
	/// </summary>
	public decimal DecreaseFactor
	{
		get => _decreaseFactor.Value;
		set => _decreaseFactor.Value = value;
	}

	/// <summary>
	/// Maximum number of losing trades allowed per day.
	/// </summary>
	public int MaxLossesPerDay
	{
		get => _maxLossesPerDay.Value;
		set => _maxLossesPerDay.Value = value;
	}

	/// <summary>
	/// Equity threshold below which the strategy stops opening new trades.
	/// </summary>
	public decimal EquityCutoff
	{
		get => _equityCutoff.Value;
		set => _equityCutoff.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneously opened grid positions.
	/// </summary>
	public int MaxOpenTrades
	{
		get => _maxOpenTrades.Value;
		set => _maxOpenTrades.Value = value;
	}

	/// <summary>
	/// Grid step in pips used when stacking positions.
	/// </summary>
	public int GridStepPips
	{
		get => _gridStepPips.Value;
		set => _gridStepPips.Value = value;
	}

	/// <summary>
	/// Period for the slow EMA trend filter.
	/// </summary>
	public int LongEmaPeriod
	{
		get => _longEmaPeriod.Value;
		set => _longEmaPeriod.Value = value;
	}

	/// <summary>
	/// Period for the fast EMA trend filter.
	/// </summary>
	public int ShortEmaPeriod
	{
		get => _shortEmaPeriod.Value;
		set => _shortEmaPeriod.Value = value;
	}

	/// <summary>
	/// Period for the CCI momentum filter.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Threshold in ticks for the EMA spread trend detector.
	/// </summary>
	public decimal AngleThreshold
	{
		get => _angleThreshold.Value;
		set => _angleThreshold.Value = value;
	}

	/// <summary>
	/// Upper Laguerre RSI level.
	/// </summary>
	public decimal LevelUp
	{
		get => _levelUp.Value;
		set => _levelUp.Value = value;
	}

	/// <summary>
	/// Lower Laguerre RSI level.
	/// </summary>
	public decimal LevelDown
	{
		get => _levelDown.Value;
		set => _levelDown.Value = value;
	}

	/// <summary>
	/// Candle data type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="StarterV6ModStrategy"/> class.
	/// </summary>
	public StarterV6ModStrategy()
	{
		_useManualVolume = Param(nameof(UseManualVolume), false)
		.SetDisplay("Manual Volume", "Use manual volume instead of risk-based sizing", "Money Management");

		_manualVolume = Param(nameof(ManualVolume), 1m)
		.SetRange(0.01m, 100m)
		.SetDisplay("Volume", "Manual volume per trade", "Money Management")
		.SetCanOptimize(true);

		_riskPercent = Param(nameof(RiskPercent), 5m)
		.SetRange(0.5m, 20m)
		.SetDisplay("Risk %", "Risk percentage when auto-sizing trades", "Money Management")
		.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 35)
		.SetRange(0, 500)
		.SetDisplay("Stop Loss", "Stop-loss distance in pips", "Risk Management");

		_takeProfitPips = Param(nameof(TakeProfitPips), 10)
		.SetRange(0, 500)
		.SetDisplay("Take Profit", "Take-profit distance in pips", "Risk Management");

		_trailingStopPips = Param(nameof(TrailingStopPips), 0)
		.SetRange(0, 500)
		.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk Management");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
		.SetRange(0, 500)
		.SetDisplay("Trailing Step", "Additional distance before trailing activates", "Risk Management");

		_decreaseFactor = Param(nameof(DecreaseFactor), 1.6m)
		.SetRange(1m, 10m)
		.SetDisplay("Decrease Factor", "Volume reduction factor after losses", "Money Management");

		_maxLossesPerDay = Param(nameof(MaxLossesPerDay), 3)
		.SetRange(0, 20)
		.SetDisplay("Daily Loss Limit", "Maximum number of losses per day", "Risk Management");

		_equityCutoff = Param(nameof(EquityCutoff), 800m)
		.SetRange(0m, 1_000_000m)
		.SetDisplay("Equity Cutoff", "Stop trading if equity drops below this value", "Risk Management");

		_maxOpenTrades = Param(nameof(MaxOpenTrades), 10)
		.SetRange(1, 100)
		.SetDisplay("Max Trades", "Maximum simultaneous grid positions", "General");

		_gridStepPips = Param(nameof(GridStepPips), 30)
		.SetRange(0, 500)
		.SetDisplay("Grid Step", "Minimum pip distance between stacked entries", "General");

		_longEmaPeriod = Param(nameof(LongEmaPeriod), 120)
		.SetRange(10, 400)
		.SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
		.SetCanOptimize(true);

		_shortEmaPeriod = Param(nameof(ShortEmaPeriod), 40)
		.SetRange(5, 200)
		.SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
		.SetCanOptimize(true);

		_cciPeriod = Param(nameof(CciPeriod), 14)
		.SetRange(5, 100)
		.SetDisplay("CCI Period", "CCI indicator length", "Indicators")
		.SetCanOptimize(true);

		_angleThreshold = Param(nameof(AngleThreshold), 3m)
		.SetRange(0m, 50m)
		.SetDisplay("Angle Threshold", "EMA spread threshold measured in ticks", "Indicators");

		_levelUp = Param(nameof(LevelUp), 0.85m)
		.SetRange(0.1m, 1m)
		.SetDisplay("Laguerre Up", "Upper Laguerre RSI level", "Indicators");

		_levelDown = Param(nameof(LevelDown), 0.15m)
		.SetRange(0m, 0.9m)
		.SetDisplay("Laguerre Down", "Lower Laguerre RSI level", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for analysis", "General");
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

		_prevLongEma = null;
		_prevShortEma = null;
		_lowestBuyPrice = null;
		_highestSellPrice = null;
		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longHighestPrice = null;
		_shortLowestPrice = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_longVolume = 0m;
		_shortVolume = 0m;
		_longTradeCount = 0;
		_shortTradeCount = 0;
		_currentDay = default;
		_lossesToday = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var priceStep = Security?.PriceStep ?? 1m;
		var decimals = Security?.Decimals ?? 0;
		var multiplier = decimals is 3 or 5 ? 10m : 1m;
		_pipSize = priceStep * multiplier;

		_longEma = new ExponentialMovingAverage { Length = LongEmaPeriod };
		_shortEma = new ExponentialMovingAverage { Length = ShortEmaPeriod };
		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_laguerreProxy = new RelativeStrengthIndex { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_longEma, _shortEma, _cci, _laguerreProxy, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _longEma);
			DrawIndicator(area, _shortEma);
			DrawIndicator(area, _cci);
			DrawIndicator(area, _laguerreProxy);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal longEmaValue, decimal shortEmaValue, decimal cciValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_longEma.IsFormed || !_shortEma.IsFormed || !_cci.IsFormed || !_laguerreProxy.IsFormed)
		{
			_prevLongEma = longEmaValue;
			_prevShortEma = shortEmaValue;
			return;
		}

		var laguerre = rsiValue / 100m;
		var time = candle.OpenTime.LocalDateTime;
		var today = time.Date;

		if (today != _currentDay)
		{
			_currentDay = today;
			_lossesToday = 0;
		}

		var dontTrade = time.DayOfWeek == DayOfWeek.Friday && time.Hour >= 18;
		var forceExit = time.DayOfWeek == DayOfWeek.Friday && time.Hour >= 20;

		ManagePositions(candle, laguerre, forceExit);

		var equity = Portfolio?.CurrentValue ?? Portfolio?.BeginValue;
		var equityAllowed = EquityCutoff <= 0 || equity == null || equity >= EquityCutoff;
		var lossLimitReached = MaxLossesPerDay > 0 && _lossesToday >= MaxLossesPerDay;

		if (!IsFormedAndOnlineAndAllowTrading() || !equityAllowed || lossLimitReached || forceExit)
		{
			_prevLongEma = longEmaValue;
			_prevShortEma = shortEmaValue;
			return;
		}

		var angleThresholdValue = AngleThreshold * (Security?.PriceStep ?? 1m);
		var emaAngle = longEmaValue - shortEmaValue;

		var hasLong = _longTradeCount > 0 || Position > 0;
		var hasShort = _shortTradeCount > 0 || Position < 0;

		var canOpenBuy = hasLong;
		var canOpenSell = hasShort;

		if (Math.Abs(emaAngle) <= angleThresholdValue)
		{
			canOpenBuy = false;
			canOpenSell = false;
		}
		else if (emaAngle < -angleThresholdValue && !hasShort)
		{
			canOpenBuy = true;
			canOpenSell = false;
		}
		else if (emaAngle > angleThresholdValue && !hasLong)
		{
			canOpenBuy = false;
			canOpenSell = true;
		}

		if (MaxOpenTrades > 0)
		{
			if (_longTradeCount >= MaxOpenTrades)
				canOpenBuy = false;

			if (_shortTradeCount >= MaxOpenTrades)
				canOpenSell = false;
		}

		if (dontTrade)
		{
			canOpenBuy = false;
			canOpenSell = false;
		}

		var gridStep = GridStepPips * _pipSize;

		if (canOpenBuy && _prevLongEma.HasValue && _prevShortEma.HasValue)
		{
			var buySignal = laguerre < LevelDown && longEmaValue < _prevLongEma.Value && shortEmaValue < _prevShortEma.Value && cciValue < 0m;

			if (buySignal)
			{
				if (_longTradeCount > 0 && GridStepPips > 0 && _lowestBuyPrice.HasValue)
				{
					var distance = _lowestBuyPrice.Value - candle.ClosePrice;
					if (distance < gridStep)
						buySignal = false;
				}

				if (buySignal)
					OpenLong(candle.ClosePrice);
			}
		}

		if (canOpenSell && _prevLongEma.HasValue && _prevShortEma.HasValue)
		{
			var sellSignal = laguerre > LevelUp && longEmaValue > _prevLongEma.Value && shortEmaValue > _prevShortEma.Value && cciValue > 0m;

			if (sellSignal)
			{
				if (_shortTradeCount > 0 && GridStepPips > 0 && _highestSellPrice.HasValue)
				{
					var distance = candle.ClosePrice - _highestSellPrice.Value;
					if (distance < gridStep)
						sellSignal = false;
				}

				if (sellSignal)
					OpenShort(candle.ClosePrice);
			}
		}

		_prevLongEma = longEmaValue;
		_prevShortEma = shortEmaValue;
	}

	private void ManagePositions(ICandleMessage candle, decimal laguerre, bool forceExit)
	{
		if (_longTradeCount > 0)
		{
			_longHighestPrice = Math.Max(_longHighestPrice ?? candle.ClosePrice, candle.HighPrice);

			var stopDistance = StopLossPips * _pipSize;
			var takeDistance = TakeProfitPips * _pipSize;
			var trailingDistance = TrailingStopPips * _pipSize;
			var trailingStep = TrailingStepPips * _pipSize;

			if (forceExit)
			{
				CloseLong(candle.ClosePrice);
				return;
			}

			if (StopLossPips > 0 && _longEntryPrice.HasValue && candle.LowPrice <= _longEntryPrice.Value - stopDistance)
			{
				CloseLong(_longEntryPrice.Value - stopDistance);
				return;
			}

			if (TakeProfitPips > 0 && _longEntryPrice.HasValue && candle.HighPrice >= _longEntryPrice.Value + takeDistance)
			{
				CloseLong(_longEntryPrice.Value + takeDistance);
				return;
			}

			if (laguerre > LevelUp)
			{
				CloseLong(candle.ClosePrice);
				return;
			}

			if (TrailingStopPips > 0 && _longEntryPrice.HasValue)
			{
				if (_longHighestPrice.HasValue && _longHighestPrice.Value - _longEntryPrice.Value > trailingDistance + trailingStep)
				{
					var newStop = _longHighestPrice.Value - trailingDistance;
					if (!_longTrailingStop.HasValue || newStop > _longTrailingStop.Value)
						_longTrailingStop = newStop;
				}

				if (_longTrailingStop.HasValue && candle.LowPrice <= _longTrailingStop.Value)
				{
					CloseLong(_longTrailingStop.Value);
					return;
				}
			}
		}

		if (_shortTradeCount > 0)
		{
			_shortLowestPrice = Math.Min(_shortLowestPrice ?? candle.ClosePrice, candle.LowPrice);

			var stopDistance = StopLossPips * _pipSize;
			var takeDistance = TakeProfitPips * _pipSize;
			var trailingDistance = TrailingStopPips * _pipSize;
			var trailingStep = TrailingStepPips * _pipSize;

			if (forceExit)
			{
				CloseShort(candle.ClosePrice);
				return;
			}

			if (StopLossPips > 0 && _shortEntryPrice.HasValue && candle.HighPrice >= _shortEntryPrice.Value + stopDistance)
			{
				CloseShort(_shortEntryPrice.Value + stopDistance);
				return;
			}

			if (TakeProfitPips > 0 && _shortEntryPrice.HasValue && candle.LowPrice <= _shortEntryPrice.Value - takeDistance)
			{
				CloseShort(_shortEntryPrice.Value - takeDistance);
				return;
			}

			if (laguerre < LevelDown)
			{
				CloseShort(candle.ClosePrice);
				return;
			}

			if (TrailingStopPips > 0 && _shortEntryPrice.HasValue)
			{
				if (_shortLowestPrice.HasValue && _shortEntryPrice.Value - _shortLowestPrice.Value > trailingDistance + trailingStep)
				{
					var newStop = _shortLowestPrice.Value + trailingDistance;
					if (!_shortTrailingStop.HasValue || newStop < _shortTrailingStop.Value)
						_shortTrailingStop = newStop;
				}

				if (_shortTrailingStop.HasValue && candle.HighPrice >= _shortTrailingStop.Value)
				{
					CloseShort(_shortTrailingStop.Value);
					return;
				}
			}
		}
	}

	private void OpenLong(decimal price)
	{
		var baseVolume = DetermineTradeVolume();
		if (baseVolume <= 0)
			return;

		var orderVolume = baseVolume + Math.Max(0m, -Position);
		if (orderVolume <= 0)
			return;

		BuyMarket(orderVolume);

		if (_shortTradeCount > 0)
		{
			EvaluateTradeResult(false, price);
			ResetShortState();
		}

		var previousVolume = _longVolume;
		_longVolume += baseVolume;
		_longTradeCount++;

		if (previousVolume <= 0 || !_longEntryPrice.HasValue)
			_longEntryPrice = price;
		else
			_longEntryPrice = ((previousVolume * _longEntryPrice.Value) + baseVolume * price) / _longVolume;

		if (!_lowestBuyPrice.HasValue || price < _lowestBuyPrice.Value)
			_lowestBuyPrice = price;

		_longHighestPrice = Math.Max(_longHighestPrice ?? price, price);
		_longTrailingStop = null;
	}

	private void OpenShort(decimal price)
	{
		var baseVolume = DetermineTradeVolume();
		if (baseVolume <= 0)
			return;

		var orderVolume = baseVolume + Math.Max(0m, Position);
		if (orderVolume <= 0)
			return;

		SellMarket(orderVolume);

		if (_longTradeCount > 0)
		{
			EvaluateTradeResult(true, price);
			ResetLongState();
		}

		var previousVolume = _shortVolume;
		_shortVolume += baseVolume;
		_shortTradeCount++;

		if (previousVolume <= 0 || !_shortEntryPrice.HasValue)
			_shortEntryPrice = price;
		else
			_shortEntryPrice = ((previousVolume * _shortEntryPrice.Value) + baseVolume * price) / _shortVolume;

		if (!_highestSellPrice.HasValue || price > _highestSellPrice.Value)
			_highestSellPrice = price;

		_shortLowestPrice = Math.Min(_shortLowestPrice ?? price, price);
		_shortTrailingStop = null;
	}

	private decimal DetermineTradeVolume()
	{
		decimal volume;

		if (UseManualVolume || StopLossPips <= 0)
		{
			volume = ManualVolume;
		}
		else
		{
			var equity = Portfolio?.CurrentValue ?? Portfolio?.BeginValue;
			if (equity == null || equity <= 0)
				volume = ManualVolume;
			else
			{
				var riskValue = equity.Value * RiskPercent / 100m;
				var stopDistance = StopLossPips * _pipSize;
				volume = stopDistance > 0 ? riskValue / stopDistance : ManualVolume;
			}
		}

		if (_lossesToday > 0 && DecreaseFactor > 1m)
		{
			var factor = (decimal)Math.Pow((double)DecreaseFactor, _lossesToday);
			if (factor > 0)
				volume /= factor;
		}

		return NormalizeVolume(volume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0)
			return 0m;

		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep ?? 1m;
		if (step > 0)
			volume = step * Math.Floor(volume / step);

		var minVolume = security.VolumeMin ?? step;
		if (volume < minVolume)
			return 0m;

		var maxVolume = security.VolumeMax;
		if (maxVolume != null && volume > maxVolume.Value)
			volume = maxVolume.Value;

		return volume;
	}

	private void CloseLong(decimal exitPrice)
	{
		var volumeToClose = Math.Abs(Position);
		if (volumeToClose <= 0)
			volumeToClose = _longVolume;

		if (volumeToClose <= 0)
		{
			ResetLongState();
			return;
		}

		SellMarket(volumeToClose);
		EvaluateTradeResult(true, exitPrice);
		ResetLongState();
	}

	private void CloseShort(decimal exitPrice)
	{
		var volumeToClose = Math.Abs(Position);
		if (volumeToClose <= 0)
			volumeToClose = _shortVolume;

		if (volumeToClose <= 0)
		{
			ResetShortState();
			return;
		}

		BuyMarket(volumeToClose);
		EvaluateTradeResult(false, exitPrice);
		ResetShortState();
	}

	private void EvaluateTradeResult(bool isLong, decimal exitPrice)
	{
		if (isLong)
		{
			if (!_longEntryPrice.HasValue || _longVolume <= 0)
				return;

			var pnl = (exitPrice - _longEntryPrice.Value) * _longVolume;
			if (pnl < 0)
				_lossesToday++;
		}
		else
		{
			if (!_shortEntryPrice.HasValue || _shortVolume <= 0)
				return;

			var pnl = (_shortEntryPrice.Value - exitPrice) * _shortVolume;
			if (pnl < 0)
				_lossesToday++;
		}
	}

	private void ResetLongState()
	{
		_longVolume = 0m;
		_longTradeCount = 0;
		_longEntryPrice = null;
		_lowestBuyPrice = null;
		_longHighestPrice = null;
		_longTrailingStop = null;
	}

	private void ResetShortState()
	{
		_shortVolume = 0m;
		_shortTradeCount = 0;
		_shortEntryPrice = null;
		_highestSellPrice = null;
		_shortLowestPrice = null;
		_shortTrailingStop = null;
	}
}
