using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Demonstrates highest and lowest functions with alternating lengths.
/// </summary>
public class FunctionHighestLowestStrategy : Strategy
{
	private readonly StrategyParam<int> _lengthA;
	private readonly StrategyParam<int> _lengthB;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _highestA = null!;
	private Highest _highestB = null!;
	private Lowest _lowestA = null!;
	private Lowest _lowestB = null!;

	/// <summary>
	/// First lookback length.
	/// </summary>
	public int LengthA
	{
		get => _lengthA.Value;
		set => _lengthA.Value = value;
	}

	/// <summary>
	/// Second lookback length.
	/// </summary>
	public int LengthB
	{
		get => _lengthB.Value;
		set => _lengthB.Value = value;
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
	/// Initializes a new instance of the <see cref="FunctionHighestLowestStrategy"/> class.
	/// </summary>
	public FunctionHighestLowestStrategy()
	{
		_lengthA = Param(nameof(LengthA), 10)
			.SetGreaterThanZero()
			.SetDisplay("Length A", "First lookback length", "General");

		_lengthB = Param(nameof(LengthB), 20)
			.SetGreaterThanZero()
			.SetDisplay("Length B", "Second lookback length", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

		_highestA = new Highest { Length = LengthA };
		_highestB = new Highest { Length = LengthB };
		_lowestA = new Lowest { Length = LengthA };
		_lowestB = new Lowest { Length = LengthB };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _highestA);
			DrawIndicator(area, _highestB);
			DrawIndicator(area, _lowestA);
			DrawIndicator(area, _lowestB);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var highA = _highestA.Process(candle).ToDecimal();
		var highB = _highestB.Process(candle).ToDecimal();
		var lowA = _lowestA.Process(candle).ToDecimal();
		var lowB = _lowestB.Process(candle).ToDecimal();

		var alternatingHigh = candle.ClosePrice > candle.OpenPrice ? highA : highB;
		var alternatingLow = candle.ClosePrice > candle.OpenPrice ? lowA : lowB;

		this.AddInfoLog($"AltHigh={alternatingHigh}, AltLow={alternatingLow}");
	}
}
