using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on two moving averages with CCI filter and ATR stop-loss.
/// Opens long when fast MA crosses above slow MA and CCI > 0.
/// Opens short when fast MA crosses below slow MA and CCI < 0.
/// </summary>
public class Ma2CciStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isInitialized;
	private decimal _stopLoss;

	public int FastMaPeriod { get => _fastMaPeriod.Value; set => _fastMaPeriod.Value = value; }
	public int SlowMaPeriod { get => _slowMaPeriod.Value; set => _slowMaPeriod.Value = value; }
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Ma2CciStrategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Period", "Period of the fast moving average", "Indicators");
		_slowMaPeriod = Param(nameof(SlowMaPeriod), 15)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Period", "Period of the slow moving average", "Indicators");
		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Period for CCI filter", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period for ATR stop-loss", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevFast = 0;
		_prevSlow = 0;
		_isInitialized = false;
		_stopLoss = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastMa = new ExponentialMovingAverage { Length = FastMaPeriod };
		var slowMa = new ExponentialMovingAverage { Length = SlowMaPeriod };
		var cci = new CommodityChannelIndex { Length = CciPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		SubscribeCandles(CandleType)
			.Bind(fastMa, slowMa, cci, atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal cciValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished) return;

		if (!_isInitialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_isInitialized = true;
			return;
		}

		// Stop-loss check
		if (Position > 0 && _stopLoss > 0 && candle.ClosePrice <= _stopLoss)
		{
			SellMarket();
			_stopLoss = 0;
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}
		else if (Position < 0 && _stopLoss > 0 && candle.ClosePrice >= _stopLoss)
		{
			BuyMarket();
			_stopLoss = 0;
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		var isFastAbove = fast > slow;
		var wasFastAbove = _prevFast > _prevSlow;

		// MA crossover up => long (CCI as optional filter)
		if (isFastAbove && !wasFastAbove)
		{
			if (Position < 0) BuyMarket();
			if (Position <= 0)
			{
				BuyMarket();
				if (atrValue > 0)
					_stopLoss = candle.ClosePrice - atrValue * 2;
			}
		}
		// MA crossover down => short
		else if (!isFastAbove && wasFastAbove)
		{
			if (Position > 0) SellMarket();
			if (Position >= 0)
			{
				SellMarket();
				if (atrValue > 0)
					_stopLoss = candle.ClosePrice + atrValue * 2;
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
