using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SMA crossover strategy with RSI filter and cooldown.
/// </summary>
public class TradingToolsLibraryStrategy : Strategy
{
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<int> _rsiUpper;
	private readonly StrategyParam<int> _rsiLower;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevShort;
	private decimal _prevLong;
	private int _lastBarIndex = int.MinValue;
	private int _barIndex;

	/// <summary>
	/// Fast SMA length.
	/// </summary>
	public int ShortLength { get => _shortLength.Value; set => _shortLength.Value = value; }

	/// <summary>
	/// Slow SMA length.
	/// </summary>
	public int LongLength { get => _longLength.Value; set => _longLength.Value = value; }

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// Bars between entries.
	/// </summary>
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	/// <summary>
	/// Upper RSI filter.
	/// </summary>
	public int RsiUpper { get => _rsiUpper.Value; set => _rsiUpper.Value = value; }

	/// <summary>
	/// Lower RSI filter.
	/// </summary>
	public int RsiLower { get => _rsiLower.Value; set => _rsiLower.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TradingToolsLibraryStrategy()
	{
		_shortLength = Param(nameof(ShortLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Short SMA", "Fast MA length", "Indicators");

		_longLength = Param(nameof(LongLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Long SMA", "Slow MA length", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI length", "RSI lookback", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 3)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown bars", "Bars between entries", "Strategy");

		_rsiUpper = Param(nameof(RsiUpper), 60)
			.SetDisplay("RSI upper", "Upper RSI filter", "Strategy");

		_rsiLower = Param(nameof(RsiLower), 40)
			.SetDisplay("RSI lower", "Lower RSI filter", "Strategy");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Type of candles", "General");
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
		_prevShort = 0m;
		_prevLong = 0m;
		_lastBarIndex = int.MinValue;
		_barIndex = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var shortMa = new SimpleMovingAverage { Length = ShortLength };
		var longMa = new SimpleMovingAverage { Length = LongLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(shortMa, longMa, rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortMa);
			DrawIndicator(area, longMa);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortVal, decimal longVal, decimal rsiVal)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_barIndex++;

		var crossUp = _prevShort <= _prevLong && shortVal > longVal;
		var crossDown = _prevShort >= _prevLong && shortVal < longVal;
		var canEnter = _barIndex - _lastBarIndex >= CooldownBars;

		if (crossUp && rsiVal < RsiUpper && canEnter)
		{
		if (Position < 0)
		BuyMarket(Math.Abs(Position));
		if (Position == 0)
		{
		BuyMarket(Volume);
		_lastBarIndex = _barIndex;
		}
		}
		else if (crossDown && rsiVal > RsiLower && canEnter)
		{
		if (Position > 0)
		SellMarket(Position);
		if (Position == 0)
		{
		SellMarket(Volume);
		_lastBarIndex = _barIndex;
		}
		}

		_prevShort = shortVal;
		_prevLong = longVal;
	}
}
