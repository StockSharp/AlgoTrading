using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Color Bears Gap zero crossing using smoothed high-close and high-open gaps.
/// Long when the difference crosses downward through zero; short on upward crossover.
/// </summary>
public class ColorBearsGapStrategy : Strategy
{
	private readonly StrategyParam<int> _length1;
	private readonly StrategyParam<int> _length2;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _sellClose;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _emaClose;
	private ExponentialMovingAverage _emaOpen;
	private ExponentialMovingAverage _gapClose;
	private ExponentialMovingAverage _gapOpen;
	private decimal? _previousGap;
	private Order _pendingOrder;

	public int Length1 { get => _length1.Value; set => _length1.Value = value; }
	public int Length2 { get => _length2.Value; set => _length2.Value = value; }
	public bool BuyOpen { get => _buyOpen.Value; set => _buyOpen.Value = value; }
	public bool BuyClose { get => _buyClose.Value; set => _buyClose.Value = value; }
	public bool SellOpen { get => _sellOpen.Value; set => _sellOpen.Value = value; }
	public bool SellClose { get => _sellClose.Value; set => _sellClose.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorBearsGapStrategy()
	{
		_length1 = Param(nameof(Length1), 12).SetGreaterThanZero()
			.SetDisplay("Length 1", "Open/close first smoothing", "Indicator");
		_length2 = Param(nameof(Length2), 5).SetGreaterThanZero()
			.SetDisplay("Length 2", "Gap second smoothing", "Indicator");
		_buyOpen = Param(nameof(BuyOpen), true).SetDisplay("Buy Open", "Allow long entries", "Permissions");
		_buyClose = Param(nameof(BuyClose), true).SetDisplay("Buy Close", "Allow closing longs", "Permissions");
		_sellOpen = Param(nameof(SellOpen), true).SetDisplay("Sell Open", "Allow short entries", "Permissions");
		_sellClose = Param(nameof(SellClose), true).SetDisplay("Sell Close", "Allow closing shorts", "Permissions");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candle subscription", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_previousGap = null;
		_pendingOrder = null;
		_emaClose = _emaOpen = _gapClose = _gapOpen = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_previousGap = null;
		_pendingOrder = null;
		_emaClose = new ExponentialMovingAverage { Length = Length1 };
		_emaOpen = new ExponentialMovingAverage { Length = Length1 };
		_gapClose = new ExponentialMovingAverage { Length = Length2 };
		_gapOpen = new ExponentialMovingAverage { Length = Length2 };
		Indicators.Add(_emaClose);
		Indicators.Add(_emaOpen);
		Indicators.Add(_gapClose);
		Indicators.Add(_gapOpen);
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
		var area = CreateChartArea();
		if (area is not null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
		var time = candle.OpenTime;
		var close = _emaClose.Process(candle.ClosePrice, time, true).GetValue<decimal>();
		var open = _emaOpen.Process(candle.OpenPrice, time, true).GetValue<decimal>();
		var gapC = _gapClose.Process(candle.HighPrice - close, time, true).GetValue<decimal>();
		var gapO = _gapOpen.Process(candle.HighPrice - open, time, true).GetValue<decimal>();
		if (!_emaClose.IsFormed || !_emaOpen.IsFormed || !_gapClose.IsFormed || !_gapOpen.IsFormed)
			return;

		// Both smoothed gap values must belong to the same finished candle.
		var diff = gapO - gapC;
		var previous = _previousGap;
		_previousGap = diff;
		if (previous is null || !IsFormedAndOnlineAndAllowTrading() ||
			(_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed)))
			return;

		if (previous > 0m && diff < 0m)
		{
			if (Position < 0m)
			{
				if (SellClose)
					_pendingOrder = BuyMarket(Math.Abs(Position) + (BuyOpen ? Volume : 0m));
			}
			else if (Position == 0m && BuyOpen)
				_pendingOrder = BuyMarket(Volume);
		}
		else if (previous < 0m && diff > 0m)
		{
			if (Position > 0m)
			{
				if (BuyClose)
					_pendingOrder = SellMarket(Math.Abs(Position) + (SellOpen ? Volume : 0m));
			}
			else if (Position == 0m && SellOpen)
				_pendingOrder = SellMarket(Volume);
		}
	}
}
