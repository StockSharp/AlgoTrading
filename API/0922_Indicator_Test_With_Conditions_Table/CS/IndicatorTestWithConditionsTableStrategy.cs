using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simple condition-based strategy.
/// Compares close price with user-defined values to open or close positions.
/// </summary>
public class IndicatorTestWithConditionsTableStrategy : Strategy
{
	private readonly StrategyParam<bool> _enableLongCond;
	private readonly StrategyParam<string> _longOperator;
	private readonly StrategyParam<decimal> _longValue;

	private readonly StrategyParam<bool> _enableCloseLongCond;
	private readonly StrategyParam<string> _closeLongOperator;
	private readonly StrategyParam<decimal> _closeLongValue;

	private readonly StrategyParam<bool> _enableShortCond;
	private readonly StrategyParam<string> _shortOperator;
	private readonly StrategyParam<decimal> _shortValue;

	private readonly StrategyParam<bool> _enableCloseShortCond;
	private readonly StrategyParam<string> _closeShortOperator;
	private readonly StrategyParam<decimal> _closeShortValue;

	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// The type of candles used for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Enable long entry condition.
	/// </summary>
	public bool EnableLongCond
	{
		get => _enableLongCond.Value;
		set => _enableLongCond.Value = value;
	}

	/// <summary>
	/// Operator for long entry.
	/// </summary>
	public string LongOperator
	{
		get => _longOperator.Value;
		set => _longOperator.Value = value;
	}

	/// <summary>
	/// Comparison value for long entry.
	/// </summary>
	public decimal LongValue
	{
		get => _longValue.Value;
		set => _longValue.Value = value;
	}

	/// <summary>
	/// Enable close long condition.
	/// </summary>
	public bool EnableCloseLongCond
	{
		get => _enableCloseLongCond.Value;
		set => _enableCloseLongCond.Value = value;
	}

	/// <summary>
	/// Operator for closing long position.
	/// </summary>
	public string CloseLongOperator
	{
		get => _closeLongOperator.Value;
		set => _closeLongOperator.Value = value;
	}

	/// <summary>
	/// Comparison value for closing long position.
	/// </summary>
	public decimal CloseLongValue
	{
		get => _closeLongValue.Value;
		set => _closeLongValue.Value = value;
	}

	/// <summary>
	/// Enable short entry condition.
	/// </summary>
	public bool EnableShortCond
	{
		get => _enableShortCond.Value;
		set => _enableShortCond.Value = value;
	}

	/// <summary>
	/// Operator for short entry.
	/// </summary>
	public string ShortOperator
	{
		get => _shortOperator.Value;
		set => _shortOperator.Value = value;
	}

	/// <summary>
	/// Comparison value for short entry.
	/// </summary>
	public decimal ShortValue
	{
		get => _shortValue.Value;
		set => _shortValue.Value = value;
	}

	/// <summary>
	/// Enable close short condition.
	/// </summary>
	public bool EnableCloseShortCond
	{
		get => _enableCloseShortCond.Value;
		set => _enableCloseShortCond.Value = value;
	}

	/// <summary>
	/// Operator for closing short position.
	/// </summary>
	public string CloseShortOperator
	{
		get => _closeShortOperator.Value;
		set => _closeShortOperator.Value = value;
	}

	/// <summary>
	/// Comparison value for closing short position.
	/// </summary>
	public decimal CloseShortValue
	{
		get => _closeShortValue.Value;
		set => _closeShortValue.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public IndicatorTestWithConditionsTableStrategy()
	{
		_enableLongCond = Param(nameof(EnableLongCond), true)
			.SetDisplay("Enable Long Condition", group: "Long Entry");
		_longOperator = Param(nameof(LongOperator), ">")
			.SetDisplay("Long Operator", group: "Long Entry");
		_longValue = Param(nameof(LongValue), 0m)
			.SetDisplay("Long Value", group: "Long Entry")
			.SetCanOptimize(true);

		_enableCloseLongCond = Param(nameof(EnableCloseLongCond), false)
			.SetDisplay("Enable Close Long", group: "Close Long");
		_closeLongOperator = Param(nameof(CloseLongOperator), "<")
			.SetDisplay("Close Long Operator", group: "Close Long");
		_closeLongValue = Param(nameof(CloseLongValue), 0m)
			.SetDisplay("Close Long Value", group: "Close Long")
			.SetCanOptimize(true);

		_enableShortCond = Param(nameof(EnableShortCond), false)
			.SetDisplay("Enable Short Condition", group: "Short Entry");
		_shortOperator = Param(nameof(ShortOperator), "<")
			.SetDisplay("Short Operator", group: "Short Entry");
		_shortValue = Param(nameof(ShortValue), 0m)
			.SetDisplay("Short Value", group: "Short Entry")
			.SetCanOptimize(true);

		_enableCloseShortCond = Param(nameof(EnableCloseShortCond), false)
			.SetDisplay("Enable Close Short", group: "Close Short");
		_closeShortOperator = Param(nameof(CloseShortOperator), ">")
			.SetDisplay("Close Short Operator", group: "Close Short");
		_closeShortValue = Param(nameof(CloseShortValue), 0m)
			.SetDisplay("Close Short Value", group: "Close Short")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", group: "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		var longEntry = _enableLongCond.Value && CheckCondition(close, _longOperator.Value, _longValue.Value);
		var closeLong = _enableCloseLongCond.Value && CheckCondition(close, _closeLongOperator.Value, _closeLongValue.Value);
		var shortEntry = _enableShortCond.Value && CheckCondition(close, _shortOperator.Value, _shortValue.Value);
		var closeShort = _enableCloseShortCond.Value && CheckCondition(close, _closeShortOperator.Value, _closeShortValue.Value);

		if (closeLong && Position > 0)
			SellMarket(Math.Abs(Position));
		else if (closeShort && Position < 0)
			BuyMarket(Math.Abs(Position));
		else if (longEntry && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortEntry && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}

	private static bool CheckCondition(decimal left, string op, decimal right)
	{
		return op switch
		{
			">" => left > right,
			"<" => left < right,
			">=" => left >= right,
			"<=" => left <= right,
			"=" => left == right,
			"!=" => left != right,
			_ => false
		};
	}
}
