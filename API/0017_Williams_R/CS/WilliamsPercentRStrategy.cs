using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Williams %R indicator.
/// Buys when Williams %R crosses from oversold zone upward,
/// sells when it crosses from overbought zone downward.
/// </summary>
public class WilliamsPercentRStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevWR;
	private bool _hasPrevValues;
	private int _cooldown;

	/// <summary>
	/// Williams %R period.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
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
	/// Initializes a new instance of the <see cref="WilliamsPercentRStrategy"/>.
	/// </summary>
	public WilliamsPercentRStrategy()
	{
		_period = Param(nameof(Period), 14)
			.SetDisplay("Period", "Period for Williams %R calculation", "Indicators")
			.SetOptimize(10, 20, 2);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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
		_prevWR = default;
		_hasPrevValues = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var williamsR = new WilliamsR { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(williamsR, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, williamsR);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal wrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_hasPrevValues)
		{
			_hasPrevValues = true;
			_prevWR = wrValue;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevWR = wrValue;
			return;
		}

		// Williams %R crosses from oversold (-80) upward - buy signal
		if (_prevWR < -80 && wrValue >= -80 && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_cooldown = 3;
		}
		// Williams %R crosses from overbought (-20) downward - sell signal
		else if (_prevWR > -20 && wrValue <= -20 && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_cooldown = 3;
		}

		_prevWR = wrValue;
	}
}
