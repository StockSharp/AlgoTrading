using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Hull Moving Average cross with a two-bar lag.
/// </summary>
public class TriangularHullMovingAverageStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<EntryDirection> _entryMode;

	private decimal? _prev1;
	private decimal? _prev2;
	private decimal? _prev3;

	/// <summary>
	/// Period for Hull Moving Average.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
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
	/// Trade direction mode.
	/// </summary>
	public EntryDirection EntryMode
	{
		get => _entryMode.Value;
		set => _entryMode.Value = value;
	}

	/// <summary>
	/// Initialize the Triangular Hull Moving Average strategy.
	/// </summary>
	public TriangularHullMovingAverageStrategy()
	{
		_length = Param(nameof(Length), 40)
			.SetDisplay("Length", "Period for Hull Moving Average", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20, 80, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_entryMode = Param(nameof(EntryMode), EntryDirection.LongAndShort)
			.SetDisplay("Entry Mode", "Trade direction", "General");
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
		_prev1 = _prev2 = _prev3 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var hma = new HullMovingAverage { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(hma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, hma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal hmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var p1 = _prev1;
		var p2 = _prev2;
		var p3 = _prev3;

		if (p1 is null || p2 is null || p3 is null)
		{
			_prev3 = p2;
			_prev2 = p1;
			_prev1 = hmaValue;
			return;
		}

		var signalUp = p1 < p3 && hmaValue >= p2;
		var signalDn = p1 > p3 && hmaValue <= p2;

		var volume = Volume + Math.Abs(Position);

		switch (EntryMode)
		{
			case EntryDirection.LongAndShort:
				if (signalUp && Position <= 0)
					BuyMarket(volume);
				else if (signalDn && Position >= 0)
					SellMarket(volume);
				break;

			case EntryDirection.OnlyLong:
				if (signalUp && Position <= 0)
					BuyMarket(volume);
				else if (signalDn && Position > 0)
					SellMarket(Position);
				break;

			case EntryDirection.OnlyShort:
				if (signalDn && Position >= 0)
					SellMarket(volume);
				else if (signalUp && Position < 0)
					BuyMarket(Math.Abs(Position));
				break;
		}

		_prev3 = p2;
		_prev2 = p1;
		_prev1 = hmaValue;
	}

	/// <summary>
	/// Trade direction options.
	/// </summary>
	public enum EntryDirection
	{
		/// <summary>
		/// Only long trades.
		/// </summary>
		OnlyLong,

		/// <summary>
		/// Only short trades.
		/// </summary>
		OnlyShort,

		/// <summary>
		/// Long and short trades.
		/// </summary>
		LongAndShort
	}
}
