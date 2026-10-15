using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Ichimoku oscillator smoothed by EMA.
/// Opens long when oscillator turns up, short when it turns down.
/// </summary>
public class IchimokuOscillatorStrategy : Strategy
{
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanBPeriod;
	private readonly StrategyParam<int> _smoothingPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;

	private ExponentialMovingAverage _ema;
	private decimal? _prevValue;
	private decimal? _prevPrevValue;

	public int TenkanPeriod { get => _tenkanPeriod.Value; set => _tenkanPeriod.Value = value; }
	public int KijunPeriod { get => _kijunPeriod.Value; set => _kijunPeriod.Value = value; }
	public int SenkouSpanBPeriod { get => _senkouSpanBPeriod.Value; set => _senkouSpanBPeriod.Value = value; }
	public int SmoothingPeriod { get => _smoothingPeriod.Value; set => _smoothingPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

	public IchimokuOscillatorStrategy()
	{
		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Tenkan Period", "Period for Tenkan-sen line", "Ichimoku");

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Kijun Period", "Period for Kijun-sen line", "Ichimoku");

		_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
			.SetGreaterThanZero()
			.SetDisplay("Senkou Span B Period", "Period for Senkou Span B", "Ichimoku");

		_smoothingPeriod = Param(nameof(SmoothingPeriod), 7)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing Period", "Period for smoothing EMA", "Oscillator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculation", "Main");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetDisplay("Stop Loss %", "Stop loss in percent", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
			.SetDisplay("Take Profit %", "Take profit in percent", "Risk");
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
		_ema = default;
		_prevValue = null;
		_prevPrevValue = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ichimoku = new Ichimoku();
		ichimoku.Tenkan.Length = TenkanPeriod;
		ichimoku.Kijun.Length = KijunPeriod;
		ichimoku.SenkouB.Length = SenkouSpanBPeriod;

		_ema = new ExponentialMovingAverage { Length = SmoothingPeriod };

		Indicators.Add(_ema);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ichimoku, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!ichimokuValue.IsFormed)
			return;

		var ich = (IchimokuValue)ichimokuValue;

		if (ich.Chinkou is not decimal chikou ||
			ich.SenkouB is not decimal spanB ||
			ich.Tenkan is not decimal tenkan ||
			ich.Kijun is not decimal kijun)
			return;

		var osc = (chikou - spanB) - (tenkan - kijun);
		var emaVal = _ema.Process(osc, candle.OpenTime, true);
		if (!emaVal.IsFormed)
			return;

		var current = emaVal.ToDecimal();

		if (_prevValue is decimal prev && _prevPrevValue is decimal prevPrev)
		{
			var rising = prev > prevPrev;
			var falling = prev < prevPrev;

			if (rising && current >= prev && Position <= 0)
			{
				if (Position < 0) BuyMarket();
				BuyMarket();
			}
			else if (falling && current <= prev && Position >= 0)
			{
				if (Position > 0) SellMarket();
				SellMarket();
			}
		}

		_prevPrevValue = _prevValue;
		_prevValue = current;
	}
}
