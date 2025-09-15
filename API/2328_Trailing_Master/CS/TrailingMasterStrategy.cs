using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple trailing stop strategy.
/// </summary>
public class TrailingMasterStrategy : Strategy
{
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<bool> _useComment;
	private readonly StrategyParam<string> _comment;
	private readonly StrategyParam<bool> _useMagicNumber;
	private readonly StrategyParam<long> _magicNumber;

	private Order _stopOrder;
	private decimal _entryPrice;

	/// <summary>
	/// Trailing stop size in ticks.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Use custom comment for stop orders.
	/// </summary>
	public bool UseComment
	{
		get => _useComment.Value;
		set => _useComment.Value = value;
	}

	/// <summary>
	/// Comment text for stop orders.
	/// </summary>
	public string CommentText
	{
		get => _comment.Value;
		set => _comment.Value = value;
	}

	/// <summary>
	/// Use custom identifier for stop orders.
	/// </summary>
	public bool UseMagicNumber
	{
		get => _useMagicNumber.Value;
		set => _useMagicNumber.Value = value;
	}

	/// <summary>
	/// Custom identifier value.
	/// </summary>
	public long MagicNumber
	{
		get => _magicNumber.Value;
		set => _magicNumber.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="TrailingMasterStrategy"/>.
	/// </summary>
	public TrailingMasterStrategy()
	{
		_trailingStop = Param(nameof(TrailingStop), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop", "Stop size in ticks", "Risk");

		_useComment = Param(nameof(UseComment), false)
			.SetDisplay("Use Comment", "Attach comment to stop", "General");

		_comment = Param(nameof(CommentText), string.Empty)
			.SetDisplay("Comment", "Custom comment text", "General");

		_useMagicNumber = Param(nameof(UseMagicNumber), false)
			.SetDisplay("Use Magic Number", "Attach identifier to stop", "General");

		_magicNumber = Param(nameof(MagicNumber), 12345L)
			.SetDisplay("Magic Number", "Custom identifier", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Ticks)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		SubscribeTrades().Bind(ProcessTrade).Start();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		{
			_entryPrice = 0m;

			if (_stopOrder != null && _stopOrder.State == OrderStates.Active)
				CancelOrder(_stopOrder);

			_stopOrder = null;
		}
	}

	private void ProcessTrade(ExecutionMessage trade)
	{
		var price = trade.TradePrice ?? 0m;
		var step = Security.PriceStep ?? 1m;

		if (_entryPrice == 0m && Position != 0)
			_entryPrice = price;

		if (TrailingStop <= 0 || Position == 0)
			return;

		var offset = TrailingStop * step;

		if (Position > 0)
		{
			var profit = price - _entryPrice;
			if (profit < offset)
				return;

			var stopPrice = price - offset;

			if (_stopOrder == null)
			{
				_stopOrder = SellStop(Position, stopPrice);
				ApplyMeta(_stopOrder);
			}
			else if (_stopOrder.Price < stopPrice)
			{
				CancelOrder(_stopOrder);
				_stopOrder = SellStop(Position, stopPrice);
				ApplyMeta(_stopOrder);
			}
		}
		else if (Position < 0)
		{
			var profit = _entryPrice - price;
			if (profit < offset)
				return;

			var stopPrice = price + offset;
			var volume = Math.Abs(Position);

			if (_stopOrder == null)
			{
				_stopOrder = BuyStop(volume, stopPrice);
				ApplyMeta(_stopOrder);
			}
			else if (_stopOrder.Price > stopPrice)
			{
				CancelOrder(_stopOrder);
				_stopOrder = BuyStop(volume, stopPrice);
				ApplyMeta(_stopOrder);
			}
		}
	}

	private void ApplyMeta(Order order)
	{
		if (order == null)
			return;

		if (UseComment)
			order.Comment = CommentText;

		if (UseMagicNumber)
			order.UserOrderId = MagicNumber.ToString();
	}
}