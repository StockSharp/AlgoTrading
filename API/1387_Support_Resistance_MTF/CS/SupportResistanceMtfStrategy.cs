using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Utility strategy that logs support and resistance levels from a higher timeframe.
/// </summary>
public class SupportResistanceMtfStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _higherCandleType;
	private readonly StrategyParam<bool> _useHighLow;

	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private decimal _currentResistance;
	private decimal _currentSupport;

	/// <summary>
	/// Lookback period on the higher timeframe.
	/// </summary>
	public int Period { get => _period.Value; set => _period.Value = value; }

	/// <summary>
	/// Higher timeframe candle type.
	/// </summary>
	public DataType HigherCandleType { get => _higherCandleType.Value; set => _higherCandleType.Value = value; }

	/// <summary>
	/// Use high/low prices instead of close/open.
	/// </summary>
	public bool UseHighLow { get => _useHighLow.Value; set => _useHighLow.Value = value; }

	/// <summary>
	/// Initialize <see cref="SupportResistanceMtfStrategy"/>.
	/// </summary>
	public SupportResistanceMtfStrategy()
	{
		_period = Param(nameof(Period), 10)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Lookback length for levels", "General");

		_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Higher Candle Type", "Timeframe for level calculation", "General");

		_useHighLow = Param(nameof(UseHighLow), true)
			.SetDisplay("Use High/Low", "Use high/low or close/open", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, HigherCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_currentResistance = _currentSupport = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highest = new Highest { Length = Period };
		_lowest = new Lowest { Length = Period };

		var subscription = SubscribeCandles(HigherCandleType);
		subscription
			.Bind(ProcessHigherCandle)
			.Start();
	}

	private void ProcessHigherCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var highPrice = UseHighLow ? candle.HighPrice : Math.Max(candle.ClosePrice, candle.OpenPrice);
		var lowPrice = UseHighLow ? candle.LowPrice : Math.Min(candle.ClosePrice, candle.OpenPrice);

		var highVal = _highest.Process(highPrice, candle.ServerTime, true);
		var lowVal = _lowest.Process(lowPrice, candle.ServerTime, true);

		if (!_highest.IsFormed || !_lowest.IsFormed)
			return;

		_currentResistance = highVal.GetValue<decimal>();
		_currentSupport = lowVal.GetValue<decimal>();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_currentSupport == 0m || _currentResistance == 0m)
			return;

		// Buy near support, sell near resistance
		var range = _currentResistance - _currentSupport;
		if (range <= 0m) return;

		if (candle.ClosePrice <= _currentSupport + range * 0.1m && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (candle.ClosePrice >= _currentResistance - range * 0.1m && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
