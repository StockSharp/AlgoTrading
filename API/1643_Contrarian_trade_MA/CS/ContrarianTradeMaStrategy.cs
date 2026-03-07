using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Weekly contrarian strategy using moving average and extreme price levels.
/// </summary>
public class ContrarianTradeMaStrategy : Strategy
{
	private readonly StrategyParam<int> _calcPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private int _barsInPosition;
	private decimal _prevHighest;
	private decimal _prevLowest;
	private decimal _prevSma;
	private bool _hasPrev;

	public int CalcPeriod { get => _calcPeriod.Value; set => _calcPeriod.Value = value; }
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ContrarianTradeMaStrategy()
	{
		_calcPeriod = Param(nameof(CalcPeriod), 10)
			.SetDisplay("Calc Period", "Lookback period for extremes", "General");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetDisplay("MA Period", "Moving average period", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = 0;
		_barsInPosition = 0;
		_prevHighest = 0;
		_prevLowest = 0;
		_prevSma = 0;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var highest = new Highest { Length = CalcPeriod };
		var lowest = new Lowest { Length = CalcPeriod };
		var sma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(highest, lowest, sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal highest, decimal lowest, decimal sma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_hasPrev)
		{
			_prevClose = candle.ClosePrice;
			_prevHighest = highest;
			_prevLowest = lowest;
			_prevSma = sma;
			_hasPrev = true;
			return;
		}

		if (Position == 0)
		{
			if (_prevHighest < _prevClose && Position <= 0)
			{
				BuyMarket();
				_barsInPosition = 0;
			}
			else if (_prevLowest > _prevClose && Position >= 0)
			{
				SellMarket();
				_barsInPosition = 0;
			}
			else if (_prevSma > candle.OpenPrice && Position <= 0)
			{
				BuyMarket();
				_barsInPosition = 0;
			}
			else if (_prevSma < candle.OpenPrice && Position >= 0)
			{
				SellMarket();
				_barsInPosition = 0;
			}
		}
		else
		{
			_barsInPosition++;

			if (_barsInPosition >= CalcPeriod)
			{
				if (Position > 0)
					SellMarket();
				else
					BuyMarket();

				_barsInPosition = 0;
			}
		}

		_prevClose = candle.ClosePrice;
		_prevHighest = highest;
		_prevLowest = lowest;
		_prevSma = sma;
	}
}
