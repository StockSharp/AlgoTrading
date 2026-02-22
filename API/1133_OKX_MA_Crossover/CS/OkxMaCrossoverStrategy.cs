using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class OkxMaCrossoverStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevMa;
	private bool _hasPrevMa;
	private bool _prevDoLong1;
	private bool _prevDoLong2;
	private bool _prevDoShort1;
	private bool _prevDoShort2;

	public int Length { get => _length.Value; set => _length.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLossPercent { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OkxMaCrossoverStrategy()
	{
		_length = Param(nameof(Length), 13).SetGreaterThanZero();
		_takeProfit = Param(nameof(TakeProfitPercent), 7m);
		_stopLoss = Param(nameof(StopLossPercent), 7m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrevMa = false;
		_prevDoLong1 = false;
		_prevDoLong2 = false;
		_prevDoShort1 = false;
		_prevDoShort2 = false;

		var sma = new SimpleMovingAverage { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, ProcessCandle).Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent));

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

		if (!_hasPrevMa)
		{
			_prevMa = maValue;
			_hasPrevMa = true;
			ShiftSignals(false, false);
			return;
		}

		var doLong = candle.LowPrice < _prevMa;
		var doShort = candle.HighPrice > _prevMa;

		if (!_prevDoLong2 && doLong && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
		}
		else if (!_prevDoShort2 && doShort && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
		}

		_prevMa = maValue;
		ShiftSignals(doLong, doShort);
	}

	private void ShiftSignals(bool currentLong, bool currentShort)
	{
		_prevDoLong2 = _prevDoLong1;
		_prevDoLong1 = currentLong;
		_prevDoShort2 = _prevDoShort1;
		_prevDoShort1 = currentShort;
	}
}
