using System;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Break-even manager that moves stop to entry price after a specified profit.
/// Allows filtering by order comment and magic number.
/// </summary>
public class BreakEvenMasterStrategy : Strategy
{
	private readonly StrategyParam<int> _breakEvenTicks;
	private readonly StrategyParam<bool> _useComment;
	private readonly StrategyParam<string> _comment;
	private readonly StrategyParam<bool> _useMagicNumber;
	private readonly StrategyParam<int> _magicNumber;
	private readonly StrategyParam<DataType> _candleType;

	private bool _isBreakEven;
	private decimal _entryPrice;
	private decimal _stopLoss;
	private decimal _tick;

	public int BreakEvenTicks { get => _breakEvenTicks.Value; set => _breakEvenTicks.Value = value; }
	public bool UseComment { get => _useComment.Value; set => _useComment.Value = value; }
	public string Comment { get => _comment.Value; set => _comment.Value = value; }
	public bool UseMagicNumber { get => _useMagicNumber.Value; set => _useMagicNumber.Value = value; }
	public int MagicNumber { get => _magicNumber.Value; set => _magicNumber.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BreakEvenMasterStrategy()
	{
		_breakEvenTicks = Param(nameof(BreakEvenTicks), 20)
			.SetDisplay("Break Even Ticks", "Profit in ticks to move stop to break even", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 5);

		_useComment = Param(nameof(UseComment), false)
			.SetDisplay("Use Comment", "Apply filter by order comment", "Filters");

		_comment = Param(nameof(Comment), string.Empty)
			.SetDisplay("Comment", "Order comment to match", "Filters");

		_useMagicNumber = Param(nameof(UseMagicNumber), false)
			.SetDisplay("Use Magic Number", "Apply filter by magic number", "Filters");

		_magicNumber = Param(nameof(MagicNumber), 12345)
			.SetDisplay("Magic Number", "Magic number to match", "Filters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for price tracking", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tick = Security?.PriceStep ?? 1m;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade myTrade)
	{
		base.OnNewMyTrade(myTrade);

		if (UseComment && myTrade.Order.Comment != Comment)
			return;

		if (UseMagicNumber && myTrade.Order.UserOrderId != MagicNumber.ToString())
			return;

		_entryPrice = myTrade.Trade.Price;
		_isBreakEven = false;
		_stopLoss = 0m;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var beDistance = BreakEvenTicks * _tick;

		if (Position > 0)
		{
			if (!_isBreakEven && candle.ClosePrice - _entryPrice >= beDistance)
			{
				_stopLoss = _entryPrice;
				_isBreakEven = true;
			}

			if (_isBreakEven && candle.LowPrice <= _stopLoss)
				SellMarket();
		}
		else if (Position < 0)
		{
			if (!_isBreakEven && _entryPrice - candle.ClosePrice >= beDistance)
			{
				_stopLoss = _entryPrice;
				_isBreakEven = true;
			}

			if (_isBreakEven && candle.HighPrice >= _stopLoss)
				BuyMarket();
		}
	}
}
