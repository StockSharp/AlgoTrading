using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SPY/TLT strategy based on TLT SMA crossover.
/// Buys when TLT closes above its SMA and exits on opposite cross.
/// </summary>
public class SpyTltStrategy : Strategy
{
	private readonly StrategyParam<DateTimeOffset> _startTime;
	private readonly StrategyParam<DateTimeOffset> _endTime;
	private readonly StrategyParam<string> _tltSymbol;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<DataType> _candleType;

	private Security _tltSecurity;

	public DateTimeOffset StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	public DateTimeOffset EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
	}

	public string TltSymbol
	{
		get => _tltSymbol.Value;
		set => _tltSymbol.Value = value;
	}

	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public SpyTltStrategy()
	{
		_startTime = Param(nameof(StartTime), new DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Time", "Beginning of trading window", "Time");

		_endTime = Param(nameof(EndTime), new DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("End Time", "End of trading window", "Time");

		_tltSymbol = Param(nameof(TltSymbol), "TLT")
			.SetDisplay("TLT Symbol", "Ticker for TLT instrument", "Security");

		_smaLength = Param(nameof(SmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Period for SMA indicator", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, null);
		if (_tltSecurity != null)
			yield return (_tltSecurity, CandleType);
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tltSecurity = SecurityProvider.LookupById(TltSymbol);
		if (_tltSecurity == null)
			throw new InvalidOperationException($"Security '{TltSymbol}' not found.");

		var sma = new SMA { Length = SmaLength };

		var subscription = SubscribeCandles(CandleType, true, _tltSecurity);

		var wasAbove = false;
		var initialized = false;

		subscription.Bind(sma, (candle, smaValue) =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (!sma.IsFormed)
				return;

			var candleTime = candle.OpenTime;
			var inWindow = candleTime >= StartTime && candleTime <= EndTime;

			var close = candle.ClosePrice;
			var isAbove = close > smaValue;
			var crossOver = !wasAbove && isAbove;
			var crossUnder = wasAbove && !isAbove;

			if (!initialized)
			{
				wasAbove = isAbove;
				initialized = true;
				return;
			}

			if (crossOver && inWindow && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));

			if (crossUnder && Position > 0)
				SellMarket(Math.Abs(Position));

			wasAbove = isAbove;
		})
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}
}
