using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy applying Parabolic SAR to RSI. Enters when SAR flips relative to RSI with optional filter.
/// </summary>
public class ParabolicRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _sarStart;
	private readonly StrategyParam<decimal> _sarIncrement;
	private readonly StrategyParam<decimal> _sarMax;
private readonly StrategyParam<Sides?> _direction;
	private readonly StrategyParam<bool> _useFilter;
	private readonly StrategyParam<decimal> _longRsiMin;
	private readonly StrategyParam<decimal> _shortRsiMax;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private ParabolicSar _psar;
	private bool? _isBelow;

	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal SarStart { get => _sarStart.Value; set => _sarStart.Value = value; }
	public decimal SarIncrement { get => _sarIncrement.Value; set => _sarIncrement.Value = value; }
	public decimal SarMax { get => _sarMax.Value; set => _sarMax.Value = value; }
public Sides? Direction { get => _direction.Value; set => _direction.Value = value; }
	public bool UseFilter { get => _useFilter.Value; set => _useFilter.Value = value; }
	public decimal LongRsiMin { get => _longRsiMin.Value; set => _longRsiMin.Value = value; }
	public decimal ShortRsiMax { get => _shortRsiMax.Value; set => _shortRsiMax.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ParabolicRsiStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI indicator period", "RSI");

		_sarStart = Param(nameof(SarStart), 0.02m)
			.SetNotNegative()
			.SetDisplay("SAR Start", "Initial acceleration factor", "SAR");

		_sarIncrement = Param(nameof(SarIncrement), 0.02m)
			.SetNotNegative()
			.SetDisplay("SAR Increment", "Acceleration increment", "SAR");

		_sarMax = Param(nameof(SarMax), 0.2m)
			.SetNotNegative()
			.SetDisplay("SAR Max", "Maximum acceleration", "SAR");

_direction = Param(nameof(Direction), (Sides?)null)
.SetDisplay("Trade Direction", "Allowed trade direction", "Strategy");

		_useFilter = Param(nameof(UseFilter), false)
			.SetDisplay("Use Filter", "Enable RSI filter", "Filter");

		_longRsiMin = Param(nameof(LongRsiMin), 50m)
			.SetDisplay("Long RSI Min", "Long only if RSI ≥", "Filter")
			.SetNotNegative();

		_shortRsiMax = Param(nameof(ShortRsiMax), 50m)
			.SetDisplay("Short RSI Max", "Short only if RSI ≤", "Filter")
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
		_isBelow = null;
		_rsi?.Reset();
		_psar?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_psar = new ParabolicSar
		{
			Acceleration = SarStart,
			AccelerationStep = SarIncrement,
			AccelerationMax = SarMax
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_rsi, ProcessCandle).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawIndicator(area, _psar);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fake = new CandleMessage
		{
			SecurityId = candle.SecurityId,
			OpenTime = candle.OpenTime,
			HighPrice = rsiValue + 1m,
			LowPrice = rsiValue - 1m,
			OpenPrice = rsiValue,
			ClosePrice = rsiValue,
			State = CandleStates.Finished
		};

		var sarValue = _psar.Process(new CandleIndicatorValue(_psar, fake));
		if (!sarValue.IsFinal)
			return;

		var sar = sarValue.GetValue<decimal>();
		var isBelow = sar < rsiValue;

		var sigLong = _isBelow is bool prev1 && !prev1 && isBelow;
		var sigShort = _isBelow is bool prev2 && prev2 && !isBelow;

if (sigLong && (!UseFilter || rsiValue >= LongRsiMin))
{
if (Direction != Sides.Sell)
{
if (Position < 0)
BuyMarket(Math.Abs(Position));
if (Position <= 0)
BuyMarket(Volume);
}
else if (Position < 0)
{
BuyMarket(Math.Abs(Position));
}
}
else if (sigShort && (!UseFilter || rsiValue <= ShortRsiMax))
{
if (Direction != Sides.Buy)
{
if (Position > 0)
SellMarket(Math.Abs(Position));
if (Position >= 0)
SellMarket(Volume);
}
else if (Position > 0)
{
SellMarket(Math.Abs(Position));
}
}

		_isBelow = isBelow;
	}
}
