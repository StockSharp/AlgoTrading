using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining a Gann-style trend filter with Laplace-smoothed volume spread analysis.
/// </summary>
public class GannLaplaceSmoothedHybridVsaStrategy : Strategy
{
	private readonly StrategyParam<int> _trendPeriod;
	private readonly StrategyParam<int> _vsaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _trendMa;
	private ExponentialMovingAverage _vsaEma;

	/// <summary>
	/// Trend moving average period.
	/// </summary>
	public int TrendPeriod
	{
		get => _trendPeriod.Value;
		set => _trendPeriod.Value = value;
	}

	/// <summary>
	/// EMA period for volume spread smoothing.
	/// </summary>
	public int VsaPeriod
	{
		get => _vsaPeriod.Value;
		set => _vsaPeriod.Value = value;
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
	/// Initialize the strategy.
	/// </summary>
	public GannLaplaceSmoothedHybridVsaStrategy()
	{
		_trendPeriod = Param(nameof(TrendPeriod), 20)
			.SetDisplay("Trend Period", "Period for trend moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_vsaPeriod = Param(nameof(VsaPeriod), 14)
			.SetDisplay("VSA Smoothing", "EMA period for volume spread", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		_trendMa = default;
		_vsaEma = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		_trendMa = new SimpleMovingAverage { Length = TrendPeriod };
		_vsaEma = new ExponentialMovingAverage { Length = VsaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_trendMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _trendMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal trendValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var range = candle.HighPrice - candle.LowPrice;
		if (range == 0)
			return;

		var spread = candle.ClosePrice - candle.OpenPrice;
		var vsa = spread / range * candle.TotalVolume;

		var smoothed = _vsaEma.Process(vsa, candle.OpenTime, true).ToDecimal();

		if (smoothed > 0 && candle.ClosePrice > trendValue && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (smoothed < 0 && candle.ClosePrice < trendValue && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
		else if (Position > 0 && smoothed < 0)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && smoothed > 0)
		{
			BuyMarket(Math.Abs(Position));
		}
	}
}
