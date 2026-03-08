using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// StdDev based channel breakout scalping strategy.
/// </summary>
public class ChannelScalperStrategy : Strategy
{
	private readonly StrategyParam<int> _stdevPeriod;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _up;
	private decimal _down;
	private int _direction;
	private bool _isInitialized;

	public int StdevPeriod { get => _stdevPeriod.Value; set => _stdevPeriod.Value = value; }
	public decimal Multiplier { get => _multiplier.Value; set => _multiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ChannelScalperStrategy()
	{
		_stdevPeriod = Param(nameof(StdevPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Period", "Standard deviation period", "General");
		_multiplier = Param(nameof(Multiplier), 1.5m)
			.SetDisplay("Multiplier", "Channel width multiplier", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle Type", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_up = 0;
		_down = 0;
		_direction = 0;
		_isInitialized = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var stdev = new StandardDeviation { Length = StdevPeriod };
		SubscribeCandles(CandleType).Bind(stdev, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal stdevValue)
	{
		if (candle.State != CandleStates.Finished || stdevValue <= 0) return;

		var middle = (candle.HighPrice + candle.LowPrice) / 2;
		var currentUp = middle + Multiplier * stdevValue;
		var currentDown = middle - Multiplier * stdevValue;

		if (!_isInitialized)
		{
			_up = currentUp;
			_down = currentDown;
			_isInitialized = true;
			return;
		}

		if (_direction <= 0 && candle.ClosePrice > _up)
		{
			if (Position < 0) BuyMarket();
			BuyMarket();
			_direction = 1;
		}
		else if (_direction >= 0 && candle.ClosePrice < _down)
		{
			if (Position > 0) SellMarket();
			SellMarket();
			_direction = -1;
		}

		if (_direction > 0)
			currentDown = Math.Max(currentDown, _down);
		else if (_direction < 0)
			currentUp = Math.Min(currentUp, _up);

		_up = currentUp;
		_down = currentDown;
	}
}
