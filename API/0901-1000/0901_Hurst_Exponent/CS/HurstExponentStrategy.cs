using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hurst exponent strategy.
/// The rescaled-range Hurst exponent of the close-to-close returns over HurstPeriod candles is smoothed with an EMA of SmoothLength.
/// A smoothed value above Threshold marks a persistent (trending) regime and holds a long, a value below it holds a short, so each
/// threshold cross exits the current side and reverses. A percent stop loss limits the loss.
/// </summary>
public class HurstExponentStrategy : Strategy
{
	private readonly StrategyParam<int> _hurstPeriod;
	private readonly StrategyParam<int> _smoothLength;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private HurstExponent _hurst;
	private ExponentialMovingAverage _smooth;
	private decimal? _prevClose;

	/// <summary>
	/// Number of returns used by the Hurst exponent.
	/// </summary>
	public int HurstPeriod
	{
		get => _hurstPeriod.Value;
		set => _hurstPeriod.Value = value;
	}

	/// <summary>
	/// EMA length that smooths the Hurst exponent.
	/// </summary>
	public int SmoothLength
	{
		get => _smoothLength.Value;
		set => _smoothLength.Value = value;
	}

	/// <summary>
	/// Regime threshold for the smoothed Hurst exponent.
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
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
	/// Constructor.
	/// </summary>
	public HurstExponentStrategy()
	{
		_hurstPeriod = Param(nameof(HurstPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("Hurst Period", "Number of returns used by the Hurst exponent", "Indicators");

		_smoothLength = Param(nameof(SmoothLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Smooth Length", "EMA length that smooths the Hurst exponent", "Indicators");

		_threshold = Param(nameof(Threshold), 0.5m)
			.SetDisplay("Threshold", "Regime threshold for the smoothed Hurst exponent", "Signals");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = null;
		_hurst = new HurstExponent { Length = HurstPeriod };
		_smooth = new ExponentialMovingAverage { Length = SmoothLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, _smooth);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var prevClose = _prevClose;
		_prevClose = close;

		if (prevClose is not decimal prev || prev == 0)
			return;

		// The exponent is measured on returns: on raw price levels the rescaled range is always near 1.
		var ret = (close - prev) / prev;
		var hurstValue = _hurst.Process(new DecimalIndicatorValue(_hurst, ret, candle.OpenTime) { IsFinal = true });
		if (!_hurst.IsFormed || hurstValue.IsEmpty)
			return;

		var smoothValue = _smooth.Process(new DecimalIndicatorValue(_smooth, hurstValue.GetValue<decimal>(), candle.OpenTime) { IsFinal = true });
		if (!_smooth.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var smoothed = smoothValue.GetValue<decimal>();

		if (smoothed > Threshold && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (smoothed < Threshold && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
