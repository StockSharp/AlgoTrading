using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades when price crosses a lag reduction filter over EMA.
/// </summary>
public class TimeSeriesLagReductionFilterStrategy : Strategy
{
	private readonly StrategyParam<decimal> _lagReduction;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevEma;
	private bool _isFirst;
	private bool _prevAboveFilter;

	/// <summary>
	/// Lag reduction factor.
	/// </summary>
	public decimal LagReduction
	{
		get => _lagReduction.Value;
		set => _lagReduction.Value = value;
	}

	/// <summary>
	/// EMA length.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
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
	/// Initialize strategy parameters.
	/// </summary>
	public TimeSeriesLagReductionFilterStrategy()
	{
		_lagReduction = Param(nameof(LagReduction), 20m)
			.SetDisplay("Lag Reduction", "Smoothing factor", "General")
			.SetCanOptimize(true)
			.SetOptimize(10m, 30m, 5m);

		_emaLength = Param(nameof(EmaLength), 100)
			.SetDisplay("EMA Length", "Period for EMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(50, 150, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_isFirst = true;
		_prevAboveFilter = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ema = new ExponentialMovingAverage
		{
			Length = EmaLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_isFirst)
		{
			_prevEma = ema;
			_isFirst = false;
			return;
		}

		if (_prevEma == 0)
			return;

		var ratio = ema / _prevEma;
		if (ratio <= 0)
		{
			_prevEma = ema;
			return;
		}

		var lagFilter = (decimal)Math.Exp((double)(LagReduction * Math.Log((double)ratio))) * ema;

		var isAbove = candle.ClosePrice > lagFilter;
		var crossedAbove = isAbove && !_prevAboveFilter;
		var crossedBelow = !isAbove && _prevAboveFilter;

		if (crossedAbove && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (crossedBelow && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		_prevAboveFilter = isAbove;
		_prevEma = ema;
	}
}