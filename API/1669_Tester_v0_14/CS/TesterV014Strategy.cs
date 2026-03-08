using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Tester strategy using SMA and MACD for entries.
/// Closes after specified number of bars.
/// </summary>
public class TesterV014Strategy : Strategy
{
	private readonly StrategyParam<int> _barsNumber;
	private readonly StrategyParam<DataType> _candleType;

	private int _barsCounter;
	private bool _positionOpened;

	public int BarsNumber { get => _barsNumber.Value; set => _barsNumber.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TesterV014Strategy()
	{
		_barsNumber = Param(nameof(BarsNumber), 3)
			.SetGreaterThanZero()
			.SetDisplay("Bars Number", "Holding period in bars", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_barsCounter = 0;
		_positionOpened = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = 14 };
		var macd = new MovingAverageConvergenceDivergence();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(sma, macd, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaVal, decimal macdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Close after specified bars
		if (_positionOpened)
		{
			_barsCounter++;
			if (_barsCounter >= BarsNumber)
			{
				if (Position > 0)
					SellMarket();
				else if (Position < 0)
					BuyMarket();
				_positionOpened = false;
				_barsCounter = 0;
			}
		}

		// Entry
		if (Position == 0 && !_positionOpened)
		{
			if (candle.ClosePrice > smaVal && macdVal > 0)
			{
				BuyMarket();
				_barsCounter = 0;
				_positionOpened = true;
			}
			else if (candle.ClosePrice < smaVal && macdVal < 0)
			{
				SellMarket();
				_barsCounter = 0;
				_positionOpened = true;
			}
		}
	}
}
