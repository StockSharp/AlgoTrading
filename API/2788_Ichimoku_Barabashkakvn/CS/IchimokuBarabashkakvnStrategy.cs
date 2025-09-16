using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ichimoku cloud strategy inspired by barabashkakvn's MQL implementation.
/// Executes Tenkan/Kijun crosses with Kumo confirmation and optional timed trading window.
/// Includes independent stop-loss/take-profit targets per direction and pip-based trailing stops.
/// </summary>
public class IchimokuBarabashkakvnStrategy : Strategy
{
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanBPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _buyStopLossPips;
	private readonly StrategyParam<int> _buyTakeProfitPips;
	private readonly StrategyParam<int> _sellStopLossPips;
	private readonly StrategyParam<int> _sellTakeProfitPips;
	private readonly StrategyParam<int> _buyTrailingStopPips;
	private readonly StrategyParam<int> _sellTrailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<bool> _useTradeHours;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;

	private Ichimoku _ichimoku = null!;
	private decimal? _prevTenkan;
	private decimal? _entryPrice;
	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;
	private decimal _pipValue;

	/// <summary>
	/// Tenkan-sen period.
	/// </summary>
	public int TenkanPeriod
	{
		get => _tenkanPeriod.Value;
		set => _tenkanPeriod.Value = value;
	}

	/// <summary>
	/// Kijun-sen period.
	/// </summary>
	public int KijunPeriod
	{
		get => _kijunPeriod.Value;
		set => _kijunPeriod.Value = value;
	}

	/// <summary>
	/// Senkou Span B period.
	/// </summary>
	public int SenkouSpanBPeriod
	{
		get => _senkouSpanBPeriod.Value;
		set => _senkouSpanBPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for long trades in pips.
	/// </summary>
	public int BuyStopLossPips
	{
		get => _buyStopLossPips.Value;
		set => _buyStopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance for long trades in pips.
	/// </summary>
	public int BuyTakeProfitPips
	{
		get => _buyTakeProfitPips.Value;
		set => _buyTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for short trades in pips.
	/// </summary>
	public int SellStopLossPips
	{
		get => _sellStopLossPips.Value;
		set => _sellStopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance for short trades in pips.
	/// </summary>
	public int SellTakeProfitPips
	{
		get => _sellTakeProfitPips.Value;
		set => _sellTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance for long trades in pips.
	/// </summary>
	public int BuyTrailingStopPips
	{
		get => _buyTrailingStopPips.Value;
		set => _buyTrailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance for short trades in pips.
	/// </summary>
	public int SellTrailingStopPips
	{
		get => _sellTrailingStopPips.Value;
		set => _sellTrailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step in pips for both directions.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Enable trading only within the configured hour window.
	/// </summary>
	public bool UseTradeHours
	{
		get => _useTradeHours.Value;
		set => _useTradeHours.Value = value;
	}

	/// <summary>
	/// First trading hour (0-23).
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Last trading hour (0-23).
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="IchimokuBarabashkakvnStrategy"/>.
	/// </summary>
	public IchimokuBarabashkakvnStrategy()
	{
		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Tenkan Period", "Tenkan-sen periods", "Ichimoku");

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Kijun Period", "Kijun-sen periods", "Ichimoku");

		_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
			.SetGreaterThanZero()
			.SetDisplay("Senkou Span B Period", "Senkou Span B periods", "Ichimoku");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Source candles for processing", "General");

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Position size in lots", "Trading");

		_buyStopLossPips = Param(nameof(BuyStopLossPips), 100)
			.SetNotNegative()
			.SetDisplay("Buy Stop Loss (pips)", "Long stop-loss distance", "Risk");

		_buyTakeProfitPips = Param(nameof(BuyTakeProfitPips), 300)
			.SetNotNegative()
			.SetDisplay("Buy Take Profit (pips)", "Long take-profit distance", "Risk");

		_sellStopLossPips = Param(nameof(SellStopLossPips), 100)
			.SetNotNegative()
			.SetDisplay("Sell Stop Loss (pips)", "Short stop-loss distance", "Risk");

		_sellTakeProfitPips = Param(nameof(SellTakeProfitPips), 300)
			.SetNotNegative()
			.SetDisplay("Sell Take Profit (pips)", "Short take-profit distance", "Risk");

		_buyTrailingStopPips = Param(nameof(BuyTrailingStopPips), 50)
			.SetNotNegative()
			.SetDisplay("Buy Trailing Stop (pips)", "Long trailing distance", "Risk");

		_sellTrailingStopPips = Param(nameof(SellTrailingStopPips), 50)
			.SetNotNegative()
			.SetDisplay("Sell Trailing Stop (pips)", "Short trailing distance", "Risk");

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetNotNegative()
			.SetDisplay("Trailing Step (pips)", "Step for trailing adjustments", "Risk");

		_useTradeHours = Param(nameof(UseTradeHours), false)
			.SetDisplay("Use Trade Hours", "Restrict trading to a time range", "Timing");

		_startHour = Param(nameof(StartHour), 0)
			.SetDisplay("Start Hour", "First hour allowed for trading", "Timing")
			.SetRange(0, 23);

		_endHour = Param(nameof(EndHour), 23)
			.SetDisplay("End Hour", "Last hour allowed for trading", "Timing")
			.SetRange(0, 23);
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
		_prevTenkan = null;
		ResetPositionState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (UseTradeHours && StartHour >= EndHour)
			throw new InvalidOperationException("Start hour must be less than end hour when time filter is enabled.");

		_ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouSpanBPeriod }
		};

		_pipValue = CalculatePipValue();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_ichimoku, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ichimoku);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!ichimokuValue.IsFinal)
			return;

		var ichimokuTyped = (IchimokuValue)ichimokuValue;

		if (ichimokuTyped.Tenkan is not decimal tenkan ||
			ichimokuTyped.Kijun is not decimal kijun ||
			ichimokuTyped.SenkouA is not decimal senkouA ||
			ichimokuTyped.SenkouB is not decimal senkouB)
		{
			return;
		}

		if (!_ichimoku.IsFormed)
		{
			_prevTenkan = tenkan;
			return;
		}

		if (_prevTenkan is null)
		{
			_prevTenkan = tenkan;
			return;
		}

		// Ensure state is cleared if positions were closed externally.
		if (Position == 0 && (_entryPrice.HasValue || _stopLossPrice.HasValue || _takeProfitPrice.HasValue))
			ResetPositionState();

		if (Position != 0 && CheckProtectiveLevels(candle))
		{
			_prevTenkan = tenkan;
			return;
		}

		if (UseTradeHours)
		{
			var hour = candle.OpenTime.Hour;
			if (!(StartHour >= hour && hour <= EndHour))
			{
				_prevTenkan = tenkan;
				return;
			}
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevTenkan = tenkan;
			return;
		}

		var buySignal = _prevTenkan < kijun && tenkan >= kijun && candle.ClosePrice > senkouB;
		var sellSignal = _prevTenkan > kijun && tenkan <= kijun && candle.ClosePrice < senkouA;

		if (Position == 0)
		{
			if (buySignal)
			{
				OpenLong(candle);
			}
			else if (sellSignal)
			{
				OpenShort(candle);
			}
		}
		else if (Position < 0)
		{
			if (buySignal)
			{
				CloseShort();
				_prevTenkan = tenkan;
				return;
			}
		}
		else if (Position > 0)
		{
			if (sellSignal)
			{
				CloseLong();
				_prevTenkan = tenkan;
				return;
			}
		}

		UpdateTrailingStops(candle);
		_prevTenkan = tenkan;
	}

	private void OpenLong(ICandleMessage candle)
	{
		if (OrderVolume <= 0)
			return;

		BuyMarket(OrderVolume);
		_entryPrice = candle.ClosePrice;

		var stopOffset = GetPriceOffset(BuyStopLossPips);
		var takeOffset = GetPriceOffset(BuyTakeProfitPips);

		_stopLossPrice = stopOffset > 0 ? candle.ClosePrice - stopOffset : null;
		_takeProfitPrice = takeOffset > 0 ? candle.ClosePrice + takeOffset : null;
	}

	private void OpenShort(ICandleMessage candle)
	{
		if (OrderVolume <= 0)
			return;

		SellMarket(OrderVolume);
		_entryPrice = candle.ClosePrice;

		var stopOffset = GetPriceOffset(SellStopLossPips);
		var takeOffset = GetPriceOffset(SellTakeProfitPips);

		_stopLossPrice = stopOffset > 0 ? candle.ClosePrice + stopOffset : null;
		_takeProfitPrice = takeOffset > 0 ? candle.ClosePrice - takeOffset : null;
	}

	private void CloseLong()
	{
		if (Position <= 0)
			return;

		SellMarket(Math.Abs(Position));
		ResetPositionState();
	}

	private void CloseShort()
	{
		if (Position >= 0)
			return;

		BuyMarket(Math.Abs(Position));
		ResetPositionState();
	}

	private void UpdateTrailingStops(ICandleMessage candle)
	{
		if (_entryPrice is null)
			return;

		if (Position > 0)
		{
			var trailingStop = GetPriceOffset(BuyTrailingStopPips);
			var trailingStep = GetPriceOffset(TrailingStepPips);

			if (trailingStop > 0 && trailingStep >= 0)
			{
				var profit = candle.ClosePrice - _entryPrice.Value;
				if (profit > trailingStop + trailingStep)
				{
					var threshold = candle.ClosePrice - (trailingStop + trailingStep);
					if (!_stopLossPrice.HasValue || _stopLossPrice.Value < threshold)
					{
						_stopLossPrice = candle.ClosePrice - trailingStop;
					}
				}
			}

			CheckProtectiveLevels(candle);
		}
		else if (Position < 0)
		{
			var trailingStop = GetPriceOffset(SellTrailingStopPips);
			var trailingStep = GetPriceOffset(TrailingStepPips);

			if (trailingStop > 0 && trailingStep >= 0)
			{
				var profit = _entryPrice.Value - candle.ClosePrice;
				if (profit > trailingStop + trailingStep)
				{
					var threshold = candle.ClosePrice + (trailingStop + trailingStep);
					if (!_stopLossPrice.HasValue || _stopLossPrice.Value > threshold)
					{
						_stopLossPrice = candle.ClosePrice + trailingStop;
					}
				}
			}

			CheckProtectiveLevels(candle);
		}
	}

	private bool CheckProtectiveLevels(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_stopLossPrice is decimal sl && candle.LowPrice <= sl)
			{
				SellMarket(Math.Abs(Position));
				ResetPositionState();
				return true;
			}

			if (_takeProfitPrice is decimal tp && candle.HighPrice >= tp)
			{
				SellMarket(Math.Abs(Position));
				ResetPositionState();
				return true;
			}
		}
		else if (Position < 0)
		{
			if (_stopLossPrice is decimal sl && candle.HighPrice >= sl)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return true;
			}

			if (_takeProfitPrice is decimal tp && candle.LowPrice <= tp)
			{
				BuyMarket(Math.Abs(Position));
				ResetPositionState();
				return true;
			}
		}

		return false;
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopLossPrice = null;
		_takeProfitPrice = null;
	}

	private decimal GetPriceOffset(int pips)
	{
		if (pips <= 0)
			return 0m;

		return pips * _pipValue;
	}

	private decimal CalculatePipValue()
	{
		var priceStep = Security?.PriceStep ?? 0m;

		if (priceStep <= 0m)
			priceStep = 1m;

		var decimals = GetDecimalPlaces(priceStep);
		var multiplier = decimals is 3 or 5 ? 10m : 1m;

		return priceStep * multiplier;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		var bits = decimal.GetBits(value);
		return (bits[3] >> 16) & 0xFF;
	}
}
