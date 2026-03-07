using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MA2CCI strategy combines two EMAs with CCI and StdDev-based stop.
/// </summary>
public class MA2CCIStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _cciPeriod;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _prevCci;
	private bool _hasPrev;
	private decimal _stopPrice;
	private decimal _entryPrice;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastMaPeriod { get => _fastMaPeriod.Value; set => _fastMaPeriod.Value = value; }
	public int SlowMaPeriod { get => _slowMaPeriod.Value; set => _slowMaPeriod.Value = value; }
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }

	public MA2CCIStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA", "Fast moving average period", "Parameters");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA", "Slow moving average period", "Parameters");

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Commodity Channel Index period", "Parameters");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0;
		_prevSlow = 0;
		_prevCci = 0;
		_hasPrev = false;
		_stopPrice = 0;
		_entryPrice = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastMa = new ExponentialMovingAverage { Length = FastMaPeriod };
		var slowMa = new ExponentialMovingAverage { Length = SlowMaPeriod };
		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var stdDev = new StandardDeviation { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, slowMa, cci, stdDev, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal cci, decimal stdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Check stop loss
		if (_stopPrice > 0)
		{
			if (Position > 0 && candle.LowPrice <= _stopPrice)
			{
				SellMarket();
				_stopPrice = 0;
				_entryPrice = 0;
			}
			else if (Position < 0 && candle.HighPrice >= _stopPrice)
			{
				BuyMarket();
				_stopPrice = 0;
				_entryPrice = 0;
			}
		}

		if (!_hasPrev)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_prevCci = cci;
			_hasPrev = true;
			return;
		}

		// Entry signals: MA crossover
		if (fast > slow && _prevFast <= _prevSlow && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_stopPrice = candle.ClosePrice - stdVal * 2;
		}
		else if (fast < slow && _prevFast >= _prevSlow && Position >= 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
			_stopPrice = candle.ClosePrice + stdVal * 2;
		}

		_prevFast = fast;
		_prevSlow = slow;
		_prevCci = cci;
	}
}
