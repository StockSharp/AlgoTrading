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

	// Most recently touched grid line; -1 when the candle did not touch any line.
	private int _prevLevel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GridBotStrategy"/>.
	/// </summary>
	public GridBotStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_upperLimit = Param(nameof(UpperLimit), 74000m)
			.SetGreaterThanZero()
			.SetDisplay("Upper Limit", "Top price of the grid range", "Grid Settings");

		_lowerLimit = Param(nameof(LowerLimit), 60000m)
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

		var level = GetTouchedLevel(candle);

		// A gap entirely outside the grid is not a touch of its outermost line.
		if (level < 0)
		{
			_prevLevel = -1;
			return;
		}

		// Repeated touches of the same line do not create duplicate signals.
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

	// Use only lines the candle actually traded through. If several were touched, use the
	// one nearest the close, breaking equal-distance ties toward the lower line.
	private int GetTouchedLevel(ICandleMessage candle)
	{
		var step = (UpperLimit - LowerLimit) / GridCount;
		var touched = -1;
		var distance = decimal.MaxValue;
		for (var index = 0; index <= GridCount; index++)
		{
			var price = LowerLimit + index * step;
			if (price < candle.LowPrice || price > candle.HighPrice)
				continue;
			var candidateDistance = Math.Abs(price - candle.ClosePrice);
			if (candidateDistance >= distance)
				continue;
			distance = candidateDistance;
			touched = index;
		}
		return touched;
	}
}
