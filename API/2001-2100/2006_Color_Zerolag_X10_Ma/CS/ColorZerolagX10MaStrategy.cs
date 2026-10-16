using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades ZLEMA slope reversals with independent long/short entry/exit switches.
/// </summary>
public class ColorZerolagX10MaStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _openBuy;
	private readonly StrategyParam<bool> _openSell;
	private readonly StrategyParam<bool> _closeBuy;
	private readonly StrategyParam<bool> _closeSell;

	private decimal? _previous;
	private decimal? _beforePrevious;
	private Order _pendingOrder;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public bool BuyPosOpen { get => _openBuy.Value; set => _openBuy.Value = value; }
	public bool SellPosOpen { get => _openSell.Value; set => _openSell.Value = value; }
	public bool BuyPosClose { get => _closeBuy.Value; set => _closeBuy.Value = value; }
	public bool SellPosClose { get => _closeSell.Value; set => _closeSell.Value = value; }

	public ColorZerolagX10MaStrategy()
	{
		_length = Param(nameof(Length), 20).SetGreaterThanZero()
			.SetDisplay("Length", "ZLEMA length", "Indicator");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_openBuy = Param(nameof(BuyPosOpen), true).SetDisplay("Open Buy", "Allow long entries", "Trading");
		_openSell = Param(nameof(SellPosOpen), true).SetDisplay("Open Sell", "Allow short entries", "Trading");
		_closeBuy = Param(nameof(BuyPosClose), true).SetDisplay("Close Buy", "Allow long exits", "Trading");
		_closeSell = Param(nameof(SellPosClose), true).SetDisplay("Close Sell", "Allow short exits", "Trading");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_previous = _beforePrevious = null;
		_pendingOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_previous = _beforePrevious = null;
		_pendingOrder = null;
		var zlma = new ZeroLagExponentialMovingAverage { Length = Length };
		SubscribeCandles(CandleType).Bind(zlma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal current)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var turnUp = _beforePrevious is decimal older && _previous is decimal previous
			&& previous < older && current > previous;
		var turnDown = _beforePrevious is decimal olderDown && _previous is decimal previousDown
			&& previousDown > olderDown && current < previousDown;
		_beforePrevious = _previous;
		_previous = current;

		if (!IsFormedAndOnlineAndAllowTrading()
			|| (_pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed)))
			return;

		if (turnUp)
		{
			if (Position < 0m && SellPosClose)
				_pendingOrder = BuyMarket(-Position + (BuyPosOpen ? Volume : 0m));
			else if (Position == 0m && BuyPosOpen)
				_pendingOrder = BuyMarket(Volume);
		}
		else if (turnDown)
		{
			if (Position > 0m && BuyPosClose)
				_pendingOrder = SellMarket(Position + (SellPosOpen ? Volume : 0m));
			else if (Position == 0m && SellPosOpen)
				_pendingOrder = SellMarket(Volume);
		}
	}
}
