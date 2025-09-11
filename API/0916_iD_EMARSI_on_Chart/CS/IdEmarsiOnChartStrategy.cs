using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI with EMA crossover strategy.
/// </summary>
public class IdEmarsiOnChartStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _minTrendBars;
	private readonly StrategyParam<bool> _useFilteredSignals;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _trailingStopPercent;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private ExponentialMovingAverage _ema;

	private decimal _prevRsi;
	private decimal _prevEma;
	private bool _isInitialized;
	private int _bullishBars;
	private int _bearishBars;

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// EMA length.
	/// </summary>
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }

	/// <summary>
	/// Minimum trend duration in bars.
	/// </summary>
	public int MinTrendBars { get => _minTrendBars.Value; set => _minTrendBars.Value = value; }

	/// <summary>
	/// Use trend duration filter.
	/// </summary>
	public bool UseFilteredSignals { get => _useFilteredSignals.Value; set => _useFilteredSignals.Value = value; }

	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

	/// <summary>
	/// Trailing stop percent.
	/// </summary>
	public decimal TrailingStopPercent { get => _trailingStopPercent.Value; set => _trailingStopPercent.Value = value; }

	/// <summary>
	/// Candle type and timeframe.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="IdEmarsiOnChartStrategy"/>.
	/// </summary>
	public IdEmarsiOnChartStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 16)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI length", "General");

		_emaLength = Param(nameof(EmaLength), 42)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA of RSI length", "General");

		_minTrendBars = Param(nameof(MinTrendBars), 20)
			.SetGreaterThanZero()
			.SetDisplay("Min Trend Bars", "Minimum trend duration", "General");

		_useFilteredSignals = Param(nameof(UseFilteredSignals), true)
			.SetDisplay("Use Filtered Signals", "Enable trend duration filter", "General");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percent", "Risk");

		_trailingStopPercent = Param(nameof(TrailingStopPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop %", "Trailing stop percent", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
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

		_rsi = null;
		_ema = null;
		_prevRsi = 0m;
		_prevEma = 0m;
		_isInitialized = false;
		_bullishBars = 0;
		_bearishBars = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(
			new Unit(TakeProfitPercent, UnitTypes.Percent),
			new Unit(TrailingStopPercent, UnitTypes.Percent),
			isStopTrailing: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var rsiValue = _rsi.Process(candle).ToDecimal();
		var emaValue = _ema.Process(rsiValue, candle.OpenTime, true).ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_isInitialized)
		{
			_prevRsi = rsiValue;
			_prevEma = emaValue;
			_isInitialized = true;
			return;
		}

		if (rsiValue > emaValue)
		{
			_bullishBars++;
			_bearishBars = 0;
		}
		else if (rsiValue < emaValue)
		{
			_bearishBars++;
			_bullishBars = 0;
		}

		var crossUp = _prevRsi <= _prevEma && rsiValue > emaValue;
		var crossDown = _prevRsi >= _prevEma && rsiValue < emaValue;

		var validBull = crossUp && (!UseFilteredSignals || _bearishBars >= MinTrendBars);
		var validBear = crossDown && (!UseFilteredSignals || _bullishBars >= MinTrendBars);

		if (validBull && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (validBear && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevRsi = rsiValue;
		_prevEma = emaValue;
	}
}
