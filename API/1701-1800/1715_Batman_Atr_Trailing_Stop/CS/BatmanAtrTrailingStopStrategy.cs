using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on ATR trailing stop similar to "Batman" EA.
/// Opens long when price breaks above ATR-based support.
/// Opens short when price breaks below ATR-based resistance.
/// </summary>
public class BatmanAtrTrailingStopStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _levelUp;
	private decimal _levelDown;
	private int _direction;
	private bool _isInitialized;

	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal Factor { get => _factor.Value; set => _factor.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public BatmanAtrTrailingStopStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR indicator period", "General");

		_factor = Param(nameof(Factor), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Factor", "Multiplier for ATR distance", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_levelUp = 0;
		_levelDown = 0;
		_direction = 1;
		_isInitialized = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var stdev = new StandardDeviation { Length = AtrPeriod };
		SubscribeCandles(CandleType).Bind(stdev, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal stdevValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var close = candle.ClosePrice;
		var currUp = close - stdevValue * Factor;
		var currDown = close + stdevValue * Factor;

		if (!_isInitialized)
		{
			_levelUp = currUp;
			_levelDown = currDown;
			_isInitialized = true;
			return;
		}

		if (_direction == 1)
		{
			if (currUp > _levelUp)
				_levelUp = currUp;

			if (candle.LowPrice < _levelUp)
			{
				_direction = -1;
				_levelDown = currDown;
				if (Position > 0) SellMarket();
				SellMarket();
			}
		}
		else
		{
			if (currDown < _levelDown)
				_levelDown = currDown;

			if (candle.HighPrice > _levelDown)
			{
				_direction = 1;
				_levelUp = currUp;
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
		}
	}
}
