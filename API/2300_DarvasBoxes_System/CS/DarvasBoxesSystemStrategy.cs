using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Darvas Boxes breakout strategy using Donchian Channels.
/// Opens long position when price breaks above the upper box line.
/// Opens short position when price breaks below the lower box line.
/// </summary>
public class DarvasBoxesSystemStrategy : Strategy
{
	private readonly StrategyParam<int> _boxPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevUpper;
	private decimal _prevLower;
	private decimal _prevClose;

	public int BoxPeriod
	{
		get => _boxPeriod.Value;
		set => _boxPeriod.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public DarvasBoxesSystemStrategy()
	{
		_boxPeriod = Param(nameof(BoxPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Box Period", "Period for box calculation", "Indicators")
			.SetOptimize(10, 40, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevUpper = 0m;
		_prevLower = 0m;
		_prevClose = 0m;

		var donchian = new DonchianChannels { Length = BoxPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(donchian, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, donchian);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (value is not IDonchianChannelsValue box)
			return;

		if (box.UpperBand is not decimal upper || box.LowerBand is not decimal lower)
			return;

		if (_prevUpper == 0m)
		{
			_prevUpper = upper;
			_prevLower = lower;
			_prevClose = candle.ClosePrice;
			return;
		}

		var isUpBreakout = candle.ClosePrice > _prevUpper && _prevClose <= _prevUpper;
		var isDownBreakout = candle.ClosePrice < _prevLower && _prevClose >= _prevLower;

		if (isUpBreakout && Position <= 0)
			BuyMarket();
		else if (isDownBreakout && Position >= 0)
			SellMarket();

		_prevUpper = upper;
		_prevLower = lower;
		_prevClose = candle.ClosePrice;
	}
}
