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

using StockSharp.Algo;
using StockSharp.Algo.Candles;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that reproduces the Surfing 3.0 expert advisor logic from MetaTrader.
/// </summary>
public class Surfing30Strategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _longRsiThreshold;
	private readonly StrategyParam<decimal> _shortRsiThreshold;
	private readonly StrategyParam<int> _tradeStartHour;
	private readonly StrategyParam<int> _tradeEndHour;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _emaHigh = null!;
	private EMA _emaLow = null!;
	private RelativeStrengthIndex _rsi = null!;

	private decimal? _previousClose;
	private decimal? _previousHighEma;
	private decimal? _previousLowEma;

	private decimal? _stopLossPrice;
	private decimal? _takeProfitPrice;

	/// <summary>
	/// Initialize <see cref="Surfing30Strategy"/>.
	/// </summary>
	public Surfing30Strategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume applied to every trade.", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 5m, 0.1m);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 80)
			.SetNotNegative()
			.SetDisplay("Take Profit Points", "Distance to the take profit in instrument points.", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10, 200, 10);

		_stopLossPoints = Param(nameof(StopLossPoints), 50)
			.SetNotNegative()
			.SetDisplay("Stop Loss Points", "Distance to the stop loss in instrument points.", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10, 150, 10);

		_maPeriod = Param(nameof(MaPeriod), 50)
			.SetGreaterThan(1)
			.SetDisplay("EMA Period", "Length of the exponential moving averages calculated over highs and lows.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 120, 5);

		_rsiPeriod = Param(nameof(RsiPeriod), 10)
			.SetGreaterThan(1)
			.SetDisplay("RSI Period", "Length of the RSI filter.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_longRsiThreshold = Param(nameof(LongRsiThreshold), 40m)
			.SetDisplay("Long RSI Threshold", "Minimum RSI value required for long entries.", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(20m, 60m, 5m);

		_shortRsiThreshold = Param(nameof(ShortRsiThreshold), 65m)
			.SetDisplay("Short RSI Threshold", "Maximum RSI value allowed for short entries.", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(40m, 80m, 5m);

		_tradeStartHour = Param(nameof(TradeStartHour), 8)
			.SetDisplay("Trade Start Hour", "Hour of the day when new trades may start.", "Sessions")
			.SetCanOptimize(false);

		_tradeEndHour = Param(nameof(TradeEndHour), 18)
			.SetDisplay("Trade End Hour", "Hour of the day when all positions are closed.", "Sessions")
			.SetCanOptimize(false);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Aggregation used for calculations.", "Data");
	}

	/// <summary>
	/// Volume used for every trade.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set
		{
			_orderVolume.Value = value;
			Volume = value;
		}
	}

	/// <summary>
	/// Distance to the take profit in instrument points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Distance to the stop loss in instrument points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Length of the exponential moving averages calculated over candle highs and lows.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Length of the RSI filter.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Minimum RSI value required for long entries.
	/// </summary>
	public decimal LongRsiThreshold
	{
		get => _longRsiThreshold.Value;
		set => _longRsiThreshold.Value = value;
	}

	/// <summary>
	/// Maximum RSI value allowed for short entries.
	/// </summary>
	public decimal ShortRsiThreshold
	{
		get => _shortRsiThreshold.Value;
		set => _shortRsiThreshold.Value = value;
	}

	/// <summary>
	/// Hour of the day when new trades may start.
	/// </summary>
	public int TradeStartHour
	{
		get => _tradeStartHour.Value;
		set => _tradeStartHour.Value = value;
	}

	/// <summary>
	/// Hour of the day when all positions are closed.
	/// </summary>
	public int TradeEndHour
	{
		get => _tradeEndHour.Value;
		set => _tradeEndHour.Value = value;
	}

	/// <summary>
	/// Candle aggregation used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_previousClose = null;
		_previousHighEma = null;
		_previousLowEma = null;
		_stopLossPrice = null;
		_takeProfitPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		_emaHigh = new EMA { Length = MaPeriod };
		_emaLow = new EMA { Length = MaPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var endHour = TradeEndHour;
		if (endHour >= 0 && candle.OpenTime.Hour >= endHour)
		{
			if (Position != 0)
			{
				ClosePosition();
				ResetTargets();
			}

			ResetHistory();
			return;
		}

		var emaHighValue = _emaHigh.Process(new CandleIndicatorValue(candle, candle.HighPrice));
		var emaLowValue = _emaLow.Process(new CandleIndicatorValue(candle, candle.LowPrice));
		var rsiValue = _rsi.Process(new CandleIndicatorValue(candle, candle.ClosePrice));

		if (!_emaHigh.IsFormed || !_emaLow.IsFormed || !_rsi.IsFormed)
		{
			UpdateHistory(candle.ClosePrice, emaHighValue.GetValue<decimal>(), emaLowValue.GetValue<decimal>());
			return;
		}

		var currentClose = candle.ClosePrice;
		var currentHighEma = emaHighValue.GetValue<decimal>();
		var currentLowEma = emaLowValue.GetValue<decimal>();
		var rsi = rsiValue.GetValue<decimal>();

		if (ManageActivePosition(candle))
		{
			UpdateHistory(currentClose, currentHighEma, currentLowEma);
			return;
		}

		if (!IsWithinTradeHours(candle.OpenTime))
		{
			UpdateHistory(currentClose, currentHighEma, currentLowEma);
			return;
		}

		if (_previousClose is null || _previousHighEma is null || _previousLowEma is null)
		{
			UpdateHistory(currentClose, currentHighEma, currentLowEma);
			return;
		}

		var previousClose = _previousClose.Value;
		var previousHighEma = _previousHighEma.Value;
		var previousLowEma = _previousLowEma.Value;

		var buySignal = previousClose <= previousHighEma && currentClose > currentHighEma && rsi > LongRsiThreshold;
		var sellSignal = previousClose >= previousLowEma && currentClose < currentLowEma && rsi < ShortRsiThreshold;

		if (buySignal && Position <= 0)
		{
			if (Position < 0)
			{
				ClosePosition();
				ResetTargets();
			}

			BuyMarket(OrderVolume);
			SetTargets(currentClose, true);
		}
		else if (sellSignal && Position >= 0)
		{
			if (Position > 0)
			{
				ClosePosition();
				ResetTargets();
			}

			SellMarket(OrderVolume);
			SetTargets(currentClose, false);
		}

		UpdateHistory(currentClose, currentHighEma, currentLowEma);
	}

	private bool ManageActivePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_stopLossPrice is not null && candle.LowPrice <= _stopLossPrice)
			{
				ClosePosition();
				ResetTargets();
				return true;
			}

			if (_takeProfitPrice is not null && candle.HighPrice >= _takeProfitPrice)
			{
				ClosePosition();
				ResetTargets();
				return true;
			}
		}
		else if (Position < 0)
		{
			if (_stopLossPrice is not null && candle.HighPrice >= _stopLossPrice)
			{
				ClosePosition();
				ResetTargets();
				return true;
			}

			if (_takeProfitPrice is not null && candle.LowPrice <= _takeProfitPrice)
			{
				ClosePosition();
				ResetTargets();
				return true;
			}
		}

		return false;
	}

	private void SetTargets(decimal entryPrice, bool isLong)
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
			priceStep = 1m;

		if (isLong)
		{
			_stopLossPrice = StopLossPoints > 0 ? entryPrice - StopLossPoints * priceStep : null;
			_takeProfitPrice = TakeProfitPoints > 0 ? entryPrice + TakeProfitPoints * priceStep : null;
		}
		else
		{
			_stopLossPrice = StopLossPoints > 0 ? entryPrice + StopLossPoints * priceStep : null;
			_takeProfitPrice = TakeProfitPoints > 0 ? entryPrice - TakeProfitPoints * priceStep : null;
		}
	}

	private void ResetTargets()
	{
		_stopLossPrice = null;
		_takeProfitPrice = null;
	}

	private void UpdateHistory(decimal currentClose, decimal currentHighEma, decimal currentLowEma)
	{
		_previousClose = currentClose;
		_previousHighEma = currentHighEma;
		_previousLowEma = currentLowEma;
	}

	private void ResetHistory()
	{
		_previousClose = null;
		_previousHighEma = null;
		_previousLowEma = null;
	}

	private bool IsWithinTradeHours(DateTimeOffset time)
	{
		var startHour = TradeStartHour;
		var endHour = TradeEndHour;

		if (endHour <= startHour)
			return time.Hour >= startHour || time.Hour < endHour;

		return time.Hour >= startHour && time.Hour < endHour;
	}
}

