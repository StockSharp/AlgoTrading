namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Trades the selected candle-body engulfing pattern and exits after HoldPeriods bars.
/// </summary>
public class EngulfingCandlestickStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _holdPeriods;
	private readonly StrategyParam<string> _pattern;
	private readonly StrategyParam<Sides> _side;
	private decimal? _previousOpen;
	private decimal _previousClose;
	private int _barsHeld;
	private Order _pendingOrder;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int HoldPeriods { get => _holdPeriods.Value; set => _holdPeriods.Value = value; }
	public string Pattern { get => _pattern.Value; set => _pattern.Value = value; }
	public Sides Side { get => _side.Value; set => _side.Value = value; }

	public EngulfingCandlestickStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
		_holdPeriods = Param(nameof(HoldPeriods), 17)
			.SetGreaterThanZero()
			.SetDisplay("Hold Periods", "Bars to hold the position", "Trading");
		_pattern = Param(nameof(Pattern), "Bullish")
			.SetDisplay("Pattern", "Bullish or Bearish engulfing", "Trading");
		_side = Param(nameof(Side), Sides.Buy)
			.SetDisplay("Side", "Buy for long, Sell for short", "Trading");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_previousOpen = null;
		_previousClose = 0m;
		_barsHeld = 0;
		_pendingOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		if (Pattern != "Bullish" && Pattern != "Bearish")
			throw new ArgumentOutOfRangeException(nameof(Pattern), Pattern, "Choose Bullish or Bearish.");
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
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

		var bullish = _previousOpen is decimal open && _previousClose < open
			&& candle.ClosePrice > candle.OpenPrice && candle.OpenPrice <= _previousClose && candle.ClosePrice >= open;
		var bearish = _previousOpen is decimal previousOpen && _previousClose > previousOpen
			&& candle.ClosePrice < candle.OpenPrice && candle.OpenPrice >= _previousClose && candle.ClosePrice <= previousOpen;

		if (Position != 0)
			_barsHeld++;

		if (_pendingOrder?.State is OrderStates.Done or OrderStates.Failed)
			_pendingOrder = null;

		if (_pendingOrder is null && IsFormedAndOnlineAndAllowTrading())
		{
			if (Position != 0 && _barsHeld >= HoldPeriods)
				_pendingOrder = Position > 0 ? SellMarket(Position) : BuyMarket(-Position);
			else if (Position == 0 && (Pattern == "Bullish" ? bullish : bearish))
			{
				_pendingOrder = Side == Sides.Buy ? BuyMarket() : SellMarket();
				_barsHeld = 0;
			}
		}

		_previousOpen = candle.OpenPrice;
		_previousClose = candle.ClosePrice;
	}
}
