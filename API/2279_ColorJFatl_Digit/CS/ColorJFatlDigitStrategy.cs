using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the slope changes of Jurik Moving Average (JMA).
/// Opens long when JMA turns up and closes short positions.
/// Opens short when JMA turns down and closes long positions.
/// </summary>
public class ColorJFatlDigitStrategy : Strategy
{
	private readonly StrategyParam<int> _jmaLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<bool> _closeLong;
	private readonly StrategyParam<bool> _closeShort;

	private decimal? _prevJma;
	private decimal? _prevSlope;

	/// <summary>
	/// JMA period length.
	/// </summary>
	public int JmaLength
	{
		get => _jmaLength.Value;
		set => _jmaLength.Value = value;
	}

	/// <summary>
	/// Candle type for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool CloseLong
	{
		get => _closeLong.Value;
		set => _closeLong.Value = value;
	}

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool CloseShort
	{
		get => _closeShort.Value;
		set => _closeShort.Value = value;
	}

	public ColorJFatlDigitStrategy()
	{
		_jmaLength = Param(nameof(JmaLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("JMA Length", "Period for Jurik Moving Average", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe of indicator", "Parameters");

		_enableLong = Param(nameof(EnableLong), true)
		.SetDisplay("Enable Long", "Allow opening long positions", "Trading");

		_enableShort = Param(nameof(EnableShort), true)
		.SetDisplay("Enable Short", "Allow opening short positions", "Trading");

		_closeLong = Param(nameof(CloseLong), true)
		.SetDisplay("Close Long", "Allow closing long positions", "Trading");

		_closeShort = Param(nameof(CloseShort), true)
		.SetDisplay("Close Short", "Allow closing short positions", "Trading");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var jma = new JurikMovingAverage { Length = JmaLength };

		SubscribeCandles(CandleType)
		.Bind(jma, Process)
		.Start();
	}

	private void Process(ICandleMessage candle, decimal jmaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var slope = _prevJma is decimal prev ? jmaValue - prev : (decimal?)null;

		if (slope is decimal s && _prevSlope is decimal ps)
		{
			var turnedUp = ps <= 0m && s > 0m;
			var turnedDown = ps >= 0m && s < 0m;

			if (turnedUp)
			{
				if (CloseShort && Position < 0)
				BuyMarket();
				if (EnableLong && Position <= 0)
				BuyMarket();
			}
			else if (turnedDown)
			{
				if (CloseLong && Position > 0)
				SellMarket();
				if (EnableShort && Position >= 0)
				SellMarket();
			}
		}

		_prevSlope = slope;
		_prevJma = jmaValue;
	}
}
