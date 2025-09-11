using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that rotates a text string on each finished candle.
/// Demonstrates non-trading logic converted from TradingView Motion script.
/// </summary>
public class MotionStrategy : Strategy
{
	private readonly StrategyParam<string> _text;
	private readonly StrategyParam<int> _step;
	private readonly StrategyParam<DataType> _candleType;

	private int _index;

	/// <summary>
	/// Text to rotate.
	/// </summary>
	public string Text
	{
		get => _text.Value;
		set => _text.Value = value;
	}

	/// <summary>
	/// Characters shifted per candle.
	/// </summary>
	public int Step
	{
		get => _step.Value;
		set => _step.Value = value;
	}

	/// <summary>
	/// Candle type used for timing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public MotionStrategy()
	{
		_text = Param(nameof(Text), "ABCDEFGHIJKLMNOPQRSTUVWXYZ 0123456789")
			.SetDisplay("Text", "Source text", "General");

		_step = Param(nameof(Step), 1)
			.SetGreaterThanZero()
			.SetDisplay("Step", "Characters per candle", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		StartProtection();

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var text = Text;
		if (string.IsNullOrEmpty(text))
			return;

		var step = Step;
		_index = (_index + step) % text.Length;
		var rotated = text.Substring(_index) + text.Substring(0, _index);

		LogInfo($"Motion text: {rotated}");
	}
}
