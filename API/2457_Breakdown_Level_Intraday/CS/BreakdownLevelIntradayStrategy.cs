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
/// Breakout strategy that trades previous high/low levels.
/// Goes long when price breaks above a recent high, short when breaks below recent low.
/// </summary>
public class BreakdownLevelIntradayStrategy : Strategy
{
	private readonly StrategyParam<int> _lookback;
	private readonly StrategyParam<decimal> _stopLossPct;
	private readonly StrategyParam<decimal> _takeProfitPct;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _hasPrev;

	/// <summary>Lookback period for high/low.</summary>
	public int Lookback { get => _lookback.Value; set => _lookback.Value = value; }

	/// <summary>Stop loss percent.</summary>
	public decimal StopLossPct { get => _stopLossPct.Value; set => _stopLossPct.Value = value; }

	/// <summary>Take profit percent.</summary>
	public decimal TakeProfitPct { get => _takeProfitPct.Value; set => _takeProfitPct.Value = value; }

	/// <summary>Candle type.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BreakdownLevelIntradayStrategy()
	{
		_lookback = Param(nameof(Lookback), 60)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Number of bars for high/low", "Parameters");

		_stopLossPct = Param(nameof(StopLossPct), 0.5m)
			.SetDisplay("Stop Loss %", "Stop loss as percent of price", "Risk");

		_takeProfitPct = Param(nameof(TakeProfitPct), 1.0m)
			.SetDisplay("Take Profit %", "Take profit as percent of price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_prevHigh = 0m;
		_prevLow = 0m;
		_hasPrev = false;
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

		_hasPrev = false;

		var highest = new Highest { Length = Lookback };
		var lowest = new Lowest { Length = Lookback };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal high, decimal low)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (_hasPrev)
		{
			if (Position == 0)
			{
				// Breakout above previous highest level
				if (close > _prevHigh * 1.002m)
				{
					BuyMarket();
					_entryPrice = close;
					_stopPrice = close * (1 - StopLossPct / 100m);
					_takePrice = close * (1 + TakeProfitPct / 100m);
				}
				// Breakout below previous lowest level
				else if (close < _prevLow * 0.998m)
				{
					SellMarket();
					_entryPrice = close;
					_stopPrice = close * (1 + StopLossPct / 100m);
					_takePrice = close * (1 - TakeProfitPct / 100m);
				}
			}
			else if (Position > 0)
			{
				if (close >= _takePrice || close <= _stopPrice)
				{
					SellMarket(Math.Abs(Position));
				}
			}
			else if (Position < 0)
			{
				if (close <= _takePrice || close >= _stopPrice)
				{
					BuyMarket(Math.Abs(Position));
				}
			}
		}

		_prevHigh = high;
		_prevLow = low;
		_hasPrev = true;
	}
}
