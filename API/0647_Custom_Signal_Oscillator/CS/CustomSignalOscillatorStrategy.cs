using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using difference between two price signals.
/// Long when oscillator crosses above zero, short when it crosses below zero.
/// Optional long-only mode closes position on negative cross.
/// </summary>
public class CustomSignalOscillatorStrategy : Strategy
{
	private readonly StrategyParam<bool> _longOnly;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevOsc;

	/// <summary>
	/// Allow only long positions.
	/// </summary>
	public bool LongOnly
	{
		get => _longOnly.Value;
		set => _longOnly.Value = value;
	}

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public CustomSignalOscillatorStrategy()
	{
		_longOnly = Param(nameof(LongOnly), false)
			.SetDisplay("Long only", "Enable only long trades", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy", "General");
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
		_prevOsc = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var osc = candle.OpenPrice - candle.ClosePrice;

		if (_prevOsc is decimal prev)
		{
			var longEntry = prev <= 0 && osc > 0;
			var shortEntry = prev >= 0 && osc < 0;

			if (longEntry && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				LogInfo($"Buy signal: oscillator {prev} -> {osc}");
			}
			else if (shortEntry)
			{
				if (LongOnly)
				{
					if (Position > 0)
					{
						SellMarket(Position);
						LogInfo($"Exit long: oscillator {prev} -> {osc}");
					}
				}
				else if (Position >= 0)
				{
					SellMarket(Volume + Math.Abs(Position));
					LogInfo($"Sell signal: oscillator {prev} -> {osc}");
				}
			}
		}

		_prevOsc = osc;
	}
}
