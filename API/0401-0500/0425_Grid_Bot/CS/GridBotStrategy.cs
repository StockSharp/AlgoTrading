namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Grid Bot Strategy.
/// Splits the predefined price range between <see cref="LowerLimit"/> and <see cref="UpperLimit"/>
/// into <see cref="GridCount"/> equal levels and trades the oscillations between them.
/// Touching a level in the lower half opens a long, touching a level in the upper half opens a short,
/// and every signal closes the opposite position first.
/// </summary>
public class GridBotStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<decimal> _upperLimit;
	private readonly StrategyParam<decimal> _lowerLimit;
	private readonly StrategyParam<int> _gridCount;

	// Grid line the previous candle closed on, -1 before the first one is evaluated.
	private int _prevLevel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GridBotStrategy"/>.
	/// </summary>
	public GridBotStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_upperLimit = Param(nameof(UpperLimit), 48000m)
			.SetGreaterThanZero()
			.SetDisplay("Upper Limit", "Top price of the grid range", "Grid Settings");

		_lowerLimit = Param(nameof(LowerLimit), 45000m)
			.SetGreaterThanZero()
			.SetDisplay("Lower Limit", "Bottom price of the grid range", "Grid Settings");

		_gridCount = Param(nameof(GridCount), 10)
			.SetGreaterThanZero()
			.SetDisplay("Grid Count", "Number of equal levels the range is split into", "Grid Settings");
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	/// <summary>
	/// Top price of the grid range.
	/// </summary>
	public decimal UpperLimit
	{
		get => _upperLimit.Value;
		set => _upperLimit.Value = value;
	}

	/// <summary>
	/// Bottom price of the grid range.
	/// </summary>
	public decimal LowerLimit
	{
		get => _lowerLimit.Value;
		set => _lowerLimit.Value = value;
	}

	/// <summary>
	/// Number of equal levels the range is split into.
	/// </summary>
	public int GridCount
	{
		get => _gridCount.Value;
		set => _gridCount.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevLevel = -1;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (UpperLimit <= LowerLimit)
			throw new InvalidOperationException($"{nameof(UpperLimit)} must be above {nameof(LowerLimit)}.");

		_prevLevel = -1;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var level = GetLevel(candle.ClosePrice);

		// A touch is the move onto another line; standing on the same one is not a new signal.
		if (level == _prevLevel)
			return;

		_prevLevel = level;

		// The middle line splits the range into halves and carries no bias of its own.
		var middle = GridCount / 2m;

		if (level < middle && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (level > middle && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}

	// Index of the grid line the price sits on, counted from LowerLimit up to GridCount.
	private int GetLevel(decimal price)
	{
		var step = (UpperLimit - LowerLimit) / GridCount;

		// A price outside the predefined range belongs to the outermost line of the grid.
		var clamped = Math.Clamp(price, LowerLimit, UpperLimit);

		return (int)Math.Floor((clamped - LowerLimit) / step + 0.5m);
	}
}
