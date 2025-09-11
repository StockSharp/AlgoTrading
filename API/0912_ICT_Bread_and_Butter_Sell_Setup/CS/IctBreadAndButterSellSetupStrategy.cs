using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ICT Bread and Butter Sell-Setup strategy.
/// Tracks session highs and lows and trades specific setups around them.
/// </summary>
public class IctBreadAndButterSellSetupStrategy : Strategy
{
	private readonly StrategyParam<int> _shortStopTicks;
	private readonly StrategyParam<int> _shortTakeTicks;
	private readonly StrategyParam<int> _buyStopTicks;
	private readonly StrategyParam<int> _buyTakeTicks;
	private readonly StrategyParam<int> _asiaStopTicks;
	private readonly StrategyParam<int> _asiaTakeTicks;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _londonHigh;
	private decimal _londonLow;
	private decimal _nyHigh;
	private decimal _nyLow;
	private decimal _asiaHigh;
	private decimal _asiaLow;

	private bool _inLondon;
	private bool _inNy;
	private bool _inAsia;

	/// <summary>
	/// Stop loss ticks for NY short entry.
	/// </summary>
	public int ShortStopTicks
	{
		get => _shortStopTicks.Value;
		set => _shortStopTicks.Value = value;
	}

	/// <summary>
	/// Take profit ticks for NY short entry.
	/// </summary>
	public int ShortTakeTicks
	{
		get => _shortTakeTicks.Value;
		set => _shortTakeTicks.Value = value;
	}

	/// <summary>
	/// Stop loss ticks for London close buy.
	/// </summary>
	public int BuyStopTicks
	{
		get => _buyStopTicks.Value;
		set => _buyStopTicks.Value = value;
	}

	/// <summary>
	/// Take profit ticks for London close buy.
	/// </summary>
	public int BuyTakeTicks
	{
		get => _buyTakeTicks.Value;
		set => _buyTakeTicks.Value = value;
	}

	/// <summary>
	/// Stop loss ticks for Asia sell entry.
	/// </summary>
	public int AsiaStopTicks
	{
		get => _asiaStopTicks.Value;
		set => _asiaStopTicks.Value = value;
	}

	/// <summary>
	/// Take profit ticks for Asia sell entry.
	/// </summary>
	public int AsiaTakeTicks
	{
		get => _asiaTakeTicks.Value;
		set => _asiaTakeTicks.Value = value;
	}

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public IctBreadAndButterSellSetupStrategy()
	{
		_shortStopTicks = Param(nameof(ShortStopTicks), 10)
			.SetGreaterThanZero()
			.SetDisplay("Short Stop Ticks", "Stop loss ticks for NY short entry", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_shortTakeTicks = Param(nameof(ShortTakeTicks), 20)
			.SetGreaterThanZero()
			.SetDisplay("Short Take Profit Ticks", "Take profit ticks for NY short entry", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_buyStopTicks = Param(nameof(BuyStopTicks), 10)
			.SetGreaterThanZero()
			.SetDisplay("Buy Stop Ticks", "Stop loss ticks for London close buy", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_buyTakeTicks = Param(nameof(BuyTakeTicks), 20)
			.SetGreaterThanZero()
			.SetDisplay("Buy Take Profit Ticks", "Take profit ticks for London close buy", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_asiaStopTicks = Param(nameof(AsiaStopTicks), 10)
			.SetGreaterThanZero()
			.SetDisplay("Asia Stop Ticks", "Stop loss ticks for Asia sell entry", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_asiaTakeTicks = Param(nameof(AsiaTakeTicks), 15)
			.SetGreaterThanZero()
			.SetDisplay("Asia Take Profit Ticks", "Take profit ticks for Asia sell entry", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_inLondon = false;
		_inNy = false;
		_inAsia = false;
		_londonHigh = _londonLow = 0m;
		_nyHigh = _nyLow = 0m;
		_asiaHigh = _asiaLow = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.OpenTime;
		var date = time.Date;

		var sessionNyOpen = new DateTimeOffset(date.Year, date.Month, date.Day, 8, 20, 0, time.Offset);
		var sessionLondon = new DateTimeOffset(date.Year, date.Month, date.Day, 2, 0, 0, time.Offset);
		var sessionAsia = new DateTimeOffset(date.Year, date.Month, date.Day, 19, 0, 0, time.Offset);
		var sessionEnd = new DateTimeOffset(date.Year, date.Month, date.Day, 16, 0, 0, time.Offset);
		var londonCloseStart = new DateTimeOffset(date.Year, date.Month, date.Day, 10, 30, 0, time.Offset);
		var londonCloseEnd = new DateTimeOffset(date.Year, date.Month, date.Day, 13, 0, 0, time.Offset);

		if (time >= sessionLondon && time < sessionNyOpen)
		{
			if (!_inLondon)
			{
				_londonHigh = candle.HighPrice;
				_londonLow = candle.LowPrice;
				_inLondon = true;
			}
			else
			{
				_londonHigh = Math.Max(_londonHigh, candle.HighPrice);
				_londonLow = Math.Min(_londonLow, candle.LowPrice);
			}
		}
		else
		{
			_inLondon = false;
		}

		if (time >= sessionNyOpen && time < sessionEnd)
		{
			if (!_inNy)
			{
				_nyHigh = candle.HighPrice;
				_nyLow = candle.LowPrice;
				_inNy = true;
			}
			else
			{
				_nyHigh = Math.Max(_nyHigh, candle.HighPrice);
				_nyLow = Math.Min(_nyLow, candle.LowPrice);
			}
		}
		else
		{
			_inNy = false;
		}

		if (time >= sessionAsia && time < sessionLondon)
		{
			if (!_inAsia)
			{
				_asiaHigh = candle.HighPrice;
				_asiaLow = candle.LowPrice;
				_inAsia = true;
			}
			else
			{
				_asiaHigh = Math.Max(_asiaHigh, candle.HighPrice);
				_asiaLow = Math.Min(_asiaLow, candle.LowPrice);
			}
		}
		else
		{
			_inAsia = false;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var judasSwing = candle.HighPrice >= _londonHigh && time >= sessionNyOpen && time < sessionEnd;
		var shortEntry = judasSwing && candle.ClosePrice < candle.OpenPrice;

		if (shortEntry && Position >= 0)
		{
			var stopLoss = candle.HighPrice + ShortStopTicks * Security.PriceStep;
			var profitTarget = candle.LowPrice - ShortTakeTicks * Security.PriceStep;

			SellMarket();
			BuyLimit(profitTarget);
			BuyStop(stopLoss);
		}

		var londonCloseBuy = time >= londonCloseStart && time <= londonCloseEnd && candle.ClosePrice < _londonLow;

		if (londonCloseBuy && Position <= 0)
		{
			var stopBuy = candle.LowPrice - BuyStopTicks * Security.PriceStep;
			var takeBuy = candle.ClosePrice + BuyTakeTicks * Security.PriceStep;

			BuyMarket();
			SellLimit(takeBuy);
			SellStop(stopBuy);
		}

		var asiaSell = time >= sessionAsia && time < sessionLondon && candle.ClosePrice > _asiaHigh;

		if (asiaSell && Position >= 0)
		{
			var stopAsia = candle.HighPrice + AsiaStopTicks * Security.PriceStep;
			var takeAsia = candle.ClosePrice - AsiaTakeTicks * Security.PriceStep;

			SellMarket();
			BuyLimit(takeAsia);
			BuyStop(stopAsia);
		}
	}
}

