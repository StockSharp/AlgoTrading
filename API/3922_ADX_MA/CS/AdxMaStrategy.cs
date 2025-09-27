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
/// Port of the MetaTrader expert ADX_MA.
/// Combines a smoothed moving average trend filter with an ADX strength confirmation.
/// Applies asymmetric stop-loss, take-profit, and trailing-stop management for long and short positions.
/// </summary>
public class AdxMaStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<decimal> _takeProfitBuyPips;
	private readonly StrategyParam<decimal> _stopLossBuyPips;
	private readonly StrategyParam<decimal> _trailingStopBuyPips;
	private readonly StrategyParam<decimal> _takeProfitSellPips;
	private readonly StrategyParam<decimal> _stopLossSellPips;
	private readonly StrategyParam<decimal> _trailingStopSellPips;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;

	private decimal? _previousClose;
	private decimal? _previousPreviousClose;
	private decimal? _previousMa;
	private decimal? _previousAdx;

	private decimal? _longEntryPrice;
	private decimal? _longStopPrice;
	private decimal? _longTakePrice;

	private decimal? _shortEntryPrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;

	/// <summary>
	/// Period of the smoothed moving average calculated on median prices.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Period of the Average Directional Index.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// Minimum ADX value required to enable entries.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Take-profit distance for long trades measured in pips.
	/// </summary>
	public decimal TakeProfitBuyPips
	{
		get => _takeProfitBuyPips.Value;
		set => _takeProfitBuyPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for long trades measured in pips.
	/// </summary>
	public decimal StopLossBuyPips
	{
		get => _stopLossBuyPips.Value;
		set => _stopLossBuyPips.Value = value;
	}

	/// <summary>
	/// Trailing-stop distance for long trades measured in pips.
	/// </summary>
	public decimal TrailingStopBuyPips
	{
		get => _trailingStopBuyPips.Value;
		set => _trailingStopBuyPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance for short trades measured in pips.
	/// </summary>
	public decimal TakeProfitSellPips
	{
		get => _takeProfitSellPips.Value;
		set => _takeProfitSellPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance for short trades measured in pips.
	/// </summary>
	public decimal StopLossSellPips
	{
		get => _stopLossSellPips.Value;
		set => _stopLossSellPips.Value = value;
	}

	/// <summary>
	/// Trailing-stop distance for short trades measured in pips.
	/// </summary>
	public decimal TrailingStopSellPips
	{
		get => _trailingStopSellPips.Value;
		set => _trailingStopSellPips.Value = value;
	}

	/// <summary>
	/// Order volume used for new entries.
	/// </summary>
	public decimal VolumePerTrade
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Type of candles used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AdxMaStrategy"/> class.
	/// </summary>
	public AdxMaStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("SMMA Period", "Length of the smoothed moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 5);

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Length of the ADX indicator", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 1);

		_adxThreshold = Param(nameof(AdxThreshold), 16m)
			.SetNotNegative()
			.SetDisplay("ADX Threshold", "Minimum ADX required to trade", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10m, 40m, 2m);

		_takeProfitBuyPips = Param(nameof(TakeProfitBuyPips), 1300m)
			.SetNotNegative()
			.SetDisplay("Long Take Profit (pips)", "Take-profit distance for buy trades", "Risk Management");

		_stopLossBuyPips = Param(nameof(StopLossBuyPips), 30m)
			.SetNotNegative()
			.SetDisplay("Long Stop Loss (pips)", "Stop-loss distance for buy trades", "Risk Management");

		_trailingStopBuyPips = Param(nameof(TrailingStopBuyPips), 270m)
			.SetNotNegative()
			.SetDisplay("Long Trailing Stop (pips)", "Trailing-stop distance for buy trades", "Risk Management");

		_takeProfitSellPips = Param(nameof(TakeProfitSellPips), 160m)
			.SetNotNegative()
			.SetDisplay("Short Take Profit (pips)", "Take-profit distance for sell trades", "Risk Management");

		_stopLossSellPips = Param(nameof(StopLossSellPips), 50m)
			.SetNotNegative()
			.SetDisplay("Short Stop Loss (pips)", "Stop-loss distance for sell trades", "Risk Management");

		_trailingStopSellPips = Param(nameof(TrailingStopSellPips), 20m)
			.SetNotNegative()
			.SetDisplay("Short Trailing Stop (pips)", "Trailing-stop distance for sell trades", "Risk Management");

		_volume = Param(nameof(VolumePerTrade), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 1m, 0.05m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series", "Trading");
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

		_previousClose = null;
		_previousPreviousClose = null;
		_previousMa = null;
		_previousAdx = null;

		_longEntryPrice = null;
		_longStopPrice = null;
		_longTakePrice = null;

		_shortEntryPrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;

		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ma = new SmoothedMovingAverage
		{
			Length = MaPeriod,
			CandlePrice = CandlePrice.Median
		};

		var adx = new AverageDirectionalIndex
		{
			Length = AdxPeriod,
			CandlePrice = CandlePrice.Median
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma, adx, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawIndicator(area, adx);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_pipSize <= 0m)
			_pipSize = GetPipSize();

		ManageOpenPosition(candle);

		if (_previousClose is decimal prevClose &&
			_previousPreviousClose is decimal prevPrevClose &&
			_previousMa is decimal prevMa &&
			_previousAdx is decimal prevAdx)
		{
			var hasStrongTrend = prevAdx > AdxThreshold;

			if (hasStrongTrend && prevClose > prevMa && prevPrevClose < prevMa)
				OpenLong(candle);
			else if (hasStrongTrend && prevClose < prevMa && prevPrevClose > prevMa)
				OpenShort(candle);
		}

		_previousAdx = adxValue;
		_previousMa = maValue;
		_previousPreviousClose = _previousClose;
		_previousClose = candle.ClosePrice;
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_previousClose is decimal prevClose && _previousMa is decimal prevMa && prevClose < prevMa)
			{
				CloseLong();
			}
			else
			{
				UpdateLongProtection(candle);
			}
		}
		else if (Position < 0)
		{
			if (_previousClose is decimal prevClose && _previousMa is decimal prevMa && prevClose > prevMa)
			{
				CloseShort();
			}
			else
			{
				UpdateShortProtection(candle);
			}
		}
	}

	private void OpenLong(ICandleMessage candle)
	{
		var volume = VolumePerTrade + (Position < 0 ? Math.Abs(Position) : 0m);
		if (volume <= 0m)
			return;

		BuyMarket(volume);

		var entryPrice = candle.ClosePrice;
		_longEntryPrice = entryPrice;
		_longStopPrice = StopLossBuyPips > 0m ? ShrinkPrice(entryPrice - StopLossBuyPips * _pipSize) : (decimal?)null;
		_longTakePrice = TakeProfitBuyPips > 0m ? ShrinkPrice(entryPrice + TakeProfitBuyPips * _pipSize) : (decimal?)null;
	}

	private void OpenShort(ICandleMessage candle)
	{
		var volume = VolumePerTrade + (Position > 0 ? Position : 0m);
		if (volume <= 0m)
			return;

		SellMarket(volume);

		var entryPrice = candle.ClosePrice;
		_shortEntryPrice = entryPrice;
		_shortStopPrice = StopLossSellPips > 0m ? ShrinkPrice(entryPrice + StopLossSellPips * _pipSize) : (decimal?)null;
		_shortTakePrice = TakeProfitSellPips > 0m ? ShrinkPrice(entryPrice - TakeProfitSellPips * _pipSize) : (decimal?)null;
	}

	private void UpdateLongProtection(ICandleMessage candle)
	{
		if (_longEntryPrice is null)
			return;

		var longEntry = _longEntryPrice.Value;

		if (_longTakePrice is decimal takePrice && candle.HighPrice >= takePrice)
		{
			CloseLong();
			return;
		}

		if (_longStopPrice is decimal stopPrice && candle.LowPrice <= stopPrice)
		{
			CloseLong();
			return;
		}

		if (TrailingStopBuyPips <= 0m)
			return;

		var trailingDistance = TrailingStopBuyPips * _pipSize;
		if (trailingDistance <= 0m)
			return;

		var desiredStop = ShrinkPrice(candle.ClosePrice - trailingDistance);
		if (candle.ClosePrice - longEntry >= trailingDistance &&
			(_longStopPrice is null || desiredStop > _longStopPrice.Value))
		{
			_longStopPrice = desiredStop;
		}
	}

	private void UpdateShortProtection(ICandleMessage candle)
	{
		if (_shortEntryPrice is null)
			return;

		var shortEntry = _shortEntryPrice.Value;

		if (_shortTakePrice is decimal takePrice && candle.LowPrice <= takePrice)
		{
			CloseShort();
			return;
		}

		if (_shortStopPrice is decimal stopPrice && candle.HighPrice >= stopPrice)
		{
			CloseShort();
			return;
		}

		if (TrailingStopSellPips <= 0m)
			return;

		var trailingDistance = TrailingStopSellPips * _pipSize;
		if (trailingDistance <= 0m)
			return;

		var desiredStop = ShrinkPrice(candle.ClosePrice + trailingDistance);
		if (shortEntry - candle.ClosePrice >= trailingDistance &&
			(_shortStopPrice is null || desiredStop < _shortStopPrice.Value))
		{
			_shortStopPrice = desiredStop;
		}
	}

	private void CloseLong()
	{
		var volume = Math.Abs(Position);
		if (volume > 0m)
			SellMarket(volume);

		_longEntryPrice = null;
		_longStopPrice = null;
		_longTakePrice = null;
	}

	private void CloseShort()
	{
		var volume = Math.Abs(Position);
		if (volume > 0m)
			BuyMarket(volume);

		_shortEntryPrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
	}

	private decimal GetPipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? step : 1m;
	}

	private decimal ShrinkPrice(decimal price)
	{
		return Security?.ShrinkPrice(price) ?? price;
	}
}

