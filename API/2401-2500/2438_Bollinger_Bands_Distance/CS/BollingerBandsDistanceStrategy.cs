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

	private readonly List<decimal> _closes = new();

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
		_bbPeriod = Param(nameof(BollingerPeriod), 20)
			.SetDisplay("BB Period", "Bollinger Bands length", "Parameters");

		_bbDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetDisplay("Deviation", "Bollinger Bands deviation", "Parameters");

		_bandDistance = Param(nameof(BandDistance), 1m)
			.SetDisplay("Band Distance", "Extra distance from bands in price steps", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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
		_closes.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();

		StartProtection(
			new Unit(2000m, UnitTypes.Absolute),
			new Unit(1000m, UnitTypes.Absolute));
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closes.Add(candle.ClosePrice);
		if (_closes.Count > BollingerPeriod)
			_closes.RemoveAt(0);

		if (_closes.Count < BollingerPeriod)
			return;

		var sum = 0m;
		foreach (var close in _closes)
			sum += close;

		var middle = sum / _closes.Count;
		var variance = 0m;
		foreach (var close in _closes)
		{
			var delta = close - middle;
			variance += delta * delta;
		}

		var stdDev = (decimal)Math.Sqrt((double)(variance / _closes.Count));
		var upper = middle + BollingerDeviation * stdDev;
		var lower = middle - BollingerDeviation * stdDev;
		var closePrice = candle.ClosePrice;
		var distance = BandDistance * (Security?.PriceStep ?? 1m);

		if (Position > 0 && closePrice >= middle)
			SellMarket();
		else if (Position < 0 && closePrice <= middle)
			BuyMarket();

		if (Position == 0)
		{
			if (closePrice > upper + distance)
				SellMarket();
			else if (closePrice < lower - distance)
				BuyMarket();
		}
	}
}
