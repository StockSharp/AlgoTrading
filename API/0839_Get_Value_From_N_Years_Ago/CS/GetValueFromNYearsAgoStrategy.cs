using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Retrieves time components from candles N years ago.
/// </summary>
public class GetValueFromNYearsAgoStrategy : Strategy
{
	private readonly StrategyParam<int> _yearsBack;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<DateTimeOffset> _times = new();

	/// <summary>
	/// Number of years to look back.
	/// </summary>
	public int YearsBack
	{
		get => _yearsBack.Value;
		set => _yearsBack.Value = value;
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
	/// Initialize strategy parameters.
	/// </summary>
	public GetValueFromNYearsAgoStrategy()
	{
		_yearsBack = Param(nameof(YearsBack), 1)
		.SetGreaterThanZero()
		.SetDisplay("Years Back", "Number of years to look back", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_times.Add(candle.OpenTime);

		var requiredTime = candle.OpenTime.AddYears(-YearsBack);
		var idx = BinarySearch(requiredTime);
		if (idx < 0)
		return;

		var t = _times[idx];
		this.AddInfoLog($"Year={t.Year}, Month={t.Month}, Day={t.Day}, Hour={t.Hour}, Minute={t.Minute}");
	}

	private int BinarySearch(DateTimeOffset time)
	{
		var left = 0;
		var right = _times.Count - 1;

		while (left <= right)
		{
			var mid = (left + right) / 2;
			var midTime = _times[mid];

			if (midTime == time)
			return mid;

			if (midTime < time)
			left = mid + 1;
			else
			right = mid - 1;
		}

		return right >= 0 ? right : -1;
	}
}
