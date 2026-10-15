using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Custom Signal Oscillator strategy.
/// The oscillator is the difference between two price signals of the candle, its close and its open.
/// A cross above zero goes long and a cross below zero goes short, reversing an opposite position.
/// In long-only mode a cross below zero only closes the long.
/// </summary>
public class CustomSignalOscillatorStrategy : Strategy
{
	private readonly StrategyParam<bool> _longOnly;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevOscillator;

	/// <summary>
	/// Trade only long positions.
	/// </summary>
	public bool LongOnly
	{
		get => _longOnly.Value;
		set => _longOnly.Value = value;
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
	/// Constructor.
	/// </summary>
	public CustomSignalOscillatorStrategy()
	{
		_longOnly = Param(nameof(LongOnly), false)
			.SetDisplay("Long Only", "Trade only long positions", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevOscillator = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevOscillator = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var oscillator = candle.ClosePrice - candle.OpenPrice;
		var prev = _prevOscillator;
		_prevOscillator = oscillator;

		if (prev is not decimal prevOscillator)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var crossUp = prevOscillator <= 0m && oscillator > 0m;
		var crossDown = prevOscillator >= 0m && oscillator < 0m;

		if (crossUp && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (crossDown)
		{
			if (LongOnly)
			{
				if (Position > 0)
					SellMarket(Position);
			}
			else if (Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
			}
		}
	}
}
