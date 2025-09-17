using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy that compares the latest close with yesterday's range.
/// Opens long positions when price closes above the previous high and short positions when price closes below the previous low.
/// </summary>
public class YesterdayTodayStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<DataType> _signalCandleType;

	private decimal _pipSize;
	private decimal? _yesterdayHigh;
	private decimal? _yesterdayLow;
	private ICandleMessage _lastDailyCandle;
	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;

	/// <summary>
	/// Volume for new trades.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
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
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Intraday candle type used to monitor breakouts.
	/// </summary>
	public DataType SignalCandleType
	{
		get => _signalCandleType.Value;
		set => _signalCandleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="YesterdayTodayStrategy"/>.
	/// </summary>
	public YesterdayTodayStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Trade Volume", "Volume used for new market orders", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 5m, 0.1m);

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 150m, 10m);

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 200m, 10m);

		_signalCandleType = Param(nameof(SignalCandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Signal Candle Type", "Intraday candles for signal detection", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, SignalCandleType), (Security, TimeSpan.FromDays(1).TimeFrame())];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_pipSize = 0m;
		_yesterdayHigh = null;
		_yesterdayLow = null;
		_lastDailyCandle = null;
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		// Enable account protection (for example margin call prevention).
		StartProtection();

		var signalSubscription = SubscribeCandles(SignalCandleType);
		signalSubscription
			.Bind(ProcessSignalCandle)
			.Start();

		var dailySubscription = SubscribeCandles(TimeSpan.FromDays(1).TimeFrame());
		dailySubscription
			.Bind(ProcessDailyCandle)
			.Start();

		var mainArea = CreateChartArea();
		if (mainArea != null)
		{
			DrawCandles(mainArea, signalSubscription);
			DrawOwnTrades(mainArea);

			var dailyArea = CreateChartArea();
			DrawCandles(dailyArea, dailySubscription);
		}
	}

	private void ProcessDailyCandle(ICandleMessage candle)
	{
		// Wait for completed daily candles to update yesterday's range.
		if (candle.State != CandleStates.Finished)
			return;

		if (_lastDailyCandle != null)
		{
			_yesterdayHigh = _lastDailyCandle.HighPrice;
			_yesterdayLow = _lastDailyCandle.LowPrice;
		}

		_lastDailyCandle = candle;
	}

	private void ProcessSignalCandle(ICandleMessage candle)
	{
		// Only analyze fully formed candles to avoid repeated signals inside the same bar.
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_pipSize <= 0m)
			_pipSize = CalculatePipSize();

		// Check stop-loss and take-profit conditions before searching for new entries.
		if (CheckRisk(candle))
			return;

		if (_yesterdayHigh == null || _yesterdayLow == null)
			return;

		var closePrice = candle.ClosePrice;
		var breakoutUp = closePrice > _yesterdayHigh.Value;
		var breakoutDown = closePrice < _yesterdayLow.Value;

		if (breakoutUp && Position <= 0)
		{
			var volume = GetAdjustedVolume(Position < 0);
			if (volume > 0m)
			{
				BuyMarket(volume);
				SetRiskLevels(closePrice, true);
			}
			return;
		}

		if (breakoutDown && Position >= 0)
		{
			var volume = GetAdjustedVolume(Position > 0);
			if (volume > 0m)
			{
				SellMarket(volume);
				SetRiskLevels(closePrice, false);
			}
		}
	}

	private decimal GetAdjustedVolume(bool hasOppositePosition)
	{
		// Combine configured trade volume with any opposing position that must be closed.
		var baseVolume = TradeVolume;
		if (baseVolume <= 0m)
			baseVolume = 1m;

		if (hasOppositePosition)
			baseVolume += Math.Abs(Position);

		return baseVolume;
	}

	private bool CheckRisk(ICandleMessage candle)
	{
		if (Position > 0)
		{
			var size = Math.Abs(Position);

			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				SellMarket(size);
				ResetRiskLevels();
				return true;
			}

			if (_takeProfitPrice.HasValue && candle.HighPrice >= _takeProfitPrice.Value)
			{
				SellMarket(size);
				ResetRiskLevels();
				return true;
			}
		}
		else if (Position < 0)
		{
			var size = Math.Abs(Position);

			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				BuyMarket(size);
				ResetRiskLevels();
				return true;
			}

			if (_takeProfitPrice.HasValue && candle.LowPrice <= _takeProfitPrice.Value)
			{
				BuyMarket(size);
				ResetRiskLevels();
				return true;
			}
		}

		return false;
	}

	private void SetRiskLevels(decimal entryPrice, bool isLong)
	{
		_entryPrice = entryPrice;

		if (_pipSize <= 0m)
		{
			_stopPrice = null;
			_takeProfitPrice = null;
			return;
		}

		var stopDistance = StopLossPips > 0m ? StopLossPips * _pipSize : 0m;
		var takeDistance = TakeProfitPips > 0m ? TakeProfitPips * _pipSize : 0m;

		if (isLong)
		{
			_stopPrice = stopDistance > 0m ? entryPrice - stopDistance : null;
			_takeProfitPrice = takeDistance > 0m ? entryPrice + takeDistance : null;
		}
		else
		{
			_stopPrice = stopDistance > 0m ? entryPrice + stopDistance : null;
			_takeProfitPrice = takeDistance > 0m ? entryPrice - takeDistance : null;
		}
	}

	private void ResetRiskLevels()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
	}

	private decimal CalculatePipSize()
	{
		if (Security == null)
			return 0m;

		var step = Security.PriceStep ?? 0m;
		if (step <= 0m)
		{
			var decimals = Security.Decimals;
			if (decimals.HasValue)
				step = (decimal)Math.Pow(10, -decimals.Value);
		}

		if (step <= 0m)
			return 0m;

		var decimalsCount = Security.Decimals ?? 0;
		if (decimalsCount == 3 || decimalsCount == 5)
			return step * 10m;

		return step;
	}
}
