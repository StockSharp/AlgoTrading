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
/// Strategy trading reversals from Bollinger Bands with extra distance.
/// </summary>
public class BollingerBandsDistanceStrategy : Strategy
{
	private readonly StrategyParam<int> _bbPeriod;
	private readonly StrategyParam<decimal> _bbDeviation;
	private readonly StrategyParam<decimal> _bandDistance;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;

	public int BollingerPeriod
	{
		get => _bbPeriod.Value;
		set => _bbPeriod.Value = value;
	}

	public decimal BollingerDeviation
	{
		get => _bbDeviation.Value;
		set => _bbDeviation.Value = value;
	}

	public decimal BandDistance
	{
		get => _bandDistance.Value;
		set => _bandDistance.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public BollingerBandsDistanceStrategy()
	{
		_bbPeriod = Param(nameof(BollingerPeriod), 4)
			.SetDisplay("BB Period", "Bollinger Bands length", "Parameters");

		_bbDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetDisplay("Deviation", "Bollinger Bands deviation", "Parameters");

		_bandDistance = Param(nameof(BandDistance), 3m)
			.SetDisplay("Band Distance", "Extra distance from bands in price steps", "Parameters");

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
		_entryPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bb = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bb, (candle, bbValue) =>
			{
				if (candle.State != CandleStates.Finished)
					return;

				var val = (IBollingerBandsValue)bbValue;
				if (val.UpBand is not decimal upper || val.LowBand is not decimal lower || val.MovingAverage is not decimal middle)
					return;

				var close = candle.ClosePrice;
				var step = Security?.PriceStep ?? 1m;
				var distance = BandDistance * step;

				// Exit logic: close at middle band
				if (Position > 0 && close >= middle)
				{
					SellMarket();
					_entryPrice = 0m;
				}
				else if (Position < 0 && close <= middle)
				{
					BuyMarket();
					_entryPrice = 0m;
				}

				// Entry logic: buy below lower band, sell above upper band
				if (Position == 0)
				{
					if (close > upper + distance)
					{
						SellMarket();
						_entryPrice = close;
					}
					else if (close < lower - distance)
					{
						BuyMarket();
						_entryPrice = close;
					}
				}
			})
			.Start();

		StartProtection(
			new Unit(2000m, UnitTypes.Absolute),
			new Unit(1000m, UnitTypes.Absolute));
	}
}
