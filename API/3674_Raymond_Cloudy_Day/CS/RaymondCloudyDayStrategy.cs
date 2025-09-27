using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Raymond Cloudy Day strategy.
/// Computes Raymond levels from a higher timeframe and trades pullbacks around the first sell take-profit level.
/// </summary>
public class RaymondCloudyDayStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _protectiveOffsetTicks;
	private readonly StrategyParam<DataType> _signalCandleType;
	private readonly StrategyParam<DataType> _pivotCandleType;

	private decimal? _tradeSessionLevel;
	private decimal? _extendedBuyLevel;
	private decimal? _extendedSellLevel;
	private decimal? _takeProfitBuyLevel;
	private decimal? _takeProfitSellLevel;
	private decimal? _takeProfitBuyLevel2;
	private decimal? _takeProfitSellLevel2;

	private decimal? _entryPrice;
	private decimal? _takePrice;
	private decimal? _stopPrice;

	/// <summary>
	/// Trade volume used for new positions.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Distance in ticks used to build stop-loss and take-profit levels around the entry price.
	/// </summary>
	public int ProtectiveOffsetTicks
	{
		get => _protectiveOffsetTicks.Value;
		set => _protectiveOffsetTicks.Value = value;
	}

	/// <summary>
	/// Candle type that triggers trade signals.
	/// </summary>
	public DataType SignalCandleType
	{
		get => _signalCandleType.Value;
		set => _signalCandleType.Value = value;
	}

	/// <summary>
	/// Higher timeframe candle type used to compute Raymond levels.
	/// </summary>
	public DataType PivotCandleType
	{
		get => _pivotCandleType.Value;
		set => _pivotCandleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public RaymondCloudyDayStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order volume used for entries", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 5m, 0.1m);

		_protectiveOffsetTicks = Param(nameof(ProtectiveOffsetTicks), 500)
			.SetGreaterThanZero()
			.SetDisplay("Protective Offset (ticks)", "Distance in ticks for stop-loss and take-profit", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(50, 1000, 50);

		_signalCandleType = Param(nameof(SignalCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Signal Candle Type", "Candle type used for trade signals", "Data");

		_pivotCandleType = Param(nameof(PivotCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Pivot Candle Type", "Higher timeframe used to compute Raymond levels", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, SignalCandleType);

		if (!SignalCandleType.Equals(PivotCandleType))
			yield return (Security, PivotCandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_tradeSessionLevel = null;
		_extendedBuyLevel = null;
		_extendedSellLevel = null;
		_takeProfitBuyLevel = null;
		_takeProfitSellLevel = null;
		_takeProfitBuyLevel2 = null;
		_takeProfitSellLevel2 = null;

		ResetProtection();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		var signalSubscription = SubscribeCandles(SignalCandleType);
		signalSubscription
			// Process only completed signal candles to avoid using partial data.
			.WhenCandlesFinished(ProcessSignalCandle)
			.Start();

		var pivotSubscription = SubscribeCandles(PivotCandleType);
		pivotSubscription
			// Recalculate Raymond levels as soon as the higher timeframe candle is finished.
			.WhenCandlesFinished(ProcessPivotCandle)
			.Start();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, signalSubscription);
			DrawOwnTrades(priceArea);
		}
	}

	private void ProcessPivotCandle(ICandleMessage candle)
	{
		// Skip unfinished candles to keep the level calculation consistent.
		if (candle.State != CandleStates.Finished)
			return;

		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var open = candle.OpenPrice;
		var close = candle.ClosePrice;

		var tradeSession = (high + low + open + close) / 4m;
		var pivotRange = high - low;

		_tradeSessionLevel = tradeSession;
		_extendedBuyLevel = tradeSession + 0.382m * pivotRange;
		_extendedSellLevel = tradeSession - 0.382m * pivotRange;
		_takeProfitBuyLevel = tradeSession + 0.618m * pivotRange;
		_takeProfitSellLevel = tradeSession - 0.618m * pivotRange;
		_takeProfitBuyLevel2 = tradeSession + pivotRange;
		_takeProfitSellLevel2 = tradeSession - pivotRange;

		LogInfo($"Updated Raymond levels from {candle.OpenTime:u}. TradeSS={tradeSession}, ETB={_extendedBuyLevel}, ETS={_extendedSellLevel}, TPB1={_takeProfitBuyLevel}, TPS1={_takeProfitSellLevel}.");
	}

	private void ProcessSignalCandle(ICandleMessage candle)
	{
		// Manage exits first so protective logic reacts even when trading is disabled.
		if (candle.State != CandleStates.Finished)
			return;

		ManageOpenPosition(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_takeProfitSellLevel is not decimal triggerLevel)
			return;

		var low = candle.LowPrice;
		var close = candle.ClosePrice;

		// Replicate the original EA condition around the TPS1 level.
		if (Position <= 0 && low < triggerLevel && close > triggerLevel)
		{
			EnterLong(close);
		}
		else if (Position >= 0 && low > triggerLevel && close < triggerLevel)
		{
			EnterShort(close);
		}
	}

	private void EnterLong(decimal closePrice)
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		{
			LogWarning("Cannot place long trade because PriceStep is not defined.");
			return;
		}

		CancelActiveOrders();

		var volume = TradeVolume + Math.Max(0m, -Position);
		BuyMarket(volume);

		// Convert the tick offset into an absolute price distance.
		var offset = priceStep * ProtectiveOffsetTicks;
		_entryPrice = closePrice;
		_takePrice = closePrice + offset;
		_stopPrice = closePrice - offset;

		LogInfo($"Opened long position at {closePrice}. TP={_takePrice}, SL={_stopPrice}.");
	}

	private void EnterShort(decimal closePrice)
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		{
			LogWarning("Cannot place short trade because PriceStep is not defined.");
			return;
		}

		CancelActiveOrders();

		var volume = TradeVolume + Math.Max(0m, Position);
		SellMarket(volume);

		// Mirror the original EA risk profile for short trades.
		var offset = priceStep * ProtectiveOffsetTicks;
		_entryPrice = closePrice;
		_takePrice = closePrice - offset;
		_stopPrice = closePrice + offset;

		LogInfo($"Opened short position at {closePrice}. TP={_takePrice}, SL={_stopPrice}.");
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position == 0)
		{
			ResetProtection();
			return;
		}

		if (_entryPrice is not decimal entry || _takePrice is not decimal take || _stopPrice is not decimal stop)
			return;

		if (Position > 0)
		{
			// Close the long position if price breaches the protective levels.
			if (candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetProtection();
				LogInfo($"Long stop-loss triggered at {stop}.");
				return;
			}

			if (candle.HighPrice >= take)
			{
				SellMarket(Position);
				ResetProtection();
				LogInfo($"Long take-profit triggered at {take}.");
				return;
			}
		}
		else
		{
			var volume = Math.Abs(Position);

			// Close the short position when stop or take-profit is hit.
			if (candle.HighPrice >= stop)
			{
				BuyMarket(volume);
				ResetProtection();
				LogInfo($"Short stop-loss triggered at {stop}.");
				return;
			}

			if (candle.LowPrice <= take)
			{
				BuyMarket(volume);
				ResetProtection();
				LogInfo($"Short take-profit triggered at {take}.");
			}
		}
	}

	private void ResetProtection()
	{
		_entryPrice = null;
		_takePrice = null;
		_stopPrice = null;
	}
}
