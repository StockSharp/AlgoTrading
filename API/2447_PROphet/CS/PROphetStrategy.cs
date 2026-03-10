using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// PROphet strategy translated from MQL.
/// Uses price ranges of previous candles to generate signals
/// and applies trailing stops.
/// </summary>
public class PROphetStrategy : Strategy
{
	private readonly StrategyParam<int> _x1;
	private readonly StrategyParam<int> _x2;
	private readonly StrategyParam<int> _x3;
	private readonly StrategyParam<int> _x4;
	private readonly StrategyParam<int> _y1;
	private readonly StrategyParam<int> _y2;
	private readonly StrategyParam<int> _y3;
	private readonly StrategyParam<int> _y4;
	private readonly StrategyParam<decimal> _stopMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopPrice;
	private decimal _prevHigh1;
	private decimal _prevLow1;
	private decimal _prevHigh2;
	private decimal _prevLow2;
	private decimal _prevHigh3;
	private decimal _prevLow3;
	private int _historyCount;

	/// <summary>First coefficient for the buy signal.</summary>
	public int X1 { get => _x1.Value; set => _x1.Value = value; }
	/// <summary>Second coefficient for the buy signal.</summary>
	public int X2 { get => _x2.Value; set => _x2.Value = value; }
	/// <summary>Third coefficient for the buy signal.</summary>
	public int X3 { get => _x3.Value; set => _x3.Value = value; }
	/// <summary>Fourth coefficient for the buy signal.</summary>
	public int X4 { get => _x4.Value; set => _x4.Value = value; }

	/// <summary>First coefficient for the sell signal.</summary>
	public int Y1 { get => _y1.Value; set => _y1.Value = value; }
	/// <summary>Second coefficient for the sell signal.</summary>
	public int Y2 { get => _y2.Value; set => _y2.Value = value; }
	/// <summary>Third coefficient for the sell signal.</summary>
	public int Y3 { get => _y3.Value; set => _y3.Value = value; }
	/// <summary>Fourth coefficient for the sell signal.</summary>
	public int Y4 { get => _y4.Value; set => _y4.Value = value; }

	/// <summary>Stop loss multiplier.</summary>
	public decimal StopMultiplier { get => _stopMultiplier.Value; set => _stopMultiplier.Value = value; }

	/// <summary>The candle type.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public PROphetStrategy()
	{
		_x1 = Param(nameof(X1), 9).SetDisplay("X1", "First coefficient for buy", "Buy");
		_x2 = Param(nameof(X2), 29).SetDisplay("X2", "Second coefficient for buy", "Buy");
		_x3 = Param(nameof(X3), 94).SetDisplay("X3", "Third coefficient for buy", "Buy");
		_x4 = Param(nameof(X4), 125).SetDisplay("X4", "Fourth coefficient for buy", "Buy");

		_y1 = Param(nameof(Y1), 61).SetDisplay("Y1", "First coefficient for sell", "Sell");
		_y2 = Param(nameof(Y2), 100).SetDisplay("Y2", "Second coefficient for sell", "Sell");
		_y3 = Param(nameof(Y3), 117).SetDisplay("Y3", "Third coefficient for sell", "Sell");
		_y4 = Param(nameof(Y4), 31).SetDisplay("Y4", "Fourth coefficient for sell", "Sell");

		_stopMultiplier = Param(nameof(StopMultiplier), 0.005m)
			.SetDisplay("Stop Multiplier", "Stop loss as fraction of price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculations", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_stopPrice = 0m;
		_prevHigh1 = 0m;
		_prevLow1 = 0m;
		_prevHigh2 = 0m;
		_prevLow2 = 0m;
		_prevHigh3 = 0m;
		_prevLow3 = 0m;
		_historyCount = 0;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_historyCount = 0;
		_stopPrice = 0;

		var atr = new AverageTrueRange { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (_historyCount >= 3)
		{
			if (Position > 0)
			{
				// Trailing stop for long
				var newStop = close - atrValue * 2;
				if (newStop > _stopPrice)
					_stopPrice = newStop;

				if (close <= _stopPrice)
					SellMarket(Math.Abs(Position));
			}
			else if (Position < 0)
			{
				// Trailing stop for short
				var newStop = close + atrValue * 2;
				if (newStop < _stopPrice || _stopPrice == 0)
					_stopPrice = newStop;

				if (close >= _stopPrice)
					BuyMarket(Math.Abs(Position));
			}
			else
			{
				var buySignal = Qu(X1, X2, X3, X4);
				var sellSignal = Qu(Y1, Y2, Y3, Y4);

				if (buySignal > 0 && sellSignal <= 0)
				{
					_stopPrice = close - atrValue * 2;
					BuyMarket();
				}
				else if (sellSignal > 0 && buySignal <= 0)
				{
					_stopPrice = close + atrValue * 2;
					SellMarket();
				}
			}
		}

		UpdateHistory(candle);
	}

	private void UpdateHistory(ICandleMessage candle)
	{
		_prevHigh3 = _prevHigh2;
		_prevLow3 = _prevLow2;
		_prevHigh2 = _prevHigh1;
		_prevLow2 = _prevLow1;
		_prevHigh1 = candle.HighPrice;
		_prevLow1 = candle.LowPrice;

		if (_historyCount < 3)
			_historyCount++;
	}

	private decimal Qu(int q1, int q2, int q3, int q4)
	{
		return (q1 - 100) * Math.Abs(_prevHigh1 - _prevLow2)
			+ (q2 - 100) * Math.Abs(_prevHigh3 - _prevLow2)
			+ (q3 - 100) * Math.Abs(_prevHigh2 - _prevLow1)
			+ (q4 - 100) * Math.Abs(_prevHigh2 - _prevLow3);
	}
}
