using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// 200 SMA Buffer Strategy - buys when price is above SMA by a percentage and exits when price falls below by another percentage.
/// </summary>
public class SmaBufferStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<decimal> _entryPercent;
	private readonly StrategyParam<decimal> _exitPercent;

	private SimpleMovingAverage _sma;

	private static readonly DateTimeOffset _startTime = new(2001, 1, 1, 0, 0, 0, TimeSpan.Zero);

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	public decimal EntryPercent
	{
		get => _entryPercent.Value;
		set => _entryPercent.Value = value;
	}

	public decimal ExitPercent
	{
		get => _exitPercent.Value;
		set => _exitPercent.Value = value;
	}

	public SmaBufferStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_smaLength = Param(nameof(SmaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Period of the moving average", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(50, 300, 25);

		_entryPercent = Param(nameof(EntryPercent), 5m)
			.SetRange(0.1m, 100m)
			.SetDisplay("Entry %", "Percent above SMA to enter", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 1m);

		_exitPercent = Param(nameof(ExitPercent), 3m)
			.SetRange(0.1m, 100m)
			.SetDisplay("Exit %", "Percent below SMA to exit", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 1m);
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

		_sma = new SimpleMovingAverage { Length = SmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_sma, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_sma.IsFormed)
			return;

		if (candle.OpenTime < _startTime)
			return;

		var upperThreshold = smaValue * (1m + EntryPercent / 100m);
		var lowerThreshold = smaValue * (1m - ExitPercent / 100m);

		if (Position <= 0 && candle.ClosePrice > upperThreshold)
			RegisterBuy();
		else if (Position > 0 && candle.ClosePrice < lowerThreshold)
			RegisterSell();
	}
}
