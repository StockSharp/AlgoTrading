using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades on changes in the slope of a smoothed trend line.
/// </summary>
public class SlopeDirectionLineStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _allowLong;
	private readonly StrategyParam<bool> _allowShort;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _trend = null!;
	private decimal? _prevTrend;
	private decimal? _prevSlope;
	private int _cooldownRemaining;

	/// <summary>
	/// Candle type for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Length of the trend calculation.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Take-profit percentage from entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool AllowLong
	{
		get => _allowLong.Value;
		set => _allowLong.Value = value;
	}

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool AllowShort
	{
		get => _allowShort.Value;
		set => _allowShort.Value = value;
	}

	/// <summary>
	/// Number of completed candles to wait after a position change.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SlopeDirectionLineStrategy"/> class.
	/// </summary>
	public SlopeDirectionLineStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candles", "General");

		_length = Param(nameof(Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("Trend Length", "Number of bars in the trend line", "Indicators")
			.SetOptimize(10, 30, 5);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk")
			.SetRange(1m, 5m);

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
			.SetRange(0.5m, 5m);

		_allowLong = Param(nameof(AllowLong), true)
			.SetDisplay("Allow Long", "Enable long entries", "Trading");

		_allowShort = Param(nameof(AllowShort), true)
			.SetDisplay("Allow Short", "Enable short entries", "Trading");

		_cooldownBars = Param(nameof(CooldownBars), 6)
			.SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading");
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

		_trend = null!;
		_prevTrend = null;
		_prevSlope = null;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_trend = new ExponentialMovingAverage { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_trend, ProcessCandle)
			.Start();

		StartProtection(new Unit(TakeProfitPercent, UnitTypes.Percent), new Unit(StopLossPercent, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _trend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal trendValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		if (_prevTrend is not decimal prevTrend)
		{
			_prevTrend = trendValue;
			return;
		}

		var currentSlope = trendValue - prevTrend;
		if (_prevSlope is decimal prevSlope)
		{
			if (currentSlope > 0m && prevSlope <= 0m && _cooldownRemaining == 0)
			{
				if (Position < 0)
					BuyMarket();
				if (Position <= 0 && AllowLong)
				{
					BuyMarket();
					_cooldownRemaining = CooldownBars;
				}
			}
			else if (currentSlope < 0m && prevSlope >= 0m && _cooldownRemaining == 0)
			{
				if (Position > 0)
					SellMarket();
				if (Position >= 0 && AllowShort)
				{
					SellMarket();
					_cooldownRemaining = CooldownBars;
				}
			}
		}

		_prevTrend = trendValue;
		_prevSlope = currentSlope;
	}
}
