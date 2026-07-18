using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Aroon Oscillator crossing predefined levels.
/// Opens long positions when the oscillator rises above the down level.
/// Opens short positions when the oscillator falls below the up level.
/// </summary>
public class AroonOscillatorSignAlertStrategy : Strategy
{
	private readonly StrategyParam<int> _aroonPeriod;
	private readonly StrategyParam<int> _upLevel;
	private readonly StrategyParam<int> _downLevel;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousValue;

	public int AroonPeriod
	{
		get => _aroonPeriod.Value;
		set => _aroonPeriod.Value = value;
	}

	public int UpLevel
	{
		get => _upLevel.Value;
		set => _upLevel.Value = value;
	}

	public int DownLevel
	{
		get => _downLevel.Value;
		set => _downLevel.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public AroonOscillatorSignAlertStrategy()
	{
		_aroonPeriod = Param(nameof(AroonPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Aroon Period", "Lookback for Aroon oscillator", "Indicator");

		_upLevel = Param(nameof(UpLevel), 50)
			.SetDisplay("Up Level", "Upper threshold for sell signal", "Indicator");

		_downLevel = Param(nameof(DownLevel), -50)
			.SetDisplay("Down Level", "Lower threshold for buy signal", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for processing", "General");
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
		_previousValue = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_previousValue = null;

		var aroon = new AroonOscillator { Length = AroonPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(aroon, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, aroon);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal aroonValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_previousValue is null)
		{
			_previousValue = aroonValue;
			return;
		}

		if (_previousValue <= DownLevel && aroonValue > DownLevel && Position <= 0)
			BuyMarket();
		else if (_previousValue >= UpLevel && aroonValue < UpLevel && Position >= 0)
			SellMarket();

		_previousValue = aroonValue;
	}
}
