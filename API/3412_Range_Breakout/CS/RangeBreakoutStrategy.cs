using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Logging;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Periodic range breakout strategy converted from the MetaTrader expert advisor "RangeBreakout.mq5".
/// The algorithm prepares breakout levels once per week and enters a single trade when price escapes the range.
/// </summary>
public class RangeBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DayOfWeek> _tradingDay;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<decimal> _pricePercentage;
	private readonly StrategyParam<decimal> _atrPercentage;
	private readonly StrategyParam<decimal> _profitPercentage;
	private readonly StrategyParam<decimal> _lossPercentage;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _hourCandleType;
	private readonly StrategyParam<DataType> _atrCandleType;

	private enum BreakoutPhase
	{
		Standby,
		Setup,
		Trade,
	}

	private BreakoutPhase _phase;
	private DayOfWeek _effectiveTradingDay;
	private AverageTrueRange _atrIndicator = null!;
	private decimal? _atrValue;
	private decimal? _centerPrice;
	private decimal? _upperTrigger;
	private decimal? _lowerTrigger;
	private decimal? _upperTake;
	private decimal? _lowerTake;
	private decimal? _upperStop;
	private decimal? _lowerStop;
	private bool _setupPrepared;
	private decimal _martingaleMultiplier = 1m;
	private decimal _compensationOffset;
	private decimal? _currentBid;
	private decimal? _currentAsk;
	private decimal _entryVolume;
	private Sides? _entryDirection;
	private bool _pendingExitIsLoss;
	private bool _entryOrderPending;
	private bool _exitOrderPending;
	private decimal _lastProfitOffset;
	private decimal _lastLossOffset;

	/// <summary>
	/// Initializes a new instance of <see cref="RangeBreakoutStrategy"/>.
	/// </summary>
	public RangeBreakoutStrategy()
	{
		_tradingDay = Param(nameof(TradingDay), DayOfWeek.Monday)
			.SetDisplay("Trading Day", "Day of the week used to prepare the breakout levels", "Schedule");

		_startHour = Param(nameof(StartHour), 0)
			.SetDisplay("Start Hour", "Hour of day (0-23) used to measure the reference candle", "Schedule")
			.SetCanOptimize(true)
			.SetOptimize(0, 23, 1);

		_pricePercentage = Param(nameof(PricePercentage), 1m)
			.SetDisplay("Price Percentage", "Fallback percentage of the ask price used when ATR data is unavailable", "Breakout");

		_atrPercentage = Param(nameof(AtrPercentage), 100m)
			.SetDisplay("ATR Percentage", "Percentage of the daily ATR added above and below the reference close", "Breakout");

		_profitPercentage = Param(nameof(ProfitPercentage), 100m)
			.SetDisplay("Take Profit Percentage", "Percentage of the range added to the entry to define the take profit", "Risk");

		_lossPercentage = Param(nameof(LossPercentage), 100m)
			.SetDisplay("Stop Loss Percentage", "Percentage of the range added to the entry to define the stop loss", "Risk");

		_baseVolume = Param(nameof(BaseVolume), 0.1m)
			.SetDisplay("Base Volume", "Initial order volume before martingale adjustments", "Risk")
			.SetCanOptimize(true);

		_atrPeriod = Param(nameof(AtrPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Number of daily candles used for the ATR calculation", "Breakout");

		_hourCandleType = Param(nameof(HourCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Hour Candle Type", "Timeframe used to detect the breakout window", "Data");

		_atrCandleType = Param(nameof(AtrCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("ATR Candle Type", "Timeframe feeding the ATR indicator", "Data");
	}

	/// <summary>
	/// Day of week used for the setup.
	/// </summary>
	public DayOfWeek TradingDay
	{
		get => _tradingDay.Value;
		set => _tradingDay.Value = value;
	}

	/// <summary>
	/// Hour of day when the reference candle closes.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Percentage of the ask price used when ATR is unavailable.
	/// </summary>
	public decimal PricePercentage
	{
		get => _pricePercentage.Value;
		set => _pricePercentage.Value = value;
	}

	/// <summary>
	/// Percentage of the daily ATR used to derive the breakout range.
	/// </summary>
	public decimal AtrPercentage
	{
		get => _atrPercentage.Value;
		set => _atrPercentage.Value = value;
	}

	/// <summary>
	/// Percentage of the range converted into take profit distance.
	/// </summary>
	public decimal ProfitPercentage
	{
		get => _profitPercentage.Value;
		set => _profitPercentage.Value = value;
	}

	/// <summary>
	/// Percentage of the range converted into stop loss distance.
	/// </summary>
	public decimal LossPercentage
	{
		get => _lossPercentage.Value;
		set => _lossPercentage.Value = value;
	}

	/// <summary>
	/// Base order volume.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// ATR lookback period in days.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for the hourly breakout logic.
	/// </summary>
	public DataType HourCandleType
	{
		get => _hourCandleType.Value;
		set => _hourCandleType.Value = value;
	}

	/// <summary>
	/// Candle type used to feed the ATR indicator.
	/// </summary>
	public DataType AtrCandleType
	{
		get => _atrCandleType.Value;
		set => _atrCandleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return
		[
			(Security, HourCandleType),
			(Security, AtrCandleType),
			(Security, DataType.Level1),
		];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_phase = BreakoutPhase.Standby;
		_effectiveTradingDay = DayOfWeek.Monday;
		_atrIndicator = null!;
		_atrValue = null;
		_centerPrice = null;
		_upperTrigger = null;
		_lowerTrigger = null;
		_upperTake = null;
		_lowerTake = null;
		_upperStop = null;
		_lowerStop = null;
		_setupPrepared = false;
		_martingaleMultiplier = 1m;
		_compensationOffset = 0m;
		_currentBid = null;
		_currentAsk = null;
		_entryVolume = 0m;
		_entryDirection = null;
		_pendingExitIsLoss = false;
		_entryOrderPending = false;
		_exitOrderPending = false;
		_lastProfitOffset = 0m;
		_lastLossOffset = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_effectiveTradingDay = ResolveTradingDay(TradingDay);
		if (_effectiveTradingDay != TradingDay)
		{
			this.LogInfo("Weekend day selected. Switching to Monday by default.");
		}

		_atrIndicator = new AverageTrueRange
		{
			Length = AtrPeriod,
		};

		var hourlySubscription = SubscribeCandles(HourCandleType);
		hourlySubscription
			.Bind(ProcessHourCandle)
			.Start();

		var atrSubscription = SubscribeCandles(AtrCandleType);
		atrSubscription
			.Bind(_atrIndicator, ProcessAtrCandle)
			.Start();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade?.Order == null)
			return;

		if (_entryDirection == null)
			return;

		if (_entryOrderPending && trade.Order.Side == _entryDirection)
		{
			_entryOrderPending = false;
		}

		if (_exitOrderPending && trade.Order.Side != _entryDirection)
		{
			_exitOrderPending = false;
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position != 0m)
			return;

		if (_entryDirection != null && _setupPrepared)
		{
			FinalizeTrade();
		}

		ResetExecutionState();
	}

	private void ProcessHourCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_phase != BreakoutPhase.Standby)
			return;

		if (candle.OpenTime.DayOfWeek != _effectiveTradingDay)
			return;

		var hour = candle.OpenTime.Hour;
		if (hour != NormalizeHour(StartHour))
			return;

		PrepareSetup(candle.ClosePrice);
	}

	private void ProcessAtrCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (atrValue <= 0m)
			return;

		_atrValue = atrValue;
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid) && bid is decimal bidPrice)
			_currentBid = bidPrice;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask) && ask is decimal askPrice)
			_currentAsk = askPrice;

		TryEnterPosition();
		ManageActivePosition();
	}

	private void TryEnterPosition()
	{
		if (_phase != BreakoutPhase.Setup)
			return;

		if (_entryOrderPending)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0m)
			return;

		var volume = GetTradeVolume();
		if (volume <= 0m)
			return;

		if (_upperTrigger is decimal upper && _currentAsk is decimal ask && ask >= upper)
		{
			BuyMarket(volume);
			_entryDirection = Sides.Buy;
			_entryVolume = volume;
			_entryOrderPending = true;
			_phase = BreakoutPhase.Trade;
			return;
		}

		if (_lowerTrigger is decimal lower && _currentBid is decimal bid && bid <= lower)
		{
			SellMarket(volume);
			_entryDirection = Sides.Sell;
			_entryVolume = volume;
			_entryOrderPending = true;
			_phase = BreakoutPhase.Trade;
		}
	}

	private void ManageActivePosition()
	{
		if (_phase != BreakoutPhase.Trade)
			return;

		if (_exitOrderPending)
			return;

		if (_entryDirection == Sides.Buy && Position > 0m)
		{
			if (_currentBid is decimal bid)
			{
				if (_upperStop is decimal stop && bid <= stop)
				{
					_pendingExitIsLoss = true;
					_exitOrderPending = true;
					SellMarket(Position);
					return;
				}

				if (_upperTake is decimal take && bid >= take)
				{
					_pendingExitIsLoss = false;
					_exitOrderPending = true;
					SellMarket(Position);
				}
			}
		}
		else if (_entryDirection == Sides.Sell && Position < 0m)
		{
			if (_currentAsk is decimal ask)
			{
				if (_lowerStop is decimal stop && ask >= stop)
				{
					_pendingExitIsLoss = true;
					_exitOrderPending = true;
					BuyMarket(-Position);
					return;
				}

				if (_lowerTake is decimal take && ask <= take)
				{
					_pendingExitIsLoss = false;
					_exitOrderPending = true;
					BuyMarket(-Position);
				}
			}
		}
	}

	private void PrepareSetup(decimal referenceClose)
	{
		var center = NormalizeAbsolute(referenceClose);
		_centerPrice = center;

		var range = CalculateRange(center);
		if (range <= 0m)
			return;

		var profitOffset = range * ProfitPercentage / 100m;
		var lossOffset = range * LossPercentage / 100m;

		if (_compensationOffset > 0m)
		{
			profitOffset = _compensationOffset;
			lossOffset += _compensationOffset;
		}

		_lastProfitOffset = profitOffset;
		_lastLossOffset = lossOffset;

		var upperStop = NormalizeAbsolute(center + range);
		var lowerStop = NormalizeAbsolute(center - range);

		_upperTrigger = upperStop;
		_lowerTrigger = lowerStop;
		_upperTake = NormalizeAbsolute(upperStop + profitOffset);
		_lowerTake = NormalizeAbsolute(lowerStop - profitOffset);
		_upperStop = NormalizeAbsolute(upperStop - lossOffset);
		_lowerStop = NormalizeAbsolute(lowerStop + lossOffset);

		_setupPrepared = true;
		_phase = BreakoutPhase.Setup;
		_entryDirection = null;
		_entryVolume = 0m;
		_pendingExitIsLoss = false;
		_entryOrderPending = false;
		_exitOrderPending = false;
	}

	private decimal CalculateRange(decimal centerPrice)
	{
		decimal baseValue;
		if (_atrValue is decimal atr)
		{
			baseValue = atr * AtrPercentage / 100m;
		}
		else
		{
			var ask = _currentAsk ?? centerPrice;
			baseValue = ask * PricePercentage / 100m;
		}

		if (baseValue <= 0m)
			return 0m;

		return NormalizeOffset(centerPrice, baseValue);
	}

	private decimal NormalizeOffset(decimal referencePrice, decimal offset)
	{
		var security = Security;
		if (security == null)
			return offset;

		var normalized = security.ShrinkPrice(referencePrice + offset);
		return normalized - referencePrice;
	}

	private decimal NormalizeAbsolute(decimal price)
	{
		var security = Security;
		return security?.ShrinkPrice(price) ?? price;
	}

	private void FinalizeTrade()
	{
		if (_pendingExitIsLoss)
		{
			_martingaleMultiplier *= 2m;
			if (_martingaleMultiplier <= 0m)
			{
				_martingaleMultiplier = 1m;
			}

			if (_lastLossOffset > 0m)
			{
				_compensationOffset += _lastLossOffset;
			}
		}
		else
		{
			_martingaleMultiplier = 1m;
			_compensationOffset = 0m;
		}

		_pendingExitIsLoss = false;
	}

	private void ResetExecutionState()
	{
		_phase = BreakoutPhase.Standby;
		_setupPrepared = false;
		_centerPrice = null;
		_upperTrigger = null;
		_lowerTrigger = null;
		_upperTake = null;
		_lowerTake = null;
		_upperStop = null;
		_lowerStop = null;
		_entryDirection = null;
		_entryVolume = 0m;
		_entryOrderPending = false;
		_exitOrderPending = false;
	}

	private decimal GetTradeVolume()
	{
		var volume = BaseVolume * _martingaleMultiplier;
		var security = Security;

		if (security == null)
			return volume;

		var minVolume = security.VolumeMin ?? 0m;
		var maxVolume = security.VolumeMax ?? 0m;
		var step = security.VolumeStep ?? 0m;

		if (step > 0m)
		{
			var ratio = volume / step;
			volume = Math.Round(ratio, MidpointRounding.AwayFromZero) * step;
		}

		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		if (maxVolume > 0m && maxVolume > 0m && volume > maxVolume)
			volume = maxVolume;

		return volume;
	}

	private static int NormalizeHour(int hour)
	{
		if (hour < 0)
			return 0;

		if (hour > 23)
			return 23;

		return hour;
	}

	private static DayOfWeek ResolveTradingDay(DayOfWeek configured)
	{
		return configured is DayOfWeek.Saturday or DayOfWeek.Sunday
			? DayOfWeek.Monday
			: configured;
	}
}
