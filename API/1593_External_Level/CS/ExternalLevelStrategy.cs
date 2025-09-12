namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Draws horizontal levels based on external signals.
/// Creates a resistance line when the previous source value equals 1
/// and a support line when it equals -1.
/// </summary>
public class ExternalLevelStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;

	private ICandleMessage? _prevCandle;
	private decimal _prevSource;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ExternalLevelStrategy"/>.
	/// </summary>
	public ExternalLevelStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Source candle timeframe", "General");
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
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevCandle != null)
		{
			if (_prevSource == 1m)
			{
				DrawLine(_prevCandle.OpenTime, _prevCandle.HighPrice, candle.OpenTime, _prevCandle.HighPrice);
			}
			else if (_prevSource == -1m)
			{
				DrawLine(_prevCandle.OpenTime, _prevCandle.LowPrice, candle.OpenTime, _prevCandle.LowPrice);
			}
		}

		_prevSource = candle.ClosePrice;
		_prevCandle = candle;
	}
}
