namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Pin Bar Magic Strategy.
/// A bullish pin bar whose tail pierces one of the averages while Fast EMA > Medium EMA > Slow SMA arms a buy stop at the
/// pin bar high; a bearish pin bar in the opposite fan arms a sell stop at its low. An entry not triggered within
/// CancelEntryBars candles is cancelled. The stop sits ATR * AtrMultiplier from the entry and the size risks EquityRisk percent
/// of the account on that distance. Positions close when the fast EMA crosses the medium EMA against them.
/// </summary>
public class PinBarMagicStrategy : Strategy
{
	private const decimal PinBarWickRatio = 0.66m;

	private readonly StrategyParam<decimal> _equityRisk;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _slowSmaLength;
	private readonly StrategyParam<int> _mediumEmaLength;
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<int> _cancelEntryBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevFast;
	private decimal? _prevMedium;

	private decimal? _pendingLevel;
	private decimal _pendingStopDistance;
	private decimal _pendingVolume;
	private int _pendingSide;
	private int _pendingBarsLeft;

	private decimal? _stopPrice;

	/// <summary>
	/// Percent of the account risked per trade.
	/// </summary>
	public decimal EquityRisk
	{
		get => _equityRisk.Value;
		set => _equityRisk.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the stop distance.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Slow SMA period.
	/// </summary>
	public int SlowSmaLength
	{
		get => _slowSmaLength.Value;
		set => _slowSmaLength.Value = value;
	}

	/// <summary>
	/// Medium EMA period.
	/// </summary>
	public int MediumEmaLength
	{
		get => _mediumEmaLength.Value;
		set => _mediumEmaLength.Value = value;
	}

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Candles after which an untriggered entry is cancelled.
	/// </summary>
	public int CancelEntryBars
	{
		get => _cancelEntryBars.Value;
		set => _cancelEntryBars.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public PinBarMagicStrategy()
	{
		_equityRisk = Param(nameof(EquityRisk), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Equity Risk %", "Percent of the account risked per trade", "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiplier of the stop distance", "Risk");

		_slowSmaLength = Param(nameof(SlowSmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA Period", "Slow SMA period", "Indicators");

		_mediumEmaLength = Param(nameof(MediumEmaLength), 18)
			.SetGreaterThanZero()
			.SetDisplay("Medium EMA Period", "Medium EMA period", "Indicators");

		_fastEmaLength = Param(nameof(FastEmaLength), 6)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Period", "Fast EMA period", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Indicators");

		_cancelEntryBars = Param(nameof(CancelEntryBars), 3)
			.SetGreaterThanZero()
			.SetDisplay("Cancel Entry Bars", "Candles after which an untriggered entry is cancelled", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	private void ResetState()
	{
		_prevFast = null;
		_prevMedium = null;
		_pendingLevel = null;
		_pendingStopDistance = 0m;
		_pendingVolume = 0m;
		_pendingSide = 0;
		_pendingBarsLeft = 0;
		_stopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var slowSma = new SimpleMovingAverage { Length = SlowSmaLength };
		var mediumEma = new ExponentialMovingAverage { Length = MediumEmaLength };
		var fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(slowSma, mediumEma, fastEma, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, slowSma);
			DrawIndicator(area, mediumEma);
			DrawIndicator(area, fastEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue slowValue, IIndicatorValue mediumValue, IIndicatorValue fastValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!slowValue.IsFormed || !mediumValue.IsFormed || !fastValue.IsFormed || !atrValue.IsFormed)
			return;

		var slow = slowValue.GetValue<decimal>();
		var medium = mediumValue.GetValue<decimal>();
		var fast = fastValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();

		var prevFast = _prevFast;
		var prevMedium = _prevMedium;
		_prevFast = fast;
		_prevMedium = medium;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var crossDown = prevFast is decimal pf1 && prevMedium is decimal pm1 && pf1 >= pm1 && fast < medium;
		var crossUp = prevFast is decimal pf2 && prevMedium is decimal pm2 && pf2 <= pm2 && fast > medium;

		// Exits of the open position: the ATR stop or the fast/medium cross against it.
		var position = Position;

		if (position > 0 && ((_stopPrice is decimal longStop && candle.LowPrice <= longStop) || crossDown))
		{
			SellMarket(position);
			_stopPrice = null;
			position = 0m;
		}
		else if (position < 0 && ((_stopPrice is decimal shortStop && candle.HighPrice >= shortStop) || crossUp))
		{
			BuyMarket(-position);
			_stopPrice = null;
			position = 0m;
		}

		// A pending stop entry triggers when the candle trades through its level.
		if (_pendingLevel is decimal level)
		{
			if (_pendingSide > 0 && candle.HighPrice >= level)
			{
				BuyMarket(_pendingVolume + Math.Max(0m, -position));
				position = _pendingVolume;
				_stopPrice = level - _pendingStopDistance;
				ClearPending();
			}
			else if (_pendingSide < 0 && candle.LowPrice <= level)
			{
				SellMarket(_pendingVolume + Math.Max(0m, position));
				position = -_pendingVolume;
				_stopPrice = level + _pendingStopDistance;
				ClearPending();
			}
			else if (--_pendingBarsLeft <= 0)
			{
				ClearPending();
			}
		}

		var range = candle.HighPrice - candle.LowPrice;
		if (range <= 0m)
			return;

		var bodyLow = Math.Min(candle.OpenPrice, candle.ClosePrice);
		var bodyHigh = Math.Max(candle.OpenPrice, candle.ClosePrice);
		var bullishPinBar = bodyLow - candle.LowPrice > PinBarWickRatio * range;
		var bearishPinBar = candle.HighPrice - bodyHigh > PinBarWickRatio * range;

		var fanUp = fast > medium && medium > slow;
		var fanDown = fast < medium && medium < slow;

		var bullPierce = Pierces(candle.LowPrice, candle, fast, true) || Pierces(candle.LowPrice, candle, medium, true) || Pierces(candle.LowPrice, candle, slow, true);
		var bearPierce = Pierces(candle.HighPrice, candle, fast, false) || Pierces(candle.HighPrice, candle, medium, false) || Pierces(candle.HighPrice, candle, slow, false);

		var stopDistance = atr * AtrMultiplier;
		if (stopDistance <= 0m)
			return;

		if (fanUp && bullishPinBar && bullPierce && position <= 0)
			ArmEntry(1, candle.HighPrice, stopDistance);
		else if (fanDown && bearishPinBar && bearPierce && position >= 0)
			ArmEntry(-1, candle.LowPrice, stopDistance);
	}

	private static bool Pierces(decimal extreme, ICandleMessage candle, decimal average, bool bullish)
		=> bullish
			? extreme < average && candle.OpenPrice > average && candle.ClosePrice > average
			: extreme > average && candle.OpenPrice < average && candle.ClosePrice < average;

	private void ArmEntry(int side, decimal level, decimal stopDistance)
	{
		var volume = CalculateVolume(stopDistance);
		if (volume <= 0m)
			return;

		_pendingSide = side;
		_pendingLevel = level;
		_pendingStopDistance = stopDistance;
		_pendingVolume = volume;
		_pendingBarsLeft = CancelEntryBars;
	}

	private void ClearPending()
	{
		_pendingLevel = null;
		_pendingSide = 0;
		_pendingBarsLeft = 0;
	}

	private decimal CalculateVolume(decimal stopDistance)
	{
		var equity = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		if (equity <= 0m)
			return Volume;

		var volume = equity * EquityRisk / 100m / stopDistance;

		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m)
			volume = Math.Floor(volume / step) * step;

		if (Security?.MaxVolume is decimal max && max > 0m && volume > max)
			volume = max;

		return volume;
	}
}
