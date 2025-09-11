using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Demonstrates CAGR calculation and trades in the direction of the growth rate.
/// </summary>
public class TaLibraryStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private DateTimeOffset? _prevTime;
	private decimal _prevClose;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TaLibraryStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Candle Type", "Working timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevTime = null;
		_prevClose = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_prevTime is null)
		{
			_prevTime = candle.OpenTime;
			_prevClose = candle.ClosePrice;
			return;
		}

		var days = (candle.OpenTime - _prevTime.Value).TotalDays;
		if (days >= 1)
		{
			var rate = candle.ClosePrice / _prevClose;
			var years = days / 365m;
			var cagr = (decimal)(Math.Pow((double)rate, 1.0 / (double)years) - 1) * 100m;

			if (cagr > 0m && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
			else if (cagr < 0m && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		}

		_prevTime = candle.OpenTime;
		_prevClose = candle.ClosePrice;
	}
}
