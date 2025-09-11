using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Highlights new highs and lows by drawing tails.
/// </summary>
public class NunchucksStrategy : Strategy
{
	private readonly StrategyParam<bool> _showZeroTails;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHigh;
	private decimal _prevLow;
	private bool _isFirst;

	public bool ShowZeroTails
	{
		get => _showZeroTails.Value;
		set => _showZeroTails.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public NunchucksStrategy()
	{
		_showZeroTails = Param(nameof(ShowZeroTails), false)
			.SetDisplay("Show Zero Tails", "Display tails even if their range is zero", "Visualization");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_prevHigh = 0m;
		_prevLow = 0m;
		_isFirst = true;
	}

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

		if (_isFirst)
		{
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_isFirst = false;
			return;
		}

		var highVisible = candle.HighPrice > _prevHigh;
		var lowVisible = candle.LowPrice < _prevLow;

		if (ShowZeroTails || highVisible)
			LogInfo($"Higher tail from {Math.Min(candle.HighPrice, _prevHigh)} to {candle.HighPrice}");

		if (ShowZeroTails || lowVisible)
			LogInfo($"Lower tail from {Math.Max(candle.LowPrice, _prevLow)} to {candle.LowPrice}");

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}
}
