using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ICT Bread and Butter Sell-Setup strategy.
/// Tracks the London (02:00-08:20 UTC), New York (08:20-16:00 UTC) and Asia (19:00-02:00 UTC) session ranges.
/// NY short: during the NY session a bearish candle whose high exceeds the London session high.
/// London close buy: between 10:30 and 13:00 a close below the London session low.
/// Asia short: during the Asia session a close above the Asia session high so far.
/// Every setup has its own stop loss and take profit in ticks; an opposite setup reverses the position.
/// </summary>
public class IctBreadAndButterSellSetupStrategy : Strategy
{
	private static readonly TimeSpan _londonStart = new(2, 0, 0);
	private static readonly TimeSpan _nyStart = new(8, 20, 0);
	private static readonly TimeSpan _nyEnd = new(16, 0, 0);
	private static readonly TimeSpan _asiaStart = new(19, 0, 0);
	private static readonly TimeSpan _londonCloseStart = new(10, 30, 0);
	private static readonly TimeSpan _londonCloseEnd = new(13, 0, 0);

	private readonly StrategyParam<int> _shortStopTicks;
	private readonly StrategyParam<int> _shortTakeTicks;
	private readonly StrategyParam<int> _buyStopTicks;
	private readonly StrategyParam<int> _buyTakeTicks;
	private readonly StrategyParam<int> _asiaStopTicks;
	private readonly StrategyParam<int> _asiaTakeTicks;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _londonHigh;
	private decimal? _londonLow;
	private decimal? _asiaHigh;
	private bool _inLondon;
	private bool _inAsia;

	private decimal? _stopPrice;
	private decimal? _takePrice;

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
	/// Candle type.
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
			.SetNotNegative()
			.SetDisplay("Short Stop Ticks", "Stop loss ticks for NY short entry", "Risk Management");

		_shortTakeTicks = Param(nameof(ShortTakeTicks), 20)
			.SetNotNegative()
			.SetDisplay("Short Take Profit Ticks", "Take profit ticks for NY short entry", "Risk Management");

		_buyStopTicks = Param(nameof(BuyStopTicks), 10)
			.SetNotNegative()
			.SetDisplay("Buy Stop Ticks", "Stop loss ticks for London close buy", "Risk Management");

		_buyTakeTicks = Param(nameof(BuyTakeTicks), 20)
			.SetNotNegative()
			.SetDisplay("Buy Take Profit Ticks", "Take profit ticks for London close buy", "Risk Management");

		_asiaStopTicks = Param(nameof(AsiaStopTicks), 10)
			.SetNotNegative()
			.SetDisplay("Asia Stop Ticks", "Stop loss ticks for Asia sell entry", "Risk Management");

		_asiaTakeTicks = Param(nameof(AsiaTakeTicks), 15)
			.SetNotNegative()
			.SetDisplay("Asia Take Profit Ticks", "Take profit ticks for Asia sell entry", "Risk Management");

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
		ResetState();
	}

	private void ResetState()
	{
		_londonHigh = null;
		_londonLow = null;
		_asiaHigh = null;
		_inLondon = false;
		_inAsia = false;
		_stopPrice = null;
		_takePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var tod = candle.OpenTime.TimeOfDay;
		var high = candle.HighPrice;
		var low = candle.LowPrice;
		var close = candle.ClosePrice;

		var inLondon = tod >= _londonStart && tod < _nyStart;
		var inNy = tod >= _nyStart && tod < _nyEnd;
		var inAsia = tod >= _asiaStart || tod < _londonStart;

		if (inLondon)
		{
			if (!_inLondon)
			{
				_londonHigh = high;
				_londonLow = low;
			}
			else
			{
				_londonHigh = Math.Max(_londonHigh ?? high, high);
				_londonLow = Math.Min(_londonLow ?? low, low);
			}
		}
		_inLondon = inLondon;

		// The Asia breakout is measured against the range before this candle.
		var prevAsiaHigh = _inAsia && inAsia ? _asiaHigh : null;
		if (inAsia)
			_asiaHigh = prevAsiaHigh is decimal h ? Math.Max(h, high) : high;
		_inAsia = inAsia;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (CheckExits(high, low))
			return;

		var step = Security?.PriceStep ?? 1m;
		if (step <= 0)
			step = 1m;

		var nyShort = inNy && _londonHigh is decimal lh && high > lh && close < candle.OpenPrice;
		var londonCloseBuy = tod >= _londonCloseStart && tod <= _londonCloseEnd && _londonLow is decimal ll && close < ll;
		var asiaShort = inAsia && prevAsiaHigh is decimal ah && close > ah;

		if (nyShort && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			SetLevels(close, false, ShortStopTicks * step, ShortTakeTicks * step);
		}
		else if (londonCloseBuy && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			SetLevels(close, true, BuyStopTicks * step, BuyTakeTicks * step);
		}
		else if (asiaShort && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			SetLevels(close, false, AsiaStopTicks * step, AsiaTakeTicks * step);
		}
	}

	private void SetLevels(decimal entry, bool isLong, decimal stop, decimal take)
	{
		_stopPrice = stop > 0 ? (isLong ? entry - stop : entry + stop) : null;
		_takePrice = take > 0 ? (isLong ? entry + take : entry - take) : null;
	}

	private bool CheckExits(decimal high, decimal low)
	{
		if (Position > 0)
		{
			if ((_stopPrice is decimal sl && low <= sl) || (_takePrice is decimal tp && high >= tp))
			{
				SellMarket(Position);
				_stopPrice = null;
				_takePrice = null;
				return true;
			}
		}
		else if (Position < 0)
		{
			if ((_stopPrice is decimal sl && high >= sl) || (_takePrice is decimal tp && low <= tp))
			{
				BuyMarket(-Position);
				_stopPrice = null;
				_takePrice = null;
				return true;
			}
		}

		return false;
	}
}
