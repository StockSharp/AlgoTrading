using System;
using System.Collections.Generic;
using System.Globalization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ADX filtered smoothed moving average crossover strategy.
/// Opens trades when the previous candle crosses the smoothed MA and ADX confirms the trend.
/// Adds configurable take profit, stop loss and trailing stop distances measured in pips.
/// </summary>
public class AdxMaCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<decimal> _takeProfitBuy;
	private readonly StrategyParam<decimal> _stopLossBuy;
	private readonly StrategyParam<decimal> _trailingStopBuy;
	private readonly StrategyParam<decimal> _takeProfitSell;
	private readonly StrategyParam<decimal> _stopLossSell;
	private readonly StrategyParam<decimal> _trailingStopSell;
	private readonly StrategyParam<DataType> _candleType;

	private SmoothedMovingAverage _ma = null!;
	private AverageDirectionalIndex _adx = null!;
	private decimal _pipSize;
	private decimal _prevClose;
	private decimal _prevPrevClose;
	private decimal _prevMa;
	private decimal _prevAdx;
	private bool _hasPrev;
	private bool _hasPrevPrev;

	private decimal _longEntryPrice;
	private decimal _longStopPrice;
	private decimal _longTakeProfitPrice;
	private decimal _shortEntryPrice;
	private decimal _shortStopPrice;
	private decimal _shortTakeProfitPrice;

	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	public decimal TakeProfitBuy
	{
		get => _takeProfitBuy.Value;
		set => _takeProfitBuy.Value = value;
	}

	public decimal StopLossBuy
	{
		get => _stopLossBuy.Value;
		set => _stopLossBuy.Value = value;
	}

	public decimal TrailingStopBuy
	{
		get => _trailingStopBuy.Value;
		set => _trailingStopBuy.Value = value;
	}

	public decimal TakeProfitSell
	{
		get => _takeProfitSell.Value;
		set => _takeProfitSell.Value = value;
	}

	public decimal StopLossSell
	{
		get => _stopLossSell.Value;
		set => _stopLossSell.Value = value;
	}

	public decimal TrailingStopSell
	{
		get => _trailingStopSell.Value;
		set => _trailingStopSell.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public AdxMaCrossoverStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 15)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period of the smoothed moving average", "General")
			.SetCanOptimize(true);
		_adxPeriod = Param(nameof(AdxPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Smoothing period for Average Directional Index", "Indicators")
			.SetCanOptimize(true);
		_adxThreshold = Param(nameof(AdxThreshold), 16m)
			.SetDisplay("ADX Threshold", "Minimum ADX value required to trade", "Indicators")
			.SetCanOptimize(true);
		_takeProfitBuy = Param(nameof(TakeProfitBuy), 83m)
			.SetDisplay("Buy Take Profit (pips)", "Take profit distance for long trades", "Risk Management")
			.SetNotNegative();
		_stopLossBuy = Param(nameof(StopLossBuy), 55m)
			.SetDisplay("Buy Stop Loss (pips)", "Stop loss distance for long trades", "Risk Management")
			.SetNotNegative();
		_trailingStopBuy = Param(nameof(TrailingStopBuy), 27m)
			.SetDisplay("Buy Trailing Stop (pips)", "Trailing stop distance for long trades", "Risk Management")
			.SetNotNegative();
		_takeProfitSell = Param(nameof(TakeProfitSell), 63m)
			.SetDisplay("Sell Take Profit (pips)", "Take profit distance for short trades", "Risk Management")
			.SetNotNegative();
		_stopLossSell = Param(nameof(StopLossSell), 50m)
			.SetDisplay("Sell Stop Loss (pips)", "Stop loss distance for short trades", "Risk Management")
			.SetNotNegative();
		_trailingStopSell = Param(nameof(TrailingStopSell), 27m)
			.SetDisplay("Sell Trailing Stop (pips)", "Trailing stop distance for short trades", "Risk Management")
			.SetNotNegative();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for calculations", "General");
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

		_ma?.Reset();
		_adx?.Reset();

		_prevClose = 0m;
		_prevPrevClose = 0m;
		_prevMa = 0m;
		_prevAdx = 0m;
		_hasPrev = false;
		_hasPrevPrev = false;

		ResetLongTargets();
		ResetShortTargets();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma = new SmoothedMovingAverage { Length = MaPeriod };
		_adx = new AverageDirectionalIndex { Length = AdxPeriod };
		_pipSize = CalculatePipSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);

			var adxArea = CreateChartArea();
			if (adxArea != null)
			{
				DrawIndicator(adxArea, _adx);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue adxValue)
	{
		// Only react to closed candles to match the MQL implementation.
		if (candle.State != CandleStates.Finished)
			return;

		var median = (candle.HighPrice + candle.LowPrice) / 2m;
		var maValue = _ma.Process(median, candle.OpenTime, true);

		if (!maValue.IsFinal || !adxValue.IsFinal)
			return;

		var ma = maValue.GetValue<decimal>();
		var adx = ((AverageDirectionalIndexValue)adxValue).MovingAverage;
		var close = candle.ClosePrice;

		if (_hasPrev && _hasPrevPrev)
		{
			ManageOpenPositions(close);

			if (IsFormedAndOnlineAndAllowTrading())
			{
				var longSignal = _prevClose > _prevMa && _prevPrevClose < _prevMa && _prevAdx >= AdxThreshold;
				var shortSignal = _prevClose < _prevMa && _prevPrevClose > _prevMa && _prevAdx >= AdxThreshold;

				if (longSignal && Position <= 0)
				{
					var volume = Volume + Math.Abs(Position);
					if (volume > 0)
					{
						BuyMarket(volume);
						InitializeLongTargets(_prevClose);
					}
				}
				else if (shortSignal && Position >= 0)
				{
					var volume = Volume + Math.Abs(Position);
					if (volume > 0)
					{
						SellMarket(volume);
						InitializeShortTargets(_prevClose);
					}
				}
			}
		}

		UpdateHistory(close, ma, adx);
	}

	private void ManageOpenPositions(decimal currentClose)
	{
		// Manage long position exits before evaluating new entries.
		if (Position > 0)
		{
			if (_prevClose < _prevMa)
			{
				SellMarket(Position);
				ResetLongTargets();
				return;
			}

			UpdateLongTrailing(currentClose);

			if (_longTakeProfitPrice > 0m && currentClose >= _longTakeProfitPrice)
			{
				SellMarket(Position);
				ResetLongTargets();
				return;
			}

			if (_longStopPrice > 0m && currentClose <= _longStopPrice)
			{
				SellMarket(Position);
				ResetLongTargets();
				return;
			}
		}
		else if (Position < 0)
		{
			if (_prevClose > _prevMa)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortTargets();
				return;
			}

			UpdateShortTrailing(currentClose);

			if (_shortTakeProfitPrice > 0m && currentClose <= _shortTakeProfitPrice)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortTargets();
				return;
			}

			if (_shortStopPrice > 0m && currentClose >= _shortStopPrice)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortTargets();
				return;
			}
		}
		else
		{
			ResetLongTargets();
			ResetShortTargets();
		}
	}

	private void UpdateLongTrailing(decimal currentClose)
	{
		if (TrailingStopBuy <= 0m || _longEntryPrice <= 0m)
			return;

		var trailingDistance = TrailingStopBuy * _pipSize;
		if (trailingDistance <= 0m)
			return;

		var profit = currentClose - _longEntryPrice;
		if (profit <= trailingDistance)
			return;

		var newStop = currentClose - trailingDistance;
		if (newStop > _longStopPrice)
			_longStopPrice = newStop;
	}

	private void UpdateShortTrailing(decimal currentClose)
	{
		if (TrailingStopSell <= 0m || _shortEntryPrice <= 0m)
			return;

		var trailingDistance = TrailingStopSell * _pipSize;
		if (trailingDistance <= 0m)
			return;

		var profit = _shortEntryPrice - currentClose;
		if (profit <= trailingDistance)
			return;

		var newStop = currentClose + trailingDistance;
		if (_shortStopPrice == 0m || newStop < _shortStopPrice)
			_shortStopPrice = newStop;
	}

	private void InitializeLongTargets(decimal entryPrice)
	{
		_longEntryPrice = entryPrice;
		_longStopPrice = StopLossBuy > 0m ? entryPrice - StopLossBuy * _pipSize : 0m;
		_longTakeProfitPrice = TakeProfitBuy > 0m ? entryPrice + TakeProfitBuy * _pipSize : 0m;

		ResetShortTargets();
	}

	private void InitializeShortTargets(decimal entryPrice)
	{
		_shortEntryPrice = entryPrice;
		_shortStopPrice = StopLossSell > 0m ? entryPrice + StopLossSell * _pipSize : 0m;
		_shortTakeProfitPrice = TakeProfitSell > 0m ? entryPrice - TakeProfitSell * _pipSize : 0m;

		ResetLongTargets();
	}

	private void ResetLongTargets()
	{
		_longEntryPrice = 0m;
		_longStopPrice = 0m;
		_longTakeProfitPrice = 0m;
	}

	private void ResetShortTargets()
	{
		_shortEntryPrice = 0m;
		_shortStopPrice = 0m;
		_shortTakeProfitPrice = 0m;
	}

	private void UpdateHistory(decimal close, decimal ma, decimal adx)
	{
		if (_hasPrev)
		{
			_prevPrevClose = _prevClose;
			_hasPrevPrev = true;
		}
		else
		{
			_hasPrevPrev = false;
		}

		_prevClose = close;
		_prevMa = ma;
		_prevAdx = adx;
		_hasPrev = true;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		var decimals = GetDecimalPlaces(step);
		if (decimals == 3 || decimals == 5)
			return step * 10m;

		return step;
	}

	private static int GetDecimalPlaces(decimal value)
	{
		var text = value.ToString(CultureInfo.InvariantCulture);
		var separatorIndex = text.IndexOf('.') >= 0 ? text.IndexOf('.') : text.IndexOf(',');
		if (separatorIndex < 0)
			return 0;

		return text.Length - separatorIndex - 1;
	}
}
