using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI strategy that enters long after oversold crossbacks with optional short trades.
/// </summary>
public class RsiLongOnlyWithConfirmedCrossbacksStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _oversold;
	private readonly StrategyParam<decimal> _longExitLevel;
	private readonly StrategyParam<decimal> _shortEntryLevel;
	private readonly StrategyParam<decimal> _shortExitLevel;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private bool _oversoldTouched;
	private decimal? _prevRsi;

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI level to confirm oversold.
	/// </summary>
	public decimal Oversold
	{
		get => _oversold.Value;
		set => _oversold.Value = value;
	}

	/// <summary>
	/// RSI level to exit long positions.
	/// </summary>
	public decimal LongExitLevel
	{
		get => _longExitLevel.Value;
		set => _longExitLevel.Value = value;
	}

	/// <summary>
	/// RSI level to enter short positions.
	/// </summary>
	public decimal ShortEntryLevel
	{
		get => _shortEntryLevel.Value;
		set => _shortEntryLevel.Value = value;
	}

	/// <summary>
	/// RSI level to exit short positions.
	/// </summary>
	public decimal ShortExitLevel
	{
		get => _shortExitLevel.Value;
		set => _shortExitLevel.Value = value;
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
	/// Initializes a new instance of <see cref="RsiLongOnlyWithConfirmedCrossbacksStrategy"/>.
	/// </summary>
	public RsiLongOnlyWithConfirmedCrossbacksStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "RSI indicator period", "RSI")
			.SetGreaterThanZero();

		_oversold = Param(nameof(Oversold), 44m)
			.SetDisplay("Long Entry RSI Threshold", "RSI level to confirm oversold", "RSI")
			.SetNotNegative();

		_longExitLevel = Param(nameof(LongExitLevel), 70m)
			.SetDisplay("Long Exit RSI Threshold", "RSI level to exit long", "RSI")
			.SetNotNegative();

		_shortEntryLevel = Param(nameof(ShortEntryLevel), 100m)
			.SetDisplay("Short Entry RSI Threshold", "RSI level to enter short", "RSI")
			.SetNotNegative();

		_shortExitLevel = Param(nameof(ShortExitLevel), 0m)
			.SetDisplay("Short Exit RSI Threshold", "RSI level to exit short", "RSI")
			.SetNotNegative();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "General");
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
		_rsi?.Reset();
		_oversoldTouched = false;
		_prevRsi = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_rsi, ProcessCandle).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (rsiValue < Oversold)
			_oversoldTouched = true;

		var prev = _prevRsi;
		_prevRsi = rsiValue;

		if (prev is decimal p && _oversoldTouched && p < Oversold && rsiValue >= Oversold && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_oversoldTouched = false;
		}
		else if (prev is decimal p2 && p2 < LongExitLevel && rsiValue >= LongExitLevel && Position > 0)
		{
			SellMarket(Position);
		}
		else if (prev is decimal p3 && p3 < ShortEntryLevel && rsiValue >= ShortEntryLevel && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}
		else if (rsiValue < ShortExitLevel && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}
	}
}
