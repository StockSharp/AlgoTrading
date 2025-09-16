using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands breakout strategy with optional balance-based position sizing.
/// </summary>
public class BollTradeStrategy : Strategy
{
	private const decimal MaxVolume = 500m;

	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _bandOffset;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<decimal> _lots;
	private readonly StrategyParam<bool> _lotIncrease;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lotBaseline;
	private decimal _pipSize;
	private decimal? _longStop;
	private decimal? _longTarget;
	private decimal? _shortStop;
	private decimal? _shortTarget;

	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	public decimal BollingerDistance
	{
		get => _bandOffset.Value;
		set => _bandOffset.Value = value;
	}

	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	public decimal Lots
	{
		get => _lots.Value;
		set => _lots.Value = value;
	}

	public bool LotIncrease
	{
		get => _lotIncrease.Value;
		set => _lotIncrease.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public BollTradeStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 3m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Take Profit (pips)", "Distance to take profit expressed in pip units.", "Orders")
		.SetCanOptimize(true, 1m, 20m, 1m);

		_stopLoss = Param(nameof(StopLoss), 20m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Stop Loss (pips)", "Distance to stop loss expressed in pip units.", "Orders")
		.SetCanOptimize(true, 5m, 100m, 5m);

		_bandOffset = Param(nameof(BollingerDistance), 3m)
		.SetGreaterOrEqualZero()
		.SetDisplay("Band Offset", "Extra pip offset beyond Bollinger Bands.", "Signals")
		.SetCanOptimize(true, 0m, 10m, 1m);

		_bollingerPeriod = Param(nameof(BollingerPeriod), 4)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Period", "Length of the Bollinger Bands.", "Signals")
		.SetCanOptimize(true, 2, 30, 1);

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Deviation", "Width multiplier of the Bollinger Bands.", "Signals")
		.SetCanOptimize(true, 1m, 4m, 0.5m);

		_lots = Param(nameof(Lots), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Base Volume", "Default trade volume in lots.", "Money Management");

		_lotIncrease = Param(nameof(LotIncrease), true)
		.SetDisplay("Scale Volume", "Increase volume with balance growth.", "Money Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe for signals.", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = Lots;

		_pipSize = CalculatePipSize();
		_lotBaseline = 0m;

		if (LotIncrease && Lots > 0m)
		{
			var balance = Portfolio?.CurrentValue ?? 0m;

			if (balance > 0m)
			_lotBaseline = balance / Lots;
		}

		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
		.Bind(bollinger, ProcessCandle)
		.Start();
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 1m;

		if (step <= 0m)
		step = 1m;

		if (step < 0.01m)
		step *= 10m;

		return step;
	}

	private decimal CalculateVolume()
	{
		var baseVolume = Lots;

		if (!LotIncrease || _lotBaseline <= 0m)
		return baseVolume;

		var balance = Portfolio?.CurrentValue ?? 0m;

		if (balance <= 0m)
		return baseVolume;

		var scaled = baseVolume * (balance / _lotBaseline);

		return Math.Min(scaled, MaxVolume);
	}

	private void ProcessCandle(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var offset = _pipSize * BollingerDistance;
		var upperThreshold = upperBand + offset;
		var lowerThreshold = lowerBand - offset;

		var shouldBuy = candle.ClosePrice < lowerThreshold;
		var shouldSell = candle.ClosePrice > upperThreshold;

		if (Position == 0)
		{
			if (shouldBuy)
			{
				EnterLong(candle);
			}
			else if (shouldSell)
			{
				EnterShort(candle);
			}

			return;
		}

		if (Position > 0)
		{
			// Close long positions when stop loss or take profit levels are hit.
			if ((_longStop.HasValue && candle.LowPrice <= _longStop.Value) ||
				(_longTarget.HasValue && candle.HighPrice >= _longTarget.Value))
			{
				SellMarket(Math.Abs(Position));
				ResetStops();
			}
		}
		else if (Position < 0)
		{
			// Close short positions when stop loss or take profit levels are hit.
			if ((_shortStop.HasValue && candle.HighPrice >= _shortStop.Value) ||
				(_shortTarget.HasValue && candle.LowPrice <= _shortTarget.Value))
			{
				BuyMarket(Math.Abs(Position));
				ResetStops();
			}
		}
	}

	private void EnterLong(ICandleMessage candle)
	{
		var volume = CalculateVolume();

		if (volume <= 0m)
		return;

		BuyMarket(volume);

		// Store exit targets for the newly opened long trade.
		_longStop = StopLoss > 0m ? candle.ClosePrice - _pipSize * StopLoss : null;
		_longTarget = TakeProfit > 0m ? candle.ClosePrice + _pipSize * TakeProfit : null;
		_shortStop = null;
		_shortTarget = null;
	}

	private void EnterShort(ICandleMessage candle)
	{
		var volume = CalculateVolume();

		if (volume <= 0m)
		return;

		SellMarket(volume);

		// Store exit targets for the newly opened short trade.
		_shortStop = StopLoss > 0m ? candle.ClosePrice + _pipSize * StopLoss : null;
		_shortTarget = TakeProfit > 0m ? candle.ClosePrice - _pipSize * TakeProfit : null;
		_longStop = null;
		_longTarget = null;
	}

	private void ResetStops()
	{
		// Clear cached exit levels after a position is closed.
		_longStop = null;
		_longTarget = null;
		_shortStop = null;
		_shortTarget = null;
	}
}
