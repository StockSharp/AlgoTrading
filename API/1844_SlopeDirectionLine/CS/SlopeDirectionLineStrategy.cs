namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that trades on changes in the slope of a linear regression line.
/// </summary>
public class SlopeDirectionLineStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _allowLong;
	private readonly StrategyParam<bool> _allowShort;

	private LinearRegression _reg;
	private decimal? _prevSlope;

	/// <summary>
	/// Candle type for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Length of the regression calculation.
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
	/// Initializes a new instance of the <see cref="SlopeDirectionLineStrategy"/> class.
	/// </summary>
	public SlopeDirectionLineStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candles", "General");

		_length = Param(nameof(Length), 12)
			.SetGreaterThanZero()
			.SetDisplay("Regression Length", "Number of bars in regression", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 5);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk")
			.SetCanOptimize(true)
			.SetRange(1m, 5m);

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
			.SetCanOptimize(true)
			.SetRange(0.5m, 5m);

		_allowLong = Param(nameof(AllowLong), true)
			.SetDisplay("Allow Long", "Enable long entries", "Trading");

		_allowShort = Param(nameof(AllowShort), true)
			.SetDisplay("Allow Short", "Enable short entries", "Trading");
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

		_reg = default;
		_prevSlope = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_reg = new LinearRegression { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_reg, ProcessCandle)
			.Start();

		StartProtection(new Unit(TakeProfitPercent, UnitTypes.Percent), new Unit(StopLossPercent, UnitTypes.Percent));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _reg);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue regValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!regValue.IsFinal)
			return;

		var slope = ((LinearRegressionValue)regValue).LinearRegSlope;
		if (slope is null)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var currentSlope = (decimal)slope;

		if (_prevSlope is decimal prev)
		{
			if (currentSlope > 0 && prev <= 0)
			{
				if (Position < 0)
					ClosePosition();
				else if (Position == 0 && AllowLong)
					BuyMarket();
			}
			else if (currentSlope < 0 && prev >= 0)
			{
				if (Position > 0)
					ClosePosition();
				else if (Position == 0 && AllowShort)
					SellMarket();
			}
		}

		_prevSlope = currentSlope;
	}
}

