using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Periodic range breakout strategy converted from the "RangeBreakout2" MetaTrader expert advisor.
/// The strategy prepares a breakout range at a scheduled time and trades the range boundaries with configurable money management.
/// </summary>
public class RangeBreakout2Strategy : Strategy
{
	private enum PeriodicityModes
	{
		Weekly,
		Daily,
		NonStop,
	}

	private enum RangeCalculationModes
	{
		Atr,
		Percent,
		Fixed,
	}

	private enum TradeModeOptions
	{
		Stop,
		Limit,
		Random,
	}

	private enum LotManagementModes
	{
		Constant,
		Linear,
		Martingale,
		Fibonacci,
	}

	private enum StrategyPhases
	{
		StandBy,
		Setup,
		Trade,
	}

	private readonly StrategyParam<PeriodicityModes> _periodicity;
	private readonly StrategyParam<DayOfWeek> _dayOfWeek;
	private readonly StrategyParam<int> _hour;
	private readonly StrategyParam<RangeCalculationModes> _rangeMode;
	private readonly StrategyParam<decimal> _atrPercentage;
	private readonly StrategyParam<decimal> _pricePercentage;
	private readonly StrategyParam<int> _fixedRangePoints;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<TradeModeOptions> _tradeMode;
	private readonly StrategyParam<decimal> _rangePercentage;
	private readonly StrategyParam<decimal> _takeProfitPercentage;
	private readonly StrategyParam<decimal> _stopLossPercentage;
	private readonly StrategyParam<LotManagementModes> _lotMode;
	private readonly StrategyParam<decimal> _marginPercentage;
	private readonly StrategyParam<decimal> _lotMultiplier;
	private readonly StrategyParam<decimal> _rangeMultiplier;
	private readonly StrategyParam<DataType> _signalCandleType;
	private readonly StrategyParam<DataType> _atrCandleType;

	private AverageTrueRange _atrIndicator;
	private decimal _atrValue;
	private decimal? _lastAsk;
	private decimal? _lastBid;
	private StrategyPhases _phase = StrategyPhases.StandBy;
	private decimal _setupRange;
	private decimal _setupCenter;
	private decimal _setupHigh;
	private decimal _setupLow;
	private decimal _baseVolume;
	private decimal _currentVolume;
	private int _linearCounter = 1;
	private decimal _fibPrev1;
	private decimal _fibPrev2;
	private decimal _lastTradeResult;
	private decimal _lastRealizedPnL;
	private bool _wasInPosition;
	private readonly Random _random = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="RangeBreakout2Strategy"/> class.
	/// </summary>
	public RangeBreakout2Strategy()
	{
		_periodicity = Param(nameof(Periodicity), PeriodicityModes.Weekly)
			.SetDisplay("Periodicity", "Schedule of the range preparation", "Schedule")
			.SetCanOptimize(true);

		_dayOfWeek = Param(nameof(DayOfWeekSetting), DayOfWeek.Monday)
			.SetDisplay("Day", "Trading day used in weekly mode", "Schedule")
			.SetCanOptimize(true);

		_hour = Param(nameof(Hour), 0)
			.SetDisplay("Hour", "Hour of the day when the range is prepared", "Schedule")
			.SetOptimize(0, 23, 1)
			.SetCanOptimize(true);

		_rangeMode = Param(nameof(RangeMode), RangeCalculationModes.Atr)
			.SetDisplay("Range Mode", "Method used to calculate the raw range", "Range")
			.SetCanOptimize(true);

		_atrPercentage = Param(nameof(AtrPercentage), 50m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Percentage", "Percentage of ATR applied to the range", "Range")
			.SetCanOptimize(true);

		_pricePercentage = Param(nameof(PricePercentage), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Price Percentage", "Percentage of the ask price used as range", "Range")
			.SetCanOptimize(true);

		_fixedRangePoints = Param(nameof(FixedRangePoints), 1000)
			.SetGreaterThanZero()
			.SetDisplay("Fixed Range Points", "Fixed range expressed in price steps", "Range")
			.SetCanOptimize(true);

		_atrLength = Param(nameof(AtrLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "Number of candles used for ATR calculation", "Range")
			.SetCanOptimize(true);

		_tradeMode = Param(nameof(TradeMode), TradeModeOptions.Stop)
			.SetDisplay("Trade Mode", "Order type used on range breakout", "Trading")
			.SetCanOptimize(true);

		_rangePercentage = Param(nameof(RangePercentage), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Range Percentage", "Percentage of the raw range used to set breakout levels", "Trading")
			.SetCanOptimize(true);

		_takeProfitPercentage = Param(nameof(TakeProfitPercentage), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Take-Profit Percentage", "Percentage of the range used for take-profit", "Trading")
			.SetCanOptimize(true);

		_stopLossPercentage = Param(nameof(StopLossPercentage), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Stop-Loss Percentage", "Percentage of the range used for stop-loss", "Trading")
			.SetCanOptimize(true);

		_lotMode = Param(nameof(LotMode), LotManagementModes.Martingale)
			.SetDisplay("Lot Mode", "Money management scheme", "Risk")
			.SetCanOptimize(true);

		_marginPercentage = Param(nameof(MarginPercentage), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Margin Percentage", "Percentage of free capital reserved for the base volume", "Risk")
			.SetCanOptimize(true);

		_lotMultiplier = Param(nameof(LotMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Multiplier", "Multiplier applied in martingale or scaling logic", "Risk")
			.SetCanOptimize(true);

		_rangeMultiplier = Param(nameof(RangeMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Range Multiplier", "Multiplier applied to the take-profit range after a loss", "Risk")
			.SetCanOptimize(true);

		_signalCandleType = Param(nameof(SignalCandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Signal Candle", "Candle type that drives the schedule", "Data");

		_atrCandleType = Param(nameof(AtrCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("ATR Candle", "Candle type used for ATR calculation", "Data");
	}

	/// <summary>
	/// Range preparation schedule.
	/// </summary>
	public PeriodicityModes Periodicity
	{
		get => _periodicity.Value;
		set => _periodicity.Value = value;
	}

	/// <summary>
	/// Day of week used in weekly mode.
	/// </summary>
	public DayOfWeek DayOfWeekSetting
	{
		get => _dayOfWeek.Value;
		set => _dayOfWeek.Value = value;
	}

	/// <summary>
	/// Hour of the day when the range is prepared.
	/// </summary>
	public int Hour
	{
		get => _hour.Value;
		set => _hour.Value = value;
	}

	/// <summary>
	/// Range calculation method.
	/// </summary>
	public RangeCalculationModes RangeMode
	{
		get => _rangeMode.Value;
		set => _rangeMode.Value = value;
	}

	/// <summary>
	/// Percentage of ATR used for the range.
	/// </summary>
	public decimal AtrPercentage
	{
		get => _atrPercentage.Value;
		set => _atrPercentage.Value = value;
	}

	/// <summary>
	/// Percentage of price used for the range.
	/// </summary>
	public decimal PricePercentage
	{
		get => _pricePercentage.Value;
		set => _pricePercentage.Value = value;
	}

	/// <summary>
	/// Fixed range size in price steps.
	/// </summary>
	public int FixedRangePoints
	{
		get => _fixedRangePoints.Value;
		set => _fixedRangePoints.Value = value;
	}

	/// <summary>
	/// Length of ATR in candles.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Trading mode (stop, limit or random).
	/// </summary>
	public TradeModeOptions TradeMode
	{
		get => _tradeMode.Value;
		set => _tradeMode.Value = value;
	}

	/// <summary>
	/// Percentage of the raw range applied to breakout levels.
	/// </summary>
	public decimal RangePercentage
	{
		get => _rangePercentage.Value;
		set => _rangePercentage.Value = value;
	}

	/// <summary>
	/// Percentage of the range used as take-profit distance.
	/// </summary>
	public decimal TakeProfitPercentage
	{
		get => _takeProfitPercentage.Value;
		set => _takeProfitPercentage.Value = value;
	}

	/// <summary>
	/// Percentage of the base range used as stop-loss distance.
	/// </summary>
	public decimal StopLossPercentage
	{
		get => _stopLossPercentage.Value;
		set => _stopLossPercentage.Value = value;
	}

	/// <summary>
	/// Money management scheme.
	/// </summary>
	public LotManagementModes LotMode
	{
		get => _lotMode.Value;
		set => _lotMode.Value = value;
	}

	/// <summary>
	/// Percentage of available capital reserved for the base lot.
	/// </summary>
	public decimal MarginPercentage
	{
		get => _marginPercentage.Value;
		set => _marginPercentage.Value = value;
	}

	/// <summary>
	/// Multiplier applied by martingale-style scaling.
	/// </summary>
	public decimal LotMultiplier
	{
		get => _lotMultiplier.Value;
		set => _lotMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the take-profit range after a losing trade.
	/// </summary>
	public decimal RangeMultiplier
	{
		get => _rangeMultiplier.Value;
		set => _rangeMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type that drives schedule checks.
	/// </summary>
	public DataType SignalCandleType
	{
		get => _signalCandleType.Value;
		set => _signalCandleType.Value = value;
	}

	/// <summary>
	/// Candle type used for ATR calculation.
	/// </summary>
	public DataType AtrCandleType
	{
		get => _atrCandleType.Value;
		set => _atrCandleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null)
			yield break;

		yield return (Security, SignalCandleType);

		if (RangeMode == RangeCalculationModes.Atr)
			yield return (Security, AtrCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_atrIndicator = null;
		_atrValue = 0m;
		_lastAsk = null;
		_lastBid = null;
		_phase = StrategyPhases.StandBy;
		_setupRange = 0m;
		_setupCenter = 0m;
		_setupHigh = 0m;
		_setupLow = 0m;
		_baseVolume = 0m;
		_currentVolume = 0m;
		_linearCounter = 1;
		_fibPrev1 = 0m;
		_fibPrev2 = 0m;
		_lastTradeResult = 0m;
		_lastRealizedPnL = 0m;
		_wasInPosition = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security?.PriceStep == null || Security.PriceStep <= 0m)
			throw new InvalidOperationException("Security price step must be defined.");

		StartProtection();

		_lastRealizedPnL = PnLManager?.RealizedPnL ?? 0m;

		var signalSubscription = SubscribeCandles(SignalCandleType);
		signalSubscription
			.WhenNew(ProcessSignalCandle)
			.Start();

		if (RangeMode == RangeCalculationModes.Atr)
		{
			_atrIndicator = new AverageTrueRange { Length = AtrLength };

			var atrSubscription = SubscribeCandles(AtrCandleType);
			atrSubscription
				.Bind(_atrIndicator, ProcessAtr)
				.Start();
		}

		SubscribeOrderBook()
			.Bind(ProcessDepth)
			.Start();
	}

	private void ProcessSignalCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_phase == StrategyPhases.StandBy && ShouldStartSetup(candle.CloseTime))
			TryPrepareSetup();
	}

	private void ProcessAtr(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_atrValue = atrValue;
	}

	private void ProcessDepth(IOrderBookMessage depth)
	{
		var bestBid = depth.GetBestBid()?.Price;
		if (bestBid != null)
			_lastBid = bestBid;

		var bestAsk = depth.GetBestAsk()?.Price;
		if (bestAsk != null)
			_lastAsk = bestAsk;

		if (_phase == StrategyPhases.Setup)
			TryExecuteEntries();
	}

	private bool ShouldStartSetup(DateTimeOffset time)
	{
		var triggerHour = Hour + 1;
		if (triggerHour >= 23)
			triggerHour = 0;

		return Periodicity switch
		{
			PeriodicityModes.Weekly => time.DayOfWeek == DayOfWeekSetting && time.Hour == triggerHour,
			PeriodicityModes.Daily => time.Hour == triggerHour,
			PeriodicityModes.NonStop => true,
			_ => false,
		};
	}

	private void TryPrepareSetup()
	{
		var ask = _lastAsk ?? Security?.LastPrice;
		if (ask == null || ask <= 0m)
			return;

		var rawRange = CalculateRawRange(ask.Value);
		if (rawRange <= 0m)
			return;

		_setupRange = rawRange;
		_setupCenter = ask.Value;
		var offset = _setupRange * RangePercentage / 100m;
		_setupHigh = _setupCenter + offset;
		_setupLow = _setupCenter - offset;

		_phase = StrategyPhases.Setup;
	}

	private decimal CalculateRawRange(decimal referencePrice)
	{
		return RangeMode switch
		{
			RangeCalculationModes.Atr => CalculateAtrRange(referencePrice),
			RangeCalculationModes.Percent => referencePrice * PricePercentage / 100m,
			RangeCalculationModes.Fixed => (Security?.PriceStep ?? 0m) * FixedRangePoints,
			_ => 0m,
		};
	}

	private decimal CalculateAtrRange(decimal referencePrice)
	{
		if (_atrIndicator == null || !_atrIndicator.IsFormed)
		{
			// Fallback replicating the MetaTrader behaviour when ATR data is not available.
			return referencePrice / 100m;
		}

		return _atrValue * AtrPercentage / 100m;
	}

	private void TryExecuteEntries()
	{
		if (_lastAsk == null || _lastBid == null)
			return;

		if (_phase != StrategyPhases.Setup)
			return;

		var ask = _lastAsk.Value;
		var bid = _lastBid.Value;

		if (ask > _setupHigh)
		{
			ExecuteBreakout(isUpper: true, ask, bid);
			return;
		}

		if (bid < _setupLow)
			ExecuteBreakout(isUpper: false, ask, bid);
	}

	private void ExecuteBreakout(bool isUpper, decimal ask, decimal bid)
	{
		var mode = ResolveTradeMode();

		var volume = PrepareTradeVolume();
		if (volume <= 0m)
		{
			ResetToStandBy();
			return;
		}

		Order order;
		decimal referencePrice;

		if (isUpper)
		{
			if (mode == TradeModeOptions.Stop)
			{
				referencePrice = ask;
				order = BuyMarket(volume);
			}
			else
			{
				referencePrice = bid;
				order = SellMarket(volume);
			}
		}
		else
		{
			if (mode == TradeModeOptions.Stop)
			{
				referencePrice = bid;
				order = SellMarket(volume);
			}
			else
			{
				referencePrice = ask;
				order = BuyMarket(volume);
			}
		}

		if (order == null)
		{
			ResetToStandBy();
			return;
		}

		ApplyProtections(order.Side, referencePrice, volume);
		_phase = StrategyPhases.Trade;
	}

	private TradeModeOptions ResolveTradeMode()
	{
		if (TradeMode != TradeModeOptions.Random)
			return TradeMode;

		return _random.Next(2) == 0 ? TradeModeOptions.Stop : TradeModeOptions.Limit;
	}

	private decimal PrepareTradeVolume()
	{
		return LotMode switch
		{
			LotManagementModes.Constant => RecalculateBaseVolume(),
			LotManagementModes.Linear => PrepareLinearVolume(),
			LotManagementModes.Martingale => PrepareMartingaleVolume(),
			LotManagementModes.Fibonacci => PrepareFibonacciVolume(),
			_ => RecalculateBaseVolume(),
		};
	}

	private decimal PrepareLinearVolume()
	{
		if (_lastTradeResult >= 0m || _currentVolume <= 0m)
		{
			_linearCounter = 1;
			var baseVolume = RecalculateBaseVolume();
			_currentVolume = baseVolume;
			return baseVolume;
		}

		_linearCounter++;
		var scaled = NormalizeVolume(_baseVolume * _linearCounter);
		_currentVolume = scaled;
		return scaled;
	}

	private decimal PrepareMartingaleVolume()
	{
		if (_lastTradeResult >= 0m || _currentVolume <= 0m)
		{
			var baseVolume = RecalculateBaseVolume();
			_currentVolume = baseVolume;
			return baseVolume;
		}

		var volume = NormalizeVolume(_currentVolume * LotMultiplier);
		_currentVolume = volume;
		return volume;
	}

	private decimal PrepareFibonacciVolume()
	{
		if (_lastTradeResult >= 0m || _currentVolume <= 0m)
		{
			var baseVolume = RecalculateBaseVolume();
			_fibPrev1 = baseVolume;
			_fibPrev2 = baseVolume;
			_currentVolume = baseVolume;
			return baseVolume;
		}

		var next = NormalizeVolume(_fibPrev1 + _fibPrev2);
		_fibPrev1 = _fibPrev2;
		_fibPrev2 = next;
		_currentVolume = next;
		return next;
	}

	private decimal RecalculateBaseVolume()
	{
		var security = Security;
		if (security == null)
			return 0m;

		var step = security.VolumeStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		var minVolume = security.MinVolume ?? step;
		var maxVolume = security.MaxVolume ?? decimal.MaxValue;

		decimal volume;

		if (Portfolio != null)
		{
			var currentValue = Portfolio.CurrentValue ?? Portfolio.BeginValue ?? 0m;
			var blocked = Portfolio.BlockedValue ?? 0m;
			var freeCapital = currentValue - blocked;
			var reserve = freeCapital * MarginPercentage / 100m;

			var price = _lastAsk ?? _lastBid ?? security.LastPrice ?? 0m;
			if (price <= 0m)
				price = security.ClosePrice ?? security.OpenPrice ?? security.PriceStep ?? 1m;

			var contractCost = price * step;
			if (contractCost <= 0m)
				volume = minVolume;
			else
				volume = reserve / contractCost;
		}
		else
		{
			volume = minVolume;
		}

		if (volume <= 0m)
			volume = minVolume;

		var normalized = NormalizeVolume(volume, minVolume, maxVolume, step);
		_baseVolume = normalized;
		_currentVolume = normalized;
		return normalized;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		var minVolume = security.MinVolume ?? step;
		var maxVolume = security.MaxVolume ?? decimal.MaxValue;

		return NormalizeVolume(volume, minVolume, maxVolume, step);
	}

	private static decimal NormalizeVolume(decimal volume, decimal minVolume, decimal maxVolume, decimal step)
	{
		if (step <= 0m)
			step = 1m;

		if (minVolume <= 0m)
			minVolume = step;

		var steps = Math.Round(volume / step, 0, MidpointRounding.AwayFromZero);
		if (steps <= 0m)
			steps = 1m;

		var normalized = steps * step;

		if (normalized < minVolume)
			normalized = minVolume;

		if (maxVolume > 0m && normalized > maxVolume)
			normalized = maxVolume;

		return normalized;
	}

	private void ApplyProtections(Sides direction, decimal referencePrice, decimal volume)
	{
		var security = Security;
		if (security?.PriceStep is not decimal priceStep || priceStep <= 0m)
			return;

		if (_setupRange <= 0m)
			return;

		var baseRange = _setupRange;
		var tpRange = baseRange;

		if (_lastTradeResult < 0m && RangeMultiplier > 0m)
			tpRange *= RangeMultiplier;

		var stopDistance = baseRange * StopLossPercentage / 100m;
		var takeProfitDistance = tpRange * TakeProfitPercentage / 100m;

		var resultingPosition = direction == Sides.Buy ? Position + volume : Position - volume;

		if (takeProfitDistance > 0m)
		{
			var tpPoints = takeProfitDistance / priceStep;
			SetTakeProfit(tpPoints, referencePrice, resultingPosition);
		}

		if (stopDistance > 0m)
		{
			var slPoints = stopDistance / priceStep;
			SetStopLoss(slPoints, referencePrice, resultingPosition);
		}
	}

	private void ResetToStandBy()
	{
		_phase = StrategyPhases.StandBy;
		_setupRange = 0m;
		_setupCenter = 0m;
		_setupHigh = 0m;
		_setupLow = 0m;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order.State != OrderStates.Done)
			return;

		if (Position != 0m)
		{
			_wasInPosition = true;
			return;
		}

		if (!_wasInPosition)
			return;

		_wasInPosition = false;

		var realized = PnLManager?.RealizedPnL ?? PnL;
		var result = realized - _lastRealizedPnL;

		if (result != 0m)
		{
			_lastTradeResult = result;
			_lastRealizedPnL = realized;
		}

		ResetToStandBy();
	}
}

