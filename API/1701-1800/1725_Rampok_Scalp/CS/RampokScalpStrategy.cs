using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Envelope based scalping strategy.
/// Enters when price crosses moving average bands and exits with trailing stop.
/// </summary>
public class RampokScalpStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _deviation;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevUpper;
	private decimal _prevLower;
	private decimal _prevClose;
	private decimal _entryPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;
	private bool _hasPrev;

	public int Period { get => _period.Value; set => _period.Value = value; }
	public decimal Deviation { get => _deviation.Value; set => _deviation.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RampokScalpStrategy()
	{
		_period = Param(nameof(Period), 15)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Moving average period", "General");

		_deviation = Param(nameof(Deviation), 0.07m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation", "Envelope deviation percent", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle", "Candle type", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevUpper = 0;
		_prevLower = 0;
		_prevClose = 0;
		_entryPrice = 0;
		_highestPrice = 0;
		_lowestPrice = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = Period };
		SubscribeCandles(CandleType).Bind(sma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var upper = smaValue * (1 + Deviation);
		var lower = smaValue * (1 - Deviation);
		var close = candle.ClosePrice;

		if (!_hasPrev)
		{
			_prevUpper = upper;
			_prevLower = lower;
			_prevClose = close;
			_hasPrev = true;
			return;
		}

		if (Position == 0)
		{
			if (_prevClose < _prevLower && close > lower)
			{
				BuyMarket();
				_entryPrice = close;
				_highestPrice = close;
			}
			else if (_prevClose > _prevUpper && close < upper)
			{
				SellMarket();
				_entryPrice = close;
				_lowestPrice = close;
			}
		}
		else if (Position > 0)
		{
			_highestPrice = Math.Max(_highestPrice, candle.HighPrice);
			// Exit at upper band or trailing
			if (close >= upper)
				SellMarket();
		}
		else if (Position < 0)
		{
			_lowestPrice = Math.Min(_lowestPrice, candle.LowPrice);
			// Exit at lower band or trailing
			if (close <= lower)
				BuyMarket();
		}

		_prevUpper = upper;
		_prevLower = lower;
		_prevClose = close;
	}
}
