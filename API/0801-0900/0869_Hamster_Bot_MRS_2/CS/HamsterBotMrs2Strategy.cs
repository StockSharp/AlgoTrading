using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hamster Bot MRS 2 strategy.
/// The level is the simple moving average shifted Shift candles back. A close crossing above the level buys and a close crossing
/// below it sells; the reverse crossing of the same level closes the position and opens the opposite one.
/// </summary>
public class HamsterBotMrs2Strategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _shift;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<decimal> _maHistory = new();
	private decimal? _prevClose;
	private decimal? _prevLevel;

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Candles the moving average level is shifted back.
	/// </summary>
	public int Shift
	{
		get => _shift.Value;
		set => _shift.Value = value;
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
	/// Constructor.
	/// </summary>
	public HamsterBotMrs2Strategy()
	{
		_maLength = Param(nameof(MaLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average length", "Indicators");

		_shift = Param(nameof(Shift), 1)
			.SetNotNegative()
			.SetDisplay("Shift", "Candles the moving average level is shifted back", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	private void ResetState()
	{
		_maHistory.Clear();
		_prevClose = null;
		_prevLevel = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var sma = new SimpleMovingAverage { Length = MaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_maHistory.Enqueue(maValue);
		while (_maHistory.Count > Shift + 1)
			_maHistory.Dequeue();

		if (_maHistory.Count < Shift + 1)
			return;

		// The oldest kept value is the average Shift candles ago.
		var level = _maHistory.Peek();
		var close = candle.ClosePrice;

		var prevClose = _prevClose;
		var prevLevel = _prevLevel;

		_prevClose = close;
		_prevLevel = level;

		if (prevClose is not decimal lastClose || prevLevel is not decimal lastLevel)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (lastClose <= lastLevel && close > level && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (lastClose >= lastLevel && close < level && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
