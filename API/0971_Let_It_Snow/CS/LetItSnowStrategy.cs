using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Decorative strategy that plots animated snowflakes on the chart.
/// </summary>
public class LetItSnowStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private IChartArea? _area;

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="LetItSnowStrategy"/>.
	/// </summary>
	public LetItSnowStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		_area = CreateChartArea();
		if (_area != null)
			DrawCandles(_area, subscription);
	}
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var barSec = (int)((DateTimeOffset.UtcNow - candle.OpenTime).TotalSeconds % 112);
		var snowTime = candle.OpenTime.AddSeconds(barSec);
		var price = candle.LowPrice;

		if (_area != null)
			DrawText(_area, snowTime, price, "❄");
	}
}
