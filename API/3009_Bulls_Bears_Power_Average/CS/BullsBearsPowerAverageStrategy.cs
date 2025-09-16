using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that compares averaged Bulls Power and Bears Power values to detect momentum shifts.
/// </summary>
public class BullsBearsPowerAverageStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage? _ema;
	private decimal? _previousAverage;

	/// <summary>
	/// Order size used for entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// EMA period used by Bulls/Bears power calculations.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
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
	/// Initializes a new instance of the <see cref="BullsBearsPowerAverageStrategy"/> class.
	/// </summary>
	public BullsBearsPowerAverageStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Order size for entries", "Trading");

		_stopLossPips = Param(nameof(StopLossPips), 15)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Stop Loss (pips)", "Stop loss distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 95)
			.SetGreaterThanOrEqualZero()
			.SetDisplay("Take Profit (pips)", "Take profit distance in pips", "Risk");

		_maPeriod = Param(nameof(MaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period for EMA used by Bulls/Bears Power", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used for calculations", "General");
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

		// Reset internal state to prepare for the next backtest or live run.
		_previousAverage = null;
		_ema = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Create EMA indicator that replicates the smoothing from Bulls/Bears Power.
		_ema = new ExponentialMovingAverage
		{
			Length = MaPeriod
		};

		// Subscribe to candle updates and process them together with the EMA output.
		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, ProcessCandle)
			.Start();

		// Use configured order volume for all entries.
		Volume = OrderVolume;

		// Configure risk management in absolute price units derived from instrument tick size.
		var step = Security?.PriceStep ?? 1m;
		Unit? stopLoss = StopLossPips > 0 ? new Unit(StopLossPips * step, UnitTypes.Absolute) : null;
		Unit? takeProfit = TakeProfitPips > 0 ? new Unit(TakeProfitPips * step, UnitTypes.Absolute) : null;

		if (stopLoss != null || takeProfit != null)
		{
			StartProtection(
				stopLoss: stopLoss,
				takeProfit: takeProfit,
				useMarketOrders: true);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Ensure that the strategy is ready for trading.
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Skip calculations until the EMA is fully formed.
		if (_ema is null || !_ema.IsFormed)
			return;

		// Compute Bulls and Bears Power values using candle extremes.
		var bullsPower = candle.HighPrice - emaValue;
		var bearsPower = candle.LowPrice - emaValue;

		// Average both forces to reproduce the original indicator combination.
		var averagePower = (bullsPower + bearsPower) / 2m;

		if (_previousAverage is decimal prevAverage)
		{
			var hasLongSignal = prevAverage < averagePower && averagePower < 0m;
			var hasShortSignal = prevAverage > averagePower && averagePower > 0m;

			// Open long position when bearish pressure fades while still below zero.
			if (hasLongSignal && Position == 0)
			{
				BuyMarket(Volume);
			}
			// Open short position when bullish pressure fades while still above zero.
			else if (hasShortSignal && Position == 0)
			{
				SellMarket(Volume);
			}
		}

		// Store the most recent average for next candle comparison.
		_previousAverage = averagePower;
	}
}
