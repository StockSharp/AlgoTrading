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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Wave Power EA grid strategy converted from the original MQL4 expert.
/// Implements stochastic-based entries with adaptive averaging and protective exits.
/// </summary>
public class WavePowerEAStrategy : Strategy
{
	/// <summary>
	/// Entry logic selector matching the original advisor modes.
	/// </summary>
	public enum EntryModes
	{
		/// <summary>
		/// Signal based on the Stochastic oscillator cross above/below extreme levels.
		/// </summary>
		Stochastic = 16,

		/// <summary>
		/// MACD slope filter that follows the sign of the main line momentum.
		/// </summary>
		MacdSlope = 17,

		/// <summary>
		/// Commodity Channel Index threshold breakout.
		/// </summary>
		CciLevels = 9,

		/// <summary>
		/// Awesome Oscillator breakout from historical extremes.
		/// </summary>
		AwesomeBreakout = 10,

		/// <summary>
		/// Fast versus slow moving average cross confirmed by RSI direction.
		/// </summary>
		RsiMa = 8,

		/// <summary>
		/// Hourly simple moving average fan confirming trend alignment.
		/// </summary>
		SmaTrend = 1,
	}

	private readonly StrategyParam<EntryModes> _entryMode;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _initialVolume;
	private readonly StrategyParam<decimal> _gridStepPips;
	private readonly StrategyParam<int> _maxOrders;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<bool> _secureProfitProtection;
	private readonly StrategyParam<int> _ordersToProtect;
	private readonly StrategyParam<decimal> _reboundProfitPrimary;
	private readonly StrategyParam<decimal> _reboundProfitSecondary;
	private readonly StrategyParam<bool> _lossProtection;
	private readonly StrategyParam<decimal> _lossThreshold;
	private readonly StrategyParam<bool> _reverseCondition;
	private readonly StrategyParam<bool> _tradeOnFriday;
	private readonly StrategyParam<int> _ordersTimeAliveSeconds;
	private readonly StrategyParam<decimal> _trendSlopeThreshold;

	private StochasticOscillator _stochastic = null!;
	private MovingAverageConvergenceDivergence _macd = null!;
	private CommodityChannelIndex _cci = null!;
	private AwesomeOscillator _ao = null!;
	private SimpleMovingAverage _fastMa = null!;
	private SimpleMovingAverage _slowMa = null!;
	private RelativeStrengthIndex _rsi = null!;
	private SimpleMovingAverage _smaShort = null!;
	private SimpleMovingAverage _smaMedium = null!;
	private SimpleMovingAverage _smaLong = null!;
	private SimpleMovingAverage _smaLongest = null!;

	private decimal _pipSize;
	private EntryDirections _gridDirection = EntryDirections.None;
	private readonly List<PositionEntry> _entries = new();
	private decimal? _lastOrderPrice;
	private DateTimeOffset? _lastOrderTime;
	private decimal? _takeProfitPrice;
	private decimal? _stopLossPrice;
	private decimal _totalVolume;
	private int _openOrderCount;
	private decimal? _prevStochMain;
	private decimal? _prevStochSignal;
	private decimal? _prevMacd;
	private decimal? _prevFastMa;
	private decimal? _prevSlowMa;
	private decimal? _prevSmaLong;
	private decimal? _prevSmaLongest;
	private decimal _aoHigh;
	private decimal _aoLow;
	private bool _aoInitialized;

	private enum EntryDirections
	{
		None = 0,
		Buy = 1,
		Sell = -1,
	}

	private readonly record struct PositionEntry(decimal Price, decimal Volume, Sides Side);

	/// <summary>
	/// Initializes a new instance of <see cref="WavePowerEAStrategy"/>.
	/// </summary>
	public WavePowerEAStrategy()
	{
		_entryMode = Param(nameof(EntryLogic), EntryModes.Stochastic)
			.SetDisplay("Entry Mode", "Indicator logic used for the first order", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for indicator calculations", "General");

		_initialVolume = Param(nameof(InitialVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Volume", "Volume used for the first market order", "Money Management");

		_gridStepPips = Param(nameof(GridStepPips), 24m)
			.SetGreaterThanZero()
			.SetDisplay("Grid Step (pips)", "Minimal distance between consecutive averaging orders", "Money Management");

		_maxOrders = Param(nameof(MaxOrders), 12)
			.SetGreaterThanZero()
			.SetDisplay("Max Orders", "Maximum number of simultaneously open orders", "Money Management");

		_takeProfitPips = Param(nameof(TakeProfitPips), 32m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Distance for shared profit target", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Distance for defensive stop loss", "Risk");

		_multiplier = Param(nameof(Multiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Volume multiplier for each additional grid layer", "Money Management");

		_secureProfitProtection = Param(nameof(SecureProfitProtection), false)
			.SetDisplay("Secure Profit", "Close all orders after reaching rebound profit", "Protection");

		_ordersToProtect = Param(nameof(OrdersToProtect), 4)
			.SetNotNegative()
			.SetDisplay("Orders To Protect", "Enable rebound protection after this amount of orders", "Protection");

		_reboundProfitPrimary = Param(nameof(ReboundProfitPrimary), 28m)
			.SetNotNegative()
			.SetDisplay("Rebound Profit 1", "Profit threshold (in pips) for the first protection stage", "Protection");

		_reboundProfitSecondary = Param(nameof(ReboundProfitSecondary), 18m)
			.SetNotNegative()
			.SetDisplay("Rebound Profit 2", "Profit threshold (in pips) after exceeding protected orders", "Protection");

		_lossProtection = Param(nameof(LossProtection), false)
			.SetDisplay("Loss Protection", "Close the basket if floating loss breaches the threshold", "Protection");

		_lossThreshold = Param(nameof(LossThreshold), 0m)
			.SetNotNegative()
			.SetDisplay("Loss Threshold (pips)", "Loss per lot that triggers the protective close", "Protection");

		_reverseCondition = Param(nameof(ReverseCondition), false)
			.SetDisplay("Reverse Signal", "Invert buy/sell signals", "Signals");

		_tradeOnFriday = Param(nameof(TradeOnFriday), true)
			.SetDisplay("Trade On Friday", "Allow opening new orders on Fridays", "General");

		_ordersTimeAliveSeconds = Param(nameof(OrdersTimeAliveSeconds), 0)
			.SetNotNegative()
			.SetDisplay("Orders Lifetime (s)", "Close basket if last entry is older than the limit", "Protection");

		_trendSlopeThreshold = Param(nameof(TrendSlopeThreshold), 0.0001m)
			.SetNotNegative()
			.SetDisplay("Trend Slope", "Minimal SMA slope difference required for trend entries", "Signals");
	}

	/// <summary>
	/// Entry logic selected by the trader.
	/// </summary>
	public EntryModes EntryLogic
	{
		get => _entryMode.Value;
		set => _entryMode.Value = value;
	}

	/// <summary>
	/// Candle type that feeds all indicators.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Volume used for the first market entry.
	/// </summary>
	public decimal InitialVolume
	{
		get => _initialVolume.Value;
		set => _initialVolume.Value = value;
	}

	/// <summary>
	/// Distance in pips between grid layers.
	/// </summary>
	public decimal GridStepPips
	{
		get => _gridStepPips.Value;
		set => _gridStepPips.Value = value;
	}

	/// <summary>
	/// Maximum number of simultaneously open orders.
	/// </summary>
	public int MaxOrders
	{
		get => _maxOrders.Value;
		set => _maxOrders.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Volume multiplier applied to the next averaging order.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Enables the rebound profit protection.
	/// </summary>
	public bool SecureProfitProtection
	{
		get => _secureProfitProtection.Value;
		set => _secureProfitProtection.Value = value;
	}

	/// <summary>
	/// Number of orders required before activating rebound profit control.
	/// </summary>
	public int OrdersToProtect
	{
		get => _ordersToProtect.Value;
		set => _ordersToProtect.Value = value;
	}

	/// <summary>
	/// Primary rebound profit in pips.
	/// </summary>
	public decimal ReboundProfitPrimary
	{
		get => _reboundProfitPrimary.Value;
		set => _reboundProfitPrimary.Value = value;
	}

	/// <summary>
	/// Secondary rebound profit in pips after exceeding the protected order count.
	/// </summary>
	public decimal ReboundProfitSecondary
	{
		get => _reboundProfitSecondary.Value;
		set => _reboundProfitSecondary.Value = value;
	}

	/// <summary>
	/// Enables the floating loss protection.
	/// </summary>
	public bool LossProtection
	{
		get => _lossProtection.Value;
		set => _lossProtection.Value = value;
	}

	/// <summary>
	/// Loss threshold in pips per lot required to liquidate the basket.
	/// </summary>
	public decimal LossThreshold
	{
		get => _lossThreshold.Value;
		set => _lossThreshold.Value = value;
	}

	/// <summary>
	/// Inverts the trading signals.
	/// </summary>
	public bool ReverseCondition
	{
		get => _reverseCondition.Value;
		set => _reverseCondition.Value = value;
	}

	/// <summary>
	/// Allows opening new orders on Fridays.
	/// </summary>
	public bool TradeOnFriday
	{
		get => _tradeOnFriday.Value;
		set => _tradeOnFriday.Value = value;
	}

	/// <summary>
	/// Basket lifetime expressed in seconds.
	/// </summary>
	public int OrdersTimeAliveSeconds
	{
		get => _ordersTimeAliveSeconds.Value;
		set => _ordersTimeAliveSeconds.Value = value;
	}

	/// <summary>
	/// Minimal slope difference required for the SMA trend logic.
	/// </summary>
	public decimal TrendSlopeThreshold
	{
		get => _trendSlopeThreshold.Value;
		set => _trendSlopeThreshold.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetGridState();
		_prevStochMain = null;
		_prevStochSignal = null;
		_prevMacd = null;
		_prevFastMa = null;
		_prevSlowMa = null;
		_prevSmaLong = null;
		_prevSmaLongest = null;
		_aoHigh = 0m;
		_aoLow = 0m;
		_aoInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = Security?.Step ?? Security?.PriceStep ?? 0.0001m;

		_stochastic = new StochasticOscillator
		{
			Length = 14,
			K = { Length = 3 },
			D = { Length = 3 }
		};

		_macd = new MovingAverageConvergenceDivergence
		{
			Fast = 14,
			Slow = 26,
			Signal = 9
		};

		_cci = new CommodityChannelIndex { Length = 15 };
		_ao = new AwesomeOscillator { ShortPeriod = 5, LongPeriod = 34 };
		_fastMa = new SimpleMovingAverage { Length = 3 };
		_slowMa = new SimpleMovingAverage { Length = 8 };
		_rsi = new RelativeStrengthIndex { Length = 14 };
		_smaShort = new SimpleMovingAverage { Length = 15 };
		_smaMedium = new SimpleMovingAverage { Length = 20 };
		_smaLong = new SimpleMovingAverage { Length = 25 };
		_smaLongest = new SimpleMovingAverage { Length = 50 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(
				_stochastic,
				_macd,
				_cci,
				_ao,
				_fastMa,
				_slowMa,
				_smaShort,
				_smaMedium,
				_smaLong,
				_smaLongest,
				_rsi,
				ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(
		ICandleMessage candle,
		IIndicatorValue stochValue,
		IIndicatorValue macdValue,
		IIndicatorValue cciValue,
		IIndicatorValue aoValue,
		IIndicatorValue fastMaValue,
		IIndicatorValue slowMaValue,
		IIndicatorValue smaShortValue,
		IIndicatorValue smaMediumValue,
		IIndicatorValue smaLongValue,
		IIndicatorValue smaLongestValue,
		IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!stochValue.IsFinal || !macdValue.IsFinal || !cciValue.IsFinal || !aoValue.IsFinal ||
			!fastMaValue.IsFinal || !slowMaValue.IsFinal || !smaShortValue.IsFinal || !smaMediumValue.IsFinal ||
			!smaLongValue.IsFinal || !smaLongestValue.IsFinal || !rsiValue.IsFinal)
			return;

		var stochTyped = (StochasticOscillatorValue)stochValue;
		if (stochTyped.K is not decimal stochMain || stochTyped.D is not decimal stochSignal)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceValue)macdValue;
		if (macdTyped.Macd is not decimal macd)
			return;

		if (cciValue is not DecimalIndicatorValue { IsFinal: true } cciDecimal)
			return;
		var cci = cciDecimal.Value;

		if (aoValue is not DecimalIndicatorValue { IsFinal: true } aoDecimal)
			return;
		var awesome = aoDecimal.Value;

		if (fastMaValue is not DecimalIndicatorValue { IsFinal: true } fastDecimal ||
			slowMaValue is not DecimalIndicatorValue { IsFinal: true } slowDecimal)
			return;
		var fastMa = fastDecimal.Value;
		var slowMa = slowDecimal.Value;

		if (smaShortValue is not DecimalIndicatorValue { IsFinal: true } smaShortDecimal ||
			smaMediumValue is not DecimalIndicatorValue { IsFinal: true } smaMediumDecimal ||
			smaLongValue is not DecimalIndicatorValue { IsFinal: true } smaLongDecimal ||
			smaLongestValue is not DecimalIndicatorValue { IsFinal: true } smaLongestDecimal)
			return;

		var smaShort = smaShortDecimal.Value;
		var smaMedium = smaMediumDecimal.Value;
		var smaLong = smaLongDecimal.Value;
		var smaLongest = smaLongestDecimal.Value;

		if (rsiValue is not DecimalIndicatorValue { IsFinal: true } rsiDecimal)
			return;
		var rsi = rsiDecimal.Value;

		UpdateAwesomeBounds(awesome);

		var signal = EvaluateEntrySignal(stochMain, stochSignal, macd, cci, awesome, fastMa, slowMa, smaShort, smaMedium, smaLong, smaLongest, rsi);

		_prevStochMain = stochMain;
		_prevStochSignal = stochSignal;
		_prevMacd = macd;
		_prevFastMa = fastMa;
		_prevSlowMa = slowMa;
		_prevSmaLong = smaLong;
		_prevSmaLongest = smaLongest;

		ApplyProtections(candle, candle.ClosePrice);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		TryOpenOrders(signal, candle.CloseTime, candle.ClosePrice);
	}

	private void ApplyProtections(ICandleMessage candle, decimal price)
	{
		if (_gridDirection == EntryDirections.None)
			return;

		ApplyLifetimeProtection(candle.CloseTime);
		ApplyPriceTargets(price);
		ApplyBasketProtection(price);
	}

	private void ApplyLifetimeProtection(DateTimeOffset time)
	{
		if (OrdersTimeAliveSeconds <= 0)
			return;

		if (_lastOrderTime is not DateTimeOffset lastTime)
			return;

		if (time - lastTime < TimeSpan.FromSeconds(OrdersTimeAliveSeconds))
			return;

		CloseAllPositions();
	}

	private void ApplyPriceTargets(decimal price)
	{
		if (_gridDirection == EntryDirections.Buy)
		{
			if (_takeProfitPrice is decimal tp && price >= tp)
				CloseAllPositions();
			else if (_stopLossPrice is decimal sl && price <= sl)
				CloseAllPositions();
		}
		else if (_gridDirection == EntryDirections.Sell)
		{
			if (_takeProfitPrice is decimal tp && price <= tp)
				CloseAllPositions();
			else if (_stopLossPrice is decimal sl && price >= sl)
				CloseAllPositions();
		}
	}

	private void ApplyBasketProtection(decimal price)
	{
		if (_entries.Count == 0 || _pipSize <= 0m)
			return;

		var profitPips = CalculateUnrealizedPips(price);
		var totalVolume = _totalVolume;

		if (SecureProfitProtection && OrdersToProtect > 0 && totalVolume > 0m)
		{
			if (_openOrderCount >= OrdersToProtect)
			{
				var required = ReboundProfitSecondary * totalVolume;
				if (profitPips >= required)
					CloseAllPositions();
			}
			else if (_openOrderCount == OrdersToProtect - 1)
			{
				var required = ReboundProfitPrimary * totalVolume;
				if (profitPips >= required)
					CloseAllPositions();
			}
		}

		if (LossProtection && LossThreshold > 0m && _openOrderCount >= MaxOrders && totalVolume > 0m)
		{
			var allowedLoss = -LossThreshold * totalVolume;
			if (profitPips <= allowedLoss)
				CloseAllPositions();
		}
	}

	private void TryOpenOrders(EntryDirections signal, DateTimeOffset time, decimal price)
	{
		var direction = ReverseCondition ? Reverse(signal) : signal;

		if (_openOrderCount == 0)
		{
			if (direction == EntryDirections.None)
				return;

			if (!TradeOnFriday && time.DayOfWeek == DayOfWeek.Friday)
				return;

			OpenOrder(direction);
			return;
		}

		if (_openOrderCount >= MaxOrders)
			return;

		if (!TradeOnFriday && time.DayOfWeek == DayOfWeek.Friday)
			return;

		if (!ShouldOpenAdditionalOrder(price))
			return;

		OpenOrder(_gridDirection);
	}

	private bool ShouldOpenAdditionalOrder(decimal price)
	{
		if (_lastOrderPrice is not decimal lastPrice)
			return true;

		var step = GridStepPips * _pipSize;
		if (step <= 0m)
			return false;

		return Math.Abs(price - lastPrice) >= step;
	}

	private void OpenOrder(EntryDirections direction)
	{
		if (direction == EntryDirections.None)
			return;

		var volume = CalculateOrderVolume();
		if (volume <= 0m)
			return;

		if (direction == EntryDirections.Buy)
			BuyMarket(volume);
		else
			SellMarket(volume);

		if (_openOrderCount == 0)
			_gridDirection = direction;
	}

	private decimal CalculateOrderVolume()
	{
		var multiplierPower = _openOrderCount;
		var volume = InitialVolume * (decimal)Math.Pow((double)Multiplier, multiplierPower);
		return volume;
	}

	private decimal CalculateUnrealizedPips(decimal price)
	{
		decimal result = 0m;
		foreach (var entry in _entries)
		{
			var direction = entry.Side == Sides.Buy ? 1m : -1m;
			result += (price - entry.Price) / _pipSize * direction * entry.Volume;
		}
		return result;
	}

	private void UpdateTargets()
	{
		if (_lastOrderPrice is not decimal price)
			return;

		if (_gridDirection == EntryDirections.None)
		{
			_takeProfitPrice = null;
			_stopLossPrice = null;
			return;
		}

		var pipOffset = _pipSize;
		if (TakeProfitPips > 0m)
		{
			var takePips = TakeProfitPips;
			if (SecureProfitProtection && OrdersToProtect > 0)
			{
				if (_openOrderCount == OrdersToProtect - 1)
					takePips = ReboundProfitPrimary;
				else if (_openOrderCount >= OrdersToProtect)
					takePips = ReboundProfitSecondary;
			}

			var offset = takePips * pipOffset;
			_takeProfitPrice = _gridDirection == EntryDirections.Buy ? price + offset : price - offset;
		}
		else
		{
			_takeProfitPrice = null;
		}

		if (StopLossPips > 0m)
		{
			var offset = StopLossPips * pipOffset;
			_stopLossPrice = _gridDirection == EntryDirections.Buy ? price - offset : price + offset;
		}
		else
		{
			_stopLossPrice = null;
		}
	}

	private void CloseAllPositions()
	{
		if (Position > 0m)
			SellMarket(Position);
		else if (Position < 0m)
			BuyMarket(Math.Abs(Position));
	}

	private void ResetGridState()
	{
		_entries.Clear();
		_gridDirection = EntryDirections.None;
		_lastOrderPrice = null;
		_lastOrderTime = null;
		_takeProfitPrice = null;
		_stopLossPrice = null;
		_totalVolume = 0m;
		_openOrderCount = 0;
	}

	private void UpdateAwesomeBounds(decimal value)
	{
		const decimal rate = 0.7m;
		var scaled = value * rate;
		if (!_aoInitialized)
		{
			_aoHigh = scaled;
			_aoLow = scaled;
			_aoInitialized = true;
			return;
		}

		if (scaled > _aoHigh)
			_aoHigh = scaled;
		if (scaled < _aoLow)
			_aoLow = scaled;
	}

	private EntryDirections EvaluateEntrySignal(
		decimal stochMain,
		decimal stochSignal,
		decimal macd,
		decimal cci,
		decimal awesome,
		decimal fastMa,
		decimal slowMa,
		decimal smaShort,
		decimal smaMedium,
		decimal smaLong,
		decimal smaLongest,
		decimal rsi)
	{
		var mode = EntryLogic;
		var direction = EntryDirections.None;

		switch (mode)
		{
			case EntryModes.Stochastic:
			{
				if (_prevStochMain is decimal prevMain && _prevStochSignal is decimal prevSignal)
				{
					var crossUp = stochMain > stochSignal && prevMain <= prevSignal && prevSignal < 8m;
					var crossDown = stochMain < stochSignal && prevMain >= prevSignal && prevSignal > 92m;

					if (crossUp)
						direction = EntryDirections.Buy;
					else if (crossDown)
						direction = EntryDirections.Sell;
				}
				break;
			}
			case EntryModes.MacdSlope:
			{
				if (_prevMacd is decimal prevMacd)
				{
					if (macd > prevMacd)
						direction = EntryDirections.Buy;
					else if (macd < prevMacd)
						direction = EntryDirections.Sell;
				}
				break;
			}
			case EntryModes.CciLevels:
			{
				if (cci < -120m)
					direction = EntryDirections.Buy;
				else if (cci > 120m)
					direction = EntryDirections.Sell;
				break;
			}
			case EntryModes.AwesomeBreakout:
			{
				if (awesome < _aoLow)
					direction = EntryDirections.Buy;
				else if (awesome > _aoHigh)
					direction = EntryDirections.Sell;
				break;
			}
			case EntryModes.RsiMa:
			{
				if (_prevFastMa is decimal prevFast && _prevSlowMa is decimal prevSlow)
				{
					var crossUp = prevFast <= prevSlow && fastMa > slowMa && rsi >= 50m;
					var crossDown = prevFast >= prevSlow && fastMa < slowMa && rsi <= 50m;

					if (crossUp)
						direction = EntryDirections.Buy;
					else if (crossDown)
						direction = EntryDirections.Sell;
				}
				break;
			}
			case EntryModes.SmaTrend:
			{
				if (_prevSmaLong is decimal prevLong && _prevSmaLongest is decimal prevLongest)
				{
					var slope = smaLong - smaLongest;
					var prevSlope = prevLong - prevLongest;

					if (smaShort > smaMedium && smaMedium > smaLong && slope >= TrendSlopeThreshold && prevSlope <= 0m)
						direction = EntryDirections.Buy;
					else if (smaShort < smaMedium && smaMedium < smaLong && slope <= -TrendSlopeThreshold && prevSlope >= 0m)
						direction = EntryDirections.Sell;
				}
				break;
			}
		}

		return direction;
	}

	private EntryDirections Reverse(EntryDirections direction)
	{
		return direction switch
		{
			EntryDirections.Buy => EntryDirections.Sell,
			EntryDirections.Sell => EntryDirections.Buy,
			_ => EntryDirections.None,
		};
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade myTrade)
	{
		base.OnNewMyTrade(myTrade);

		if (myTrade.Order.Security != Security)
			return;

		var orderDirection = myTrade.Order.Side;
		var trade = myTrade.Trade;
		if (trade == null)
			return;

		var volume = trade.Volume ?? myTrade.Order.Volume ?? 0m;
		if (volume <= 0m)
			return;

		_lastOrderPrice = trade.Price;
		_lastOrderTime = trade.Time;

		if (orderDirection == Sides.Buy)
		{
			if (Position > 0m)
			{
				_gridDirection = EntryDirections.Buy;
				_entries.Add(new PositionEntry(trade.Price, volume, Sides.Buy));
				_totalVolume += volume;
				_openOrderCount++;
				UpdateTargets();
			}
			else if (Position == 0m)
			{
				ResetGridState();
			}
		}
		else if (orderDirection == Sides.Sell)
		{
			if (Position < 0m)
			{
				_gridDirection = EntryDirections.Sell;
				_entries.Add(new PositionEntry(trade.Price, volume, Sides.Sell));
				_totalVolume += volume;
				_openOrderCount++;
				UpdateTargets();
			}
			else if (Position == 0m)
			{
				ResetGridState();
			}
		}
	}
}
