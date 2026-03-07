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
/// Detects 1-2-3 bottom pattern (descending lows with rising highs) and buys.
/// Exits after holding period or when price crosses above MA.
/// </summary>
public class OneTwoThreeReversalStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _holdBars;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _cooldownBars;

	private SimpleMovingAverage _sma;

	private decimal _low1, _low2, _low3, _low4;
	private decimal _high1, _high2, _high3;
	private int _historyCount;
	private int _barsSinceEntry;
	private int _cooldownRemaining;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int HoldBars
	{
		get => _holdBars.Value;
		set => _holdBars.Value = value;
	}

	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public OneTwoThreeReversalStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_holdBars = Param(nameof(HoldBars), 15)
			.SetGreaterThanZero()
			.SetDisplay("Hold Bars", "Bars to hold position", "Trading");

		_maLength = Param(nameof(MaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average period", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_sma = null;
		_low1 = _low2 = _low3 = _low4 = 0;
		_high1 = _high2 = _high3 = 0;
		_historyCount = 0;
		_barsSinceEntry = int.MaxValue;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_sma = new SimpleMovingAverage { Length = MaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_sma, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_sma.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
			_barsSinceEntry++;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			UpdateHistory(candle);
			return;
		}

		if (_historyCount >= 4)
		{
			// Exit conditions
			if (Position > 0 && (_barsSinceEntry >= HoldBars || candle.ClosePrice >= maValue))
			{
				SellMarket(Math.Abs(Position));
				_barsSinceEntry = int.MaxValue;
				_cooldownRemaining = CooldownBars;
			}
			// 1-2-3 bottom pattern: descending lows (bearish trend weakening)
			// + highs starting to rise (bullish reversal)
			else if (Position <= 0)
			{
				var condition1 = candle.LowPrice < _low1;
				var condition2 = _low1 < _low3;
				var condition3 = _low2 < _low4;
				var condition4 = _high2 < _high3;

				if (condition1 && condition2 && condition3 && condition4)
				{
					if (Position < 0)
						BuyMarket(Math.Abs(Position));
					BuyMarket(Volume);
					_barsSinceEntry = 0;
					_cooldownRemaining = CooldownBars;
				}
			}
		}

		UpdateHistory(candle);
	}

	private void UpdateHistory(ICandleMessage candle)
	{
		_low4 = _low3;
		_low3 = _low2;
		_low2 = _low1;
		_low1 = candle.LowPrice;

		_high3 = _high2;
		_high2 = _high1;
		_high1 = candle.HighPrice;

		if (_historyCount < 4)
			_historyCount++;
	}
}
