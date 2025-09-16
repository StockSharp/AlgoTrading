using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trailing stop management based on CandleStop logic.
/// </summary>
public class CandleStopTrailingStrategy : Strategy
{
	private readonly StrategyParam<int> _upTrailPeriods;
	private readonly StrategyParam<int> _dnTrailPeriods;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopPrice;

	public CandleStopTrailingStrategy()
	{
		_upTrailPeriods = Param(nameof(UpTrailPeriods), 5)
			.SetDisplay("Up Trail Periods", "Look-back period for highest high used in short trailing.", "Parameters");

		_dnTrailPeriods = Param(nameof(DnTrailPeriods), 5)
			.SetDisplay("Down Trail Periods", "Look-back period for lowest low used in long trailing.", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for analysis.", "General");
	}

	/// <summary>
	/// Look-back period to calculate highest high for short positions.
	/// </summary>
	public int UpTrailPeriods
	{
		get => _upTrailPeriods.Value;
		set => _upTrailPeriods.Value = value;
	}

	/// <summary>
	/// Look-back period to calculate lowest low for long positions.
	/// </summary>
	public int DnTrailPeriods
	{
		get => _dnTrailPeriods.Value;
		set => _dnTrailPeriods.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_stopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var upChannel = new DonchianChannels { Length = UpTrailPeriods };
		var downChannel = new DonchianChannels { Length = DnTrailPeriods };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(upChannel, downChannel, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, upChannel);
			DrawIndicator(area, downChannel);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue upValue, IIndicatorValue downValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var up = (DonchianChannelsValue)upValue;
		var dn = (DonchianChannelsValue)downValue;

		if (up.UpperBand is not decimal upper || dn.LowerBand is not decimal lower)
			return;

		if (Position > 0)
		{
			if (_stopPrice == 0m || lower > _stopPrice)
				_stopPrice = lower;

			if (candle.LowPrice <= _stopPrice)
			{
				SellMarket(Position);
				_stopPrice = 0m;
			}
		}
		else if (Position < 0)
		{
			if (_stopPrice == 0m || upper < _stopPrice)
				_stopPrice = upper;

			if (candle.HighPrice >= _stopPrice)
			{
				BuyMarket(Math.Abs(Position));
				_stopPrice = 0m;
			}
		}
		else
		{
			_stopPrice = 0m;
		}
	}
}
