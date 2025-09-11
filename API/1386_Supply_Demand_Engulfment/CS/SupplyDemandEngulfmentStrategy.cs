using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Supply and demand zones with engulfing pattern entries.
/// </summary>
public class SupplyDemandEngulfmentStrategy : Strategy
{
	private readonly StrategyParam<int> _zonePeriod;
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage _prevCandle;

	/// <summary>
	/// Period for zone detection.
	/// </summary>
	public int ZonePeriod
	{
		get => _zonePeriod.Value;
		set => _zonePeriod.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize the strategy.
	/// </summary>
	public SupplyDemandEngulfmentStrategy()
	{
		_zonePeriod = Param(nameof(ZonePeriod), 20)
			.SetDisplay("Zone Period", "Lookback for zones", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 2);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevCandle = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var donchian = new DonchianChannel { Length = ZonePeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(donchian, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal upper, decimal lower)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevCandle == null)
		{
			_prevCandle = candle;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevCandle = candle;
			return;
		}

		var bullishEngulf = candle.OpenPrice < _prevCandle.ClosePrice && candle.ClosePrice > _prevCandle.OpenPrice && _prevCandle.ClosePrice < _prevCandle.OpenPrice;
		var bearishEngulf = candle.OpenPrice > _prevCandle.ClosePrice && candle.ClosePrice < _prevCandle.OpenPrice && _prevCandle.ClosePrice > _prevCandle.OpenPrice;

		if (bullishEngulf && candle.LowPrice <= lower && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (bearishEngulf && candle.HighPrice >= upper && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}

		_prevCandle = candle;
	}
}
