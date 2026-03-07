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
/// VWAP breakout strategy with StdDev-based stop-loss and take-profit.
/// Uses SMA as VWAP proxy and StdDev for ATR-like volatility stops.
/// Enters on price crossing above/below the moving average.
/// </summary>
public class VwapBreakoutAtrStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _stdLength;
	private readonly StrategyParam<decimal> _stopMult;
	private readonly StrategyParam<decimal> _takeMult;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevMa;
	private bool _hasPrev;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;

	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public int StdLength { get => _stdLength.Value; set => _stdLength.Value = value; }
	public decimal StopMult { get => _stopMult.Value; set => _stopMult.Value = value; }
	public decimal TakeMult { get => _takeMult.Value; set => _takeMult.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VwapBreakoutAtrStrategy()
	{
		_maLength = Param(nameof(MaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average period", "Parameters");

		_stdLength = Param(nameof(StdLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Length", "Volatility period", "Parameters");

		_stopMult = Param(nameof(StopMult), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Mult", "StdDev multiplier for stop", "Parameters");

		_takeMult = Param(nameof(TakeMult), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Take Mult", "StdDev multiplier for TP", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = 0;
		_prevMa = 0;
		_hasPrev = false;
		_entryPrice = 0;
		_stopPrice = 0;
		_takePrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = MaLength };
		var stdDev = new StandardDeviation { Length = StdLength };

		_prevClose = 0;
		_prevMa = 0;
		_hasPrev = false;
		_entryPrice = 0;
		_stopPrice = 0;
		_takePrice = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, stdDev, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaVal, decimal stdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// TP/SL management
		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		if (_hasPrev && stdVal > 0)
		{
			var crossOver = _prevClose <= _prevMa && candle.ClosePrice > smaVal;
			var crossUnder = _prevClose >= _prevMa && candle.ClosePrice < smaVal;

			if (crossOver && Position <= 0)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice - stdVal * StopMult;
				_takePrice = _entryPrice + stdVal * TakeMult;
			}
			else if (crossUnder && Position >= 0)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice + stdVal * StopMult;
				_takePrice = _entryPrice - stdVal * TakeMult;
			}
		}

		_prevClose = candle.ClosePrice;
		_prevMa = smaVal;
		_hasPrev = true;
	}
}
