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
/// Strategy based on the Sidus v1 expert advisor using EMA and RSI filters.
/// Buys when the fast EMA is sufficiently below the slow EMA and RSI is oversold.
/// Sells when the fast EMA is sufficiently above the slow EMA and RSI is overbought.
/// </summary>
public class SidusV1Strategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _fastEma2Length;
	private readonly StrategyParam<int> _slowEma2Length;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _rsiPeriod2;
	private readonly StrategyParam<decimal> _buyDifferenceThreshold;
	private readonly StrategyParam<decimal> _buyRsiThreshold;
	private readonly StrategyParam<decimal> _sellDifferenceThreshold;
	private readonly StrategyParam<decimal> _sellRsiThreshold;
	private readonly StrategyParam<decimal> _buyTakeProfitPips;
	private readonly StrategyParam<decimal> _buyStopLossPips;
	private readonly StrategyParam<decimal> _sellTakeProfitPips;
	private readonly StrategyParam<decimal> _sellStopLossPips;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _maxCandleVolume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _priceStep;

	/// <summary>
	/// Length of the fast EMA for buy signal calculation.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	/// <summary>
	/// Length of the slow EMA for buy signal calculation.
	/// </summary>
	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	/// <summary>
	/// Length of the fast EMA for sell signal calculation.
	/// </summary>
	public int FastEma2Length
	{
		get => _fastEma2Length.Value;
		set => _fastEma2Length.Value = value;
	}

	/// <summary>
	/// Length of the slow EMA for sell signal calculation.
	/// </summary>
	public int SlowEma2Length
	{
		get => _slowEma2Length.Value;
		set => _slowEma2Length.Value = value;
	}

	/// <summary>
	/// RSI period used for buy signals.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI period used for sell signals.
	/// </summary>
	public int RsiPeriod2
	{
		get => _rsiPeriod2.Value;
		set => _rsiPeriod2.Value = value;
	}

	/// <summary>
	/// Threshold for EMA difference to allow buy orders.
	/// </summary>
	public decimal BuyDifferenceThreshold
	{
		get => _buyDifferenceThreshold.Value;
		set => _buyDifferenceThreshold.Value = value;
	}

	/// <summary>
	/// RSI threshold to confirm oversold conditions.
	/// </summary>
	public decimal BuyRsiThreshold
	{
		get => _buyRsiThreshold.Value;
		set => _buyRsiThreshold.Value = value;
	}

	/// <summary>
	/// Threshold for EMA difference to allow sell orders.
	/// </summary>
	public decimal SellDifferenceThreshold
	{
		get => _sellDifferenceThreshold.Value;
		set => _sellDifferenceThreshold.Value = value;
	}

	/// <summary>
	/// RSI threshold to confirm overbought conditions.
	/// </summary>
	public decimal SellRsiThreshold
	{
		get => _sellRsiThreshold.Value;
		set => _sellRsiThreshold.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips for long positions.
	/// </summary>
	public decimal BuyTakeProfitPips
	{
		get => _buyTakeProfitPips.Value;
		set => _buyTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips for long positions.
	/// </summary>
	public decimal BuyStopLossPips
	{
		get => _buyStopLossPips.Value;
		set => _buyStopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips for short positions.
	/// </summary>
	public decimal SellTakeProfitPips
	{
		get => _sellTakeProfitPips.Value;
		set => _sellTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop loss distance in pips for short positions.
	/// </summary>
	public decimal SellStopLossPips
	{
		get => _sellStopLossPips.Value;
		set => _sellStopLossPips.Value = value;
	}

	/// <summary>
	/// Volume for new market orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Maximum candle volume to allow trading.
	/// </summary>
	public decimal MaxCandleVolume
	{
		get => _maxCandleVolume.Value;
		set => _maxCandleVolume.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SidusV1Strategy"/> class.
	/// </summary>
	public SidusV1Strategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 23)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Length", "Length of the fast EMA for buy signals", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_slowEmaLength = Param(nameof(SlowEmaLength), 62)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Length", "Length of the slow EMA for buy signals", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(40, 90, 10);

		_fastEma2Length = Param(nameof(FastEma2Length), 18)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Length (Sell)", "Length of the fast EMA for sell signals", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 35, 5);

		_slowEma2Length = Param(nameof(SlowEma2Length), 54)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA Length (Sell)", "Length of the slow EMA for sell signals", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(40, 80, 10);

		_rsiPeriod = Param(nameof(RsiPeriod), 67)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI period used for buy signals", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(30, 90, 10);

		_rsiPeriod2 = Param(nameof(RsiPeriod2), 97)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period (Sell)", "RSI period used for sell signals", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(40, 110, 10);

		_buyDifferenceThreshold = Param(nameof(BuyDifferenceThreshold), 63m)
			.SetDisplay("Buy EMA Threshold", "Maximum fast-slow EMA difference to allow buy", "Trading Rules")
			.SetCanOptimize(true)
			.SetOptimize(40m, 80m, 5m);

		_buyRsiThreshold = Param(nameof(BuyRsiThreshold), 59m)
			.SetDisplay("Buy RSI Threshold", "Maximum RSI level to allow buy", "Trading Rules")
			.SetCanOptimize(true)
			.SetOptimize(40m, 70m, 5m);

		_sellDifferenceThreshold = Param(nameof(SellDifferenceThreshold), -57m)
			.SetDisplay("Sell EMA Threshold", "Minimum fast-slow EMA difference to allow sell", "Trading Rules")
			.SetCanOptimize(true)
			.SetOptimize(-80m, -40m, 5m);

		_sellRsiThreshold = Param(nameof(SellRsiThreshold), 60m)
			.SetDisplay("Sell RSI Threshold", "Minimum RSI level to allow sell", "Trading Rules")
			.SetCanOptimize(true)
			.SetOptimize(50m, 80m, 5m);

		_buyTakeProfitPips = Param(nameof(BuyTakeProfitPips), 95m)
			.SetGreaterThanOrEqualTo(0m)
			.SetDisplay("Buy Take Profit", "Take profit distance in pips for long trades", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(50m, 120m, 10m);

		_buyStopLossPips = Param(nameof(BuyStopLossPips), 100m)
			.SetGreaterThanOrEqualTo(0m)
			.SetDisplay("Buy Stop Loss", "Stop loss distance in pips for long trades", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(60m, 140m, 10m);

		_sellTakeProfitPips = Param(nameof(SellTakeProfitPips), 17m)
			.SetGreaterThanOrEqualTo(0m)
			.SetDisplay("Sell Take Profit", "Take profit distance in pips for short trades", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10m, 40m, 5m);

		_sellStopLossPips = Param(nameof(SellStopLossPips), 69m)
			.SetGreaterThanOrEqualTo(0m)
			.SetDisplay("Sell Stop Loss", "Stop loss distance in pips for short trades", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(40m, 100m, 10m);

		_orderVolume = Param(nameof(OrderVolume), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume for opening new positions", "General");

		_maxCandleVolume = Param(nameof(MaxCandleVolume), 10m)
			.SetGreaterThanOrEqualTo(0m)
			.SetDisplay("Max Candle Volume", "Maximum candle volume allowed for trading", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(0m, 20m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for calculations", "General");
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

		_priceStep = Security.PriceStep ?? 1m;

		var fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		var fastEma2 = new ExponentialMovingAverage { Length = FastEma2Length };
		var slowEma2 = new ExponentialMovingAverage { Length = SlowEma2Length };
		var rsi = new RSI { Length = RsiPeriod };
		var rsi2 = new RSI { Length = RsiPeriod2 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, fastEma2, slowEma2, rsi, rsi2, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle,
		decimal fastEmaValue,
		decimal slowEmaValue,
		decimal fastEma2Value,
		decimal slowEma2Value,
		decimal rsiValue,
		decimal rsi2Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (candle.TotalVolume > MaxCandleVolume)
			return;

		var diffBuy = fastEmaValue - slowEmaValue;
		var diffSell = fastEma2Value - slowEma2Value;

		if (diffBuy < BuyDifferenceThreshold && rsiValue < BuyRsiThreshold && Position <= 0)
		{
			CancelActiveOrders();

			var volume = OrderVolume + Math.Max(0m, -Position);
			BuyMarket(volume);

			PlaceRiskOrders(candle.ClosePrice, true, volume);
		}

		if (diffSell > SellDifferenceThreshold && rsi2Value > SellRsiThreshold && Position >= 0)
		{
			CancelActiveOrders();

			var volume = OrderVolume + Math.Max(0m, Position);
			SellMarket(volume);

			PlaceRiskOrders(candle.ClosePrice, false, volume);
		}
	}

	private void PlaceRiskOrders(decimal entryPrice, bool isLong, decimal volume)
	{
		if (isLong)
		{
			if (BuyTakeProfitPips > 0)
			{
				var tpPrice = entryPrice + BuyTakeProfitPips * _priceStep;
				SellLimit(tpPrice, volume);
			}

			if (BuyStopLossPips > 0)
			{
				var slPrice = entryPrice - BuyStopLossPips * _priceStep;
				SellStop(slPrice, volume);
			}
		}
		else
		{
			if (SellTakeProfitPips > 0)
			{
				var tpPrice = entryPrice - SellTakeProfitPips * _priceStep;
				BuyLimit(tpPrice, volume);
			}

			if (SellStopLossPips > 0)
			{
				var slPrice = entryPrice + SellStopLossPips * _priceStep;
				BuyStop(slPrice, volume);
			}
		}
	}
}

