namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Port of the "MA2CCI" MetaTrader expert advisor.
/// Combines a dual moving average crossover with a CCI zero-line filter and ATR based protective stops.
/// </summary>
public class Ma2CciStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _maxRiskPerThousand;
	private readonly StrategyParam<decimal> _decreaseFactor;

	private SimpleMovingAverage _fastMa = null!;
	private SimpleMovingAverage _slowMa = null!;
	private CommodityChannelIndex _cci = null!;
	private AverageTrueRange _atr = null!;

	private decimal? _prevFast;
	private decimal? _prevSlow;
	private decimal? _prevPrevFast;
	private decimal? _prevPrevSlow;
	private decimal? _prevCci;
	private decimal? _prevPrevCci;
	private decimal? _lastAtr;
	private decimal? _stopPrice;
	private decimal? _entryPrice;
	private decimal? _pendingPnl;

	private int _consecutiveLosses;

	/// <summary>
	/// Initializes a new instance of the <see cref="Ma2CciStrategy"/> class.
	/// </summary>
	public Ma2CciStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Primary timeframe", "Candle series used for signal calculations.", "General")
			.SetCanOptimize(true);

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Base volume", "Fallback volume used when risk based sizing is not available.", "Trading")
			.SetCanOptimize(true);

		_fastMaPeriod = Param(nameof(FastMaPeriod), 4)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA", "Length of the fast simple moving average.", "Indicators")
			.SetCanOptimize(true);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA", "Length of the slow simple moving average.", "Indicators")
			.SetCanOptimize(true);

		_cciPeriod = Param(nameof(CciPeriod), 4)
			.SetGreaterThanZero()
			.SetDisplay("CCI period", "Number of bars used by the commodity channel index filter.", "Indicators")
			.SetCanOptimize(true);

		_atrPeriod = Param(nameof(AtrPeriod), 4)
			.SetGreaterThanZero()
			.SetDisplay("ATR period", "Length of the ATR used for protective stop placement.", "Indicators")
			.SetCanOptimize(true);

		_maxRiskPerThousand = Param(nameof(MaxRiskPerThousand), 0.02m)
			.SetNotNegative()
			.SetDisplay("Risk fraction", "Fraction of free capital allocated per trade expressed per 1000 units.", "Risk")
			.SetCanOptimize(true);

		_decreaseFactor = Param(nameof(DecreaseFactor), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Decrease factor", "Divisor applied to reduce size after consecutive losses.", "Risk")
			.SetCanOptimize(true);
	}

	/// <summary>
	/// Candle data series used for all indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Minimal volume used when risk based sizing cannot be calculated.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Length of the fast moving average.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Length of the slow moving average.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Period of the commodity channel index filter.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Period of the ATR used for stop calculation.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Risk capital allocation expressed per 1000 units of free capital.
	/// </summary>
	public decimal MaxRiskPerThousand
	{
		get => _maxRiskPerThousand.Value;
		set => _maxRiskPerThousand.Value = value;
	}

	/// <summary>
	/// Factor that controls volume reduction after losing streaks.
	/// </summary>
	public decimal DecreaseFactor
	{
		get => _decreaseFactor.Value;
		set => _decreaseFactor.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevFast = null;
		_prevSlow = null;
		_prevPrevFast = null;
		_prevPrevSlow = null;
		_prevCci = null;
		_prevPrevCci = null;
		_lastAtr = null;
		_stopPrice = null;
		_entryPrice = null;
		_pendingPnl = null;
		_consecutiveLosses = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Prepare indicator instances for all required calculations.
		_fastMa = new SimpleMovingAverage
		{
			Length = FastMaPeriod,
			CandlePrice = CandlePrice.Close
		};

		_slowMa = new SimpleMovingAverage
		{
			Length = SlowMaPeriod,
			CandlePrice = CandlePrice.Close
		};

		_cci = new CommodityChannelIndex
		{
			Length = CciPeriod
		};

		_atr = new AverageTrueRange
		{
			Length = AtrPeriod
		};

		// Subscribe to the selected candle series and bind indicators in a single callback.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastMa, _slowMa, _cci, _atr, ProcessCandle)
			.Start();

		// Optional visualization for manual validation.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _cci);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			// Reset state when the position is fully closed.
			if (_pendingPnl.HasValue)
			{
				if (_pendingPnl.Value < 0m)
				{
					_consecutiveLosses++;
				}
				else if (_pendingPnl.Value > 0m)
				{
					_consecutiveLosses = 0;
				}
			}

			_pendingPnl = null;
			_stopPrice = null;
			_entryPrice = null;
			return;
		}

		// Store the average entry price whenever exposure increases.
		if ((delta > 0m && Position > 0m) || (delta < 0m && Position < 0m))
		{
			_entryPrice = PositionPrice;

			if (_lastAtr is { } atrValue && atrValue > 0m)
			{
				_stopPrice = Position > 0m
					? PositionPrice - atrValue
					: PositionPrice + atrValue;
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastMa, decimal slowMa, decimal cciValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Update most recent ATR for stop calculation when a new bar closes.
		_lastAtr = atrValue;

		// Manage protective stop exits before evaluating new signals.
		if (_stopPrice is { } stop && _entryPrice is { } entry)
		{
			var currentPosition = Position;
			if (currentPosition > 0m && candle.LowPrice <= stop)
			{
				_pendingPnl = (stop - entry) * currentPosition;
				ClosePosition();
				return;
			}

			if (currentPosition < 0m && candle.HighPrice >= stop)
			{
				_pendingPnl = (entry - stop) * Math.Abs(currentPosition);
				ClosePosition();
				return;
			}
		}

		var hasPrev = _prevFast.HasValue && _prevSlow.HasValue && _prevPrevFast.HasValue && _prevPrevSlow.HasValue;
		var hasCciHistory = _prevCci.HasValue && _prevPrevCci.HasValue;

		var crossUp = hasPrev && fastMa > slowMa && _prevFast!.Value <= _prevSlow!.Value;
		var crossDown = hasPrev && fastMa < slowMa && _prevFast!.Value >= _prevSlow!.Value;
		var cciBull = hasCciHistory && _prevCci!.Value > 0m && _prevPrevCci!.Value <= 0m;
		var cciBear = hasCciHistory && _prevCci!.Value < 0m && _prevPrevCci!.Value >= 0m;

		// Exit long positions on bearish crossover.
		if (Position > 0m && crossDown)
		{
			var position = Position;
			var exitPrice = candle.ClosePrice;
			var entryPrice = _entryPrice ?? exitPrice;
			_pendingPnl = (exitPrice - entryPrice) * position;
			ClosePosition();
			return;
		}

		// Exit short positions on bullish crossover.
		if (Position < 0m && crossUp)
		{
			var position = Position;
			var exitPrice = candle.ClosePrice;
			var entryPrice = _entryPrice ?? exitPrice;
			_pendingPnl = (entryPrice - exitPrice) * Math.Abs(position);
			ClosePosition();
			return;
		}

		if (Position == 0m && IsFormedAndOnlineAndAllowTrading())
		{
			if (crossUp && cciBull)
			{
				var volume = CalculateOrderVolume();
				if (volume > 0m)
				{
					_pendingPnl = null;
					BuyMarket(volume);
				}
			}
			else if (crossDown && cciBear)
			{
				var volume = CalculateOrderVolume();
				if (volume > 0m)
				{
					_pendingPnl = null;
					SellMarket(volume);
				}
			}
		}

		// Shift indicator history to evaluate next bar conditions.
		_prevPrevFast = _prevFast;
		_prevPrevSlow = _prevSlow;
		_prevPrevCci = _prevCci;
		_prevFast = fastMa;
		_prevSlow = slowMa;
		_prevCci = cciValue;
	}

	private decimal CalculateOrderVolume()
	{
		var volume = OrderVolume;

		var portfolio = Portfolio;
		if (portfolio != null)
		{
			var freeMargin = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
			if (freeMargin > 0m && MaxRiskPerThousand > 0m)
			{
				var riskVolume = freeMargin * MaxRiskPerThousand / 1000m;
				if (riskVolume > 0m)
				{
					volume = Math.Max(volume, riskVolume);
				}
			}
		}

		if (_consecutiveLosses > 1 && DecreaseFactor > 0m)
		{
			var reduction = volume * _consecutiveLosses / DecreaseFactor;
			volume -= reduction;
			if (volume < 0m)
				volume = 0m;
		}

		if (volume <= 0m)
			return 0m;

		return AdjustVolume(volume);
	}

	private decimal AdjustVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
			volume = steps * step;
		}

		var minVolume = security.MinVolume ?? 0m;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		var maxVolume = security.MaxVolume ?? 0m;
		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume;

		return volume;
	}
}
