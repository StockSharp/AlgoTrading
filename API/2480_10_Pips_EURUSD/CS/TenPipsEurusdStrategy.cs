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
/// Breakout strategy: enters long when price breaks above previous candle high,
/// enters short when price breaks below previous candle low.
/// Uses ATR-based stop loss and take profit.
/// </summary>
public class TenPipsEurusdStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossMult;
	private readonly StrategyParam<decimal> _takeProfitMult;
	private readonly StrategyParam<decimal> _trailingMult;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHigh;
	private decimal _prevLow;
	private decimal _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private bool _hasPrev;

	public decimal StopLossMult { get => _stopLossMult.Value; set => _stopLossMult.Value = value; }
	public decimal TakeProfitMult { get => _takeProfitMult.Value; set => _takeProfitMult.Value = value; }
	public decimal TrailingMult { get => _trailingMult.Value; set => _trailingMult.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TenPipsEurusdStrategy()
	{
		_stopLossMult = Param(nameof(StopLossMult), 1.0m)
			.SetGreaterThanZero()
			.SetDisplay("SL Mult", "Stop loss ATR multiplier", "Risk");

		_takeProfitMult = Param(nameof(TakeProfitMult), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("TP Mult", "Take profit ATR multiplier", "Risk");

		_trailingMult = Param(nameof(TrailingMult), 0.8m)
			.SetGreaterThanZero()
			.SetDisplay("Trail Mult", "Trailing stop ATR multiplier", "Risk");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR calculation length", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevHigh = 0;
		_prevLow = 0;
		_entryPrice = 0;
		_stopPrice = null;
		_takePrice = null;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_hasPrev = true;
			return;
		}

		var close = candle.ClosePrice;

		// Manage existing position
		if (Position > 0)
		{
			// Trail
			var trail = close - TrailingMult * atr;
			if (_stopPrice == null || trail > _stopPrice)
				_stopPrice = trail;

			if (close <= _stopPrice || (_takePrice != null && close >= _takePrice))
			{
				SellMarket(Math.Abs(Position));
				_stopPrice = null;
				_takePrice = null;
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			var trail = close + TrailingMult * atr;
			if (_stopPrice == null || trail < _stopPrice)
				_stopPrice = trail;

			if (close >= _stopPrice || (_takePrice != null && close <= _takePrice))
			{
				BuyMarket(Math.Abs(Position));
				_stopPrice = null;
				_takePrice = null;
				_entryPrice = 0;
			}
		}

		// Entry on breakout
		if (_hasPrev && Position == 0)
		{
			if (close > _prevHigh)
			{
				BuyMarket();
				_entryPrice = close;
				_stopPrice = close - StopLossMult * atr;
				_takePrice = close + TakeProfitMult * atr;
			}
			else if (close < _prevLow)
			{
				SellMarket();
				_entryPrice = close;
				_stopPrice = close + StopLossMult * atr;
				_takePrice = close - TakeProfitMult * atr;
			}
		}

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_hasPrev = true;
	}
}
