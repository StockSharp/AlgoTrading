namespace StockSharp.Samples.Strategies;

using System;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Directed Movement Candle strategy.
/// Uses RSI levels to detect momentum shifts and trade accordingly.
/// </summary>
public class DirectedMovementCandleStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _highLevel;
	private readonly StrategyParam<decimal> _middleLevel;
	private readonly StrategyParam<decimal> _lowLevel;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private decimal? _prevColor;

	/// <summary>
	/// Initializes a new instance of <see cref="DirectedMovementCandleStrategy"/>.
	/// </summary>
	public DirectedMovementCandleStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "RSI period", "Indicator")
			.SetCanOptimize();
		_highLevel = Param(nameof(HighLevel), 70m)
			.SetDisplay("High Level", "Upper threshold", "Indicator")
			.SetCanOptimize();
		_middleLevel = Param(nameof(MiddleLevel), 50m)
			.SetDisplay("Middle Level", "Middle threshold", "Indicator")
			.SetCanOptimize();
		_lowLevel = Param(nameof(LowLevel), 30m)
			.SetDisplay("Low Level", "Lower threshold", "Indicator")
			.SetCanOptimize();
		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
			.SetDisplay("Candle Type", "Candle type", "Data");
	}

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	/// <summary>
	/// Upper RSI threshold.
	/// </summary>
	public decimal HighLevel { get => _highLevel.Value; set => _highLevel.Value = value; }

	/// <summary>
	/// Middle RSI threshold.
	/// </summary>
	public decimal MiddleLevel { get => _middleLevel.Value; set => _middleLevel.Value = value; }

	/// <summary>
	/// Lower RSI threshold.
	/// </summary>
	public decimal LowLevel { get => _lowLevel.Value; set => _lowLevel.Value = value; }

	/// <summary>
	/// Candle type for subscriptions.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var color = 1m;

		if (rsiValue >= HighLevel)
			color = 2m;
		else if (rsiValue <= LowLevel)
			color = 0m;

		if (_prevColor == null)
		{
			_prevColor = color;
			return;
		}

		if (color == 2m && _prevColor < 2m)
		{
			if (Position < 0)
				BuyMarket(-Position);
			BuyMarket();
		}
		else if (color == 0m && _prevColor > 0m)
		{
			if (Position > 0)
				SellMarket(Position);
			SellMarket();
		}

		_prevColor = color;
	}
}
