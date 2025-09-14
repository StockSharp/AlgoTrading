using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Coppock histogram turns to capture trend reversals.
/// </summary>
public class CoppockHistogramStrategy : Strategy
{
	private readonly StrategyParam<int> _roc1Period;
	private readonly StrategyParam<int> _roc2Period;
	private readonly StrategyParam<int> _smoothPeriod;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _sma = null!;
	private decimal? _prev;
	private decimal? _prev2;

	/// <summary>
	/// First rate of change period.
	/// </summary>
	public int Roc1Period
	{
		get => _roc1Period.Value;
		set => _roc1Period.Value = value;
	}

	/// <summary>
	/// Second rate of change period.
	/// </summary>
	public int Roc2Period
	{
		get => _roc2Period.Value;
		set => _roc2Period.Value = value;
	}

	/// <summary>
	/// Moving average smoothing length.
	/// </summary>
	public int SmoothPeriod
	{
		get => _smoothPeriod.Value;
		set => _smoothPeriod.Value = value;
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
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
	/// Initializes a new instance of the <see cref="CoppockHistogramStrategy"/>.
	/// </summary>
	public CoppockHistogramStrategy()
	{
		_roc1Period = Param(nameof(Roc1Period), 14)
			.SetRange(1, 200)
			.SetDisplay("ROC1 Period", "First ROC length", "Parameters")
			.SetCanOptimize(true);

		_roc2Period = Param(nameof(Roc2Period), 11)
			.SetRange(1, 200)
			.SetDisplay("ROC2 Period", "Second ROC length", "Parameters")
			.SetCanOptimize(true);

		_smoothPeriod = Param(nameof(SmoothPeriod), 3)
			.SetRange(1, 50)
			.SetDisplay("Smoothing", "Moving average length", "Parameters")
			.SetCanOptimize(true);

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");
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

		_sma = new SimpleMovingAverage { Length = SmoothPeriod };

		var roc1 = new RateOfChange { Length = Roc1Period };
		var roc2 = new RateOfChange { Length = Roc2Period };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(roc1, roc2, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal roc1Value, decimal roc2Value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var smoothValue = _sma.Process(roc1Value + roc2Value);
		if (!smoothValue.IsFinal)
			return;

		var coppock = smoothValue.ToDecimal();

		if (_prev is decimal prev && _prev2 is decimal prev2)
		{
			if (prev < prev2)
			{
				if (Position < 0)
					BuyMarket(Math.Abs(Position));

				if (coppock > prev && Position <= 0)
					BuyMarket(Volume);
			}
			else if (prev > prev2)
			{
				if (Position > 0)
					SellMarket(Position);

				if (coppock < prev && Position >= 0)
					SellMarket(Volume);
			}
		}

		_prev2 = _prev;
		_prev = coppock;
	}
}
