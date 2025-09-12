namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Displays higher time frame candles with volume information.
/// This strategy is for visualization only and does not trade.
/// </summary>
public class MultiTimeFrameCandlesWithVolumeInfo3DStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _candleCount;

	private readonly Queue<ICandleMessage> _candles = new();

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of candles to keep.
	/// </summary>
	public int CandleCount
	{
		get => _candleCount.Value;
		set => _candleCount.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="MultiTimeFrameCandlesWithVolumeInfo3DStrategy"/>.
	/// </summary>
	public MultiTimeFrameCandlesWithVolumeInfo3DStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Higher time frame", "General");

		_candleCount = Param(nameof(CandleCount), 8)
			.SetGreaterThanZero()
			.SetDisplay("Candle Count", "Number of HTF candles", "General")
			.SetCanOptimize(true)
			.SetOptimize(4, 10, 2);
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
		_candles.Clear();
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
			DrawCandles(area, subscription);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_candles.Enqueue(candle);
		if (_candles.Count > CandleCount)
			_candles.Dequeue();

		var (v, unit) = GetVolume(candle.TotalVolume);
		var tf = (TimeSpan)CandleType.Arg;
		var timeToClose = candle.OpenTime + tf - CurrentTime;

		AddInfo($"HTF Candle O:{candle.OpenPrice} H:{candle.HighPrice} L:{candle.LowPrice} C:{candle.ClosePrice} V:{v}{unit}");
		AddInfo($"Time to close: {TimeToString(timeToClose)}");
	}

	private static (decimal value, string unit) GetVolume(decimal volume)
	{
		if (volume >= 1_000_000_000m)
			return (volume / 1_000_000_000m, "B");

		if (volume >= 1_000_000m)
			return (volume / 1_000_000m, "M");

		if (volume >= 1_000m)
			return (volume / 1_000m, "K");

		return (volume, string.Empty);
	}

	private static string TimeToString(TimeSpan span)
	{
		var d = (int)span.TotalDays;
		var h = span.Hours;
		var m = span.Minutes;
		var s = span.Seconds;

		var result = d > 0 ? $"{d}D " : string.Empty;
		result += h > 0 ? $"{h}H " : string.Empty;
		result += m > 0 ? $"{m}m " : string.Empty;

		if (d == 0)
			result += s > 0 ? $"{s}s " : string.Empty;

		return result.TrimEnd();
	}
}

