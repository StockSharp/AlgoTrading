namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// One-Two-Three Reversal Strategy.
/// A long opens on a bullish 1-2-3 pattern: the current low is below the previous low, the previous low is below the low
/// three bars ago, the low two bars ago is below the low four bars ago and the high two bars ago is below the high three
/// bars ago. The long closes after DaysToHold bars or when the close crosses above the MaLength SMA.
/// </summary>
public class OneTwoThreeReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _daysToHold;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<ICandleMessage> _history = [];
	private decimal? _prevClose;
	private decimal? _prevMa;
	private int _barsInPosition;

	/// <summary>
	/// Bars to hold the position.
	/// </summary>
	public int DaysToHold
	{
		get => _daysToHold.Value;
		set => _daysToHold.Value = value;
	}

	/// <summary>
	/// SMA period of the exit.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
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
	public OneTwoThreeReversalStrategy()
	{
		_daysToHold = Param(nameof(DaysToHold), 7)
			.SetGreaterThanZero()
			.SetDisplay("Days To Hold", "Bars to hold the position", "Trading");

		_maLength = Param(nameof(MaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "SMA period of the exit", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	private void ResetState()
	{
		_history.Clear();
		_prevClose = null;
		_prevMa = null;
		_barsInPosition = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var sma = new SimpleMovingAverage { Length = MaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_history.Add(candle);
		if (_history.Count > 5)
			_history.RemoveAt(0);

		decimal? ma = smaValue.IsFormed ? smaValue.GetValue<decimal>() : null;
		var prevClose = _prevClose;
		var prevMa = _prevMa;
		_prevClose = candle.ClosePrice;
		_prevMa = ma;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			_barsInPosition++;

			var crossAboveMa = ma is decimal m && prevMa is decimal pm && prevClose is decimal pc && pc <= pm && candle.ClosePrice > m;

			if (_barsInPosition >= DaysToHold || crossAboveMa)
			{
				SellMarket(Position);
				_barsInPosition = 0;
			}

			return;
		}

		if (_history.Count < 5)
			return;

		// _history[4] is the current bar, _history[0] is the bar four bars ago.
		var current = _history[4];
		var bar1 = _history[3];
		var bar2 = _history[2];
		var bar3 = _history[1];
		var bar4 = _history[0];

		var pattern = current.LowPrice < bar1.LowPrice
			&& bar1.LowPrice < bar3.LowPrice
			&& bar2.LowPrice < bar4.LowPrice
			&& bar2.HighPrice < bar3.HighPrice;

		if (pattern && Position == 0)
		{
			BuyMarket(Volume);
			_barsInPosition = 0;
		}
	}
}
