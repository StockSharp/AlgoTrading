using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert "DeMarker gaining position volume".
/// Uses the DeMarker oscillator to accumulate positions when extreme levels are reached.
/// Supports optional reversal logic and skipping losing exits when flipping direction.
/// </summary>
public class DeMarkerGainingPositionVolumeStrategy : Strategy
{
	private readonly StrategyParam<int> _deMarkerPeriod;
	private readonly StrategyParam<decimal> _upperLevel;
	private readonly StrategyParam<decimal> _lowerLevel;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<bool> _onlyOnePosition;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _longEntryPrice;
	private decimal _longVolume;
	private decimal _shortEntryPrice;
	private decimal _shortVolume;

	private DateTimeOffset? _lastBuyBarTime;
	private DateTimeOffset? _lastSellBarTime;

	/// <summary>
	/// Number of candles used by the DeMarker indicator.
	/// </summary>
	public int DeMarkerPeriod
	{
		get => _deMarkerPeriod.Value;
		set => _deMarkerPeriod.Value = value;
	}

	/// <summary>
	/// DeMarker level that triggers short entries (or long exits in reverse mode).
	/// </summary>
	public decimal UpperLevel
	{
		get => _upperLevel.Value;
		set => _upperLevel.Value = value;
	}

	/// <summary>
	/// DeMarker level that triggers long entries (or short exits in reverse mode).
	/// </summary>
	public decimal LowerLevel
	{
		get => _lowerLevel.Value;
		set => _lowerLevel.Value = value;
	}

	/// <summary>
	/// Market order volume used on each entry.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Restricts the strategy to a single aggregated position when enabled.
	/// </summary>
	public bool OnlyOnePosition
	{
		get => _onlyOnePosition.Value;
		set => _onlyOnePosition.Value = value;
	}

	/// <summary>
	/// Inverts the signal mapping so that overbought levels buy and oversold levels sell.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Candle type that defines the timeframe for the oscillator.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public DeMarkerGainingPositionVolumeStrategy()
	{
		_deMarkerPeriod = Param(nameof(DeMarkerPeriod), 14)
			.SetDisplay("DeMarker Period", "Number of bars used by the oscillator.", "Indicator")
			.SetCanOptimize(true);

		_upperLevel = Param(nameof(UpperLevel), 0.7m)
			.SetDisplay("Upper Level", "Threshold that prepares short exposure.", "Indicator")
			.SetCanOptimize(true);

		_lowerLevel = Param(nameof(LowerLevel), 0.3m)
			.SetDisplay("Lower Level", "Threshold that prepares long exposure.", "Indicator")
			.SetCanOptimize(true);

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Trade Volume", "Order volume submitted on each signal.", "Trading")
			.SetCanOptimize(true);

		_onlyOnePosition = Param(nameof(OnlyOnePosition), false)
			.SetDisplay("Only One Position", "Prohibit holding both long and short exposure simultaneously.", "Trading");

		_reverseSignals = Param(nameof(ReverseSignals), false)
			.SetDisplay("Reverse Signals", "Swap the buy and sell conditions.", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for DeMarker calculations.", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var deMarker = new DeMarker
		{
			Length = DeMarkerPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(deMarker, ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal deMarkerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var barTime = candle.OpenTime;

		var wantLong = deMarkerValue <= LowerLevel;
		var wantShort = deMarkerValue >= UpperLevel;

		if (ReverseSignals)
		{
			(wantLong, wantShort) = (wantShort, wantLong);
		}

		if (wantLong && _lastBuyBarTime != barTime)
		{
			if (TryOpenLong(candle))
			{
				_lastBuyBarTime = barTime;
			}
		}

		if (wantShort && _lastSellBarTime != barTime)
		{
			if (TryOpenShort(candle))
			{
				_lastSellBarTime = barTime;
			}
		}
	}

	private bool TryOpenLong(ICandleMessage candle)
	{
		if (TradeVolume <= 0m)
			return false;

		if (Position < 0m)
		{
			if (!CanCloseShort(candle.ClosePrice))
				return false;

			ClosePosition();
			ResetShortState();
		}

		var canOpen = OnlyOnePosition ? Position == 0m : Position >= 0m;
		if (!canOpen)
			return false;

		BuyMarket(TradeVolume);
		RegisterLongEntry(candle.ClosePrice, TradeVolume);
		return true;
	}

	private bool TryOpenShort(ICandleMessage candle)
	{
		if (TradeVolume <= 0m)
			return false;

		if (Position > 0m)
		{
			if (!CanCloseLong(candle.ClosePrice))
				return false;

			ClosePosition();
			ResetLongState();
		}

		var canOpen = OnlyOnePosition ? Position == 0m : Position <= 0m;
		if (!canOpen)
			return false;

		SellMarket(TradeVolume);
		RegisterShortEntry(candle.ClosePrice, TradeVolume);
		return true;
	}

	private bool CanCloseShort(decimal closePrice)
	{
		if (_shortVolume <= 0m)
			return true;

		var profit = (_shortEntryPrice - closePrice) * _shortVolume;
		return profit > 0m;
	}

	private bool CanCloseLong(decimal closePrice)
	{
		if (_longVolume <= 0m)
			return true;

		var profit = (closePrice - _longEntryPrice) * _longVolume;
		return profit > 0m;
	}

	private void RegisterLongEntry(decimal price, decimal volume)
	{
		if (volume <= 0m)
			return;

		var totalVolume = _longVolume + volume;
		_longEntryPrice = totalVolume == 0m ? 0m : (_longEntryPrice * _longVolume + price * volume) / totalVolume;
		_longVolume = totalVolume;
	}

	private void RegisterShortEntry(decimal price, decimal volume)
	{
		if (volume <= 0m)
			return;

		var totalVolume = _shortVolume + volume;
		_shortEntryPrice = totalVolume == 0m ? 0m : (_shortEntryPrice * _shortVolume + price * volume) / totalVolume;
		_shortVolume = totalVolume;
	}

	private void ResetLongState()
	{
		_longEntryPrice = 0m;
		_longVolume = 0m;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = 0m;
		_shortVolume = 0m;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		ResetLongState();
		ResetShortState();
		_lastBuyBarTime = null;
		_lastSellBarTime = null;
	}
}
