using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining smoothed EMA crossover and smoothed directional index.
/// </summary>
public class StrategySemaSdiWebhookStrategy : Strategy
{
	private readonly StrategyParam<bool> _useEma;
	private readonly StrategyParam<bool> _useSdi;
	private readonly StrategyParam<bool> _useSmooth;
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _smoothLength;
	private readonly StrategyParam<int> _diLength;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<bool> _useTrailing;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _trailingPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private ExponentialMovingAverage _fastSmooth;
	private ExponentialMovingAverage _slowSmooth;
	private AverageDirectionalIndex _dmi;

	private decimal _maxProfit;
	private bool _trailingActive;

	public bool UseEma { get => _useEma.Value; set => _useEma.Value = value; }
	public bool UseSdi { get => _useSdi.Value; set => _useSdi.Value = value; }
	public bool UseSmooth { get => _useSmooth.Value; set => _useSmooth.Value = value; }
	public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }
	public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }
	public int SmoothLength { get => _smoothLength.Value; set => _smoothLength.Value = value; }
	public int DiLength { get => _diLength.Value; set => _diLength.Value = value; }
	public bool UseTakeProfit { get => _useTakeProfit.Value; set => _useTakeProfit.Value = value; }
	public bool UseStopLoss { get => _useStopLoss.Value; set => _useStopLoss.Value = value; }
	public bool UseTrailing { get => _useTrailing.Value; set => _useTrailing.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public decimal TrailingPercent { get => _trailingPercent.Value; set => _trailingPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public DateTimeOffset StartDate { get => _startDate.Value; set => _startDate.Value = value; }
	public DateTimeOffset EndDate { get => _endDate.Value; set => _endDate.Value = value; }

	public StrategySemaSdiWebhookStrategy()
	{
		_useEma = Param(nameof(UseEma), true)
			.SetDisplay("EMA", "Enable EMA condition", "Inputs");
		_useSdi = Param(nameof(UseSdi), true)
			.SetDisplay("SDI", "Enable SDI condition", "Inputs");
		_useSmooth = Param(nameof(UseSmooth), true)
			.SetDisplay("Smooth", "Smooth EMAs", "Inputs");
		_fastEmaLength = Param(nameof(FastEmaLength), 58)
			.SetDisplay("Fast EMA", "Fast EMA length", "Inputs");
		_slowEmaLength = Param(nameof(SlowEmaLength), 70)
			.SetDisplay("Slow EMA", "Slow EMA length", "Inputs");
		_smoothLength = Param(nameof(SmoothLength), 3)
			.SetDisplay("Smooth", "Smoothing length", "Inputs");
		_diLength = Param(nameof(DiLength), 1)
			.SetDisplay("DI Length", "Directional index length", "SDI");
		_useTakeProfit = Param(nameof(UseTakeProfit), false)
			.SetDisplay("TP", "Use take profit", "Take Profit/Stop Loss");
		_useStopLoss = Param(nameof(UseStopLoss), true)
			.SetDisplay("SL", "Use stop loss", "Take Profit/Stop Loss");
		_useTrailing = Param(nameof(UseTrailing), true)
			.SetDisplay("Trailing", "Enable trailing stop", "Take Profit/Stop Loss");
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 25m)
			.SetDisplay("Take Profit %", "Take profit percent", "Take Profit/Stop Loss");
		_stopLossPercent = Param(nameof(StopLossPercent), 4.8m)
			.SetDisplay("Stop Loss %", "Stop loss percent", "Take Profit/Stop Loss");
		_trailingPercent = Param(nameof(TrailingPercent), 1.9m)
			.SetDisplay("Trailing %", "Trailing percent", "Take Profit/Stop Loss");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");
		_startDate = Param(nameof(StartDate), new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc))
			.SetDisplay("Start Date", "Trading start date", "General");
		_endDate = Param(nameof(EndDate), new DateTime(2124, 1, 1, 0, 0, 0, DateTimeKind.Utc))
			.SetDisplay("End Date", "Trading end date", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_fastEma?.Reset();
		_slowEma?.Reset();
		_fastSmooth?.Reset();
		_slowSmooth?.Reset();
		_dmi?.Reset();
		_maxProfit = 0m;
		_trailingActive = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		_slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		_fastSmooth = new ExponentialMovingAverage { Length = SmoothLength };
		_slowSmooth = new ExponentialMovingAverage { Length = SmoothLength };
		_dmi = new AverageDirectionalIndex { Length = DiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_fastEma, _slowEma, _dmi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _slowEma);
			DrawOwnTrades(area);
		}

		StartProtection(
			takeProfit: UseTakeProfit ? new Unit(TakeProfitPercent, UnitTypes.Percent) : null,
			stopLoss: UseStopLoss ? new Unit(StopLossPercent, UnitTypes.Percent) : null,
			isStopTrailing: false,
			useMarketOrders: true);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue dmiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var emaFast = fastValue.ToDecimal();
		var emaSlow = slowValue.ToDecimal();

		var fast = UseSmooth ? _fastSmooth.Process(emaFast).GetValue<decimal>() : emaFast;
		var slow = UseSmooth ? _slowSmooth.Process(emaSlow).GetValue<decimal>() : emaSlow;

		var typed = (AverageDirectionalIndexValue)dmiValue;
		if (typed.Dx.Plus is not decimal plusDi || typed.Dx.Minus is not decimal minusDi)
			return;

		var withinTime = candle.OpenTime >= StartDate && candle.OpenTime <= EndDate;

		var longCondition = (!UseSdi || plusDi > minusDi) && (!UseEma || fast > slow);
		var shortCondition = (!UseSdi || plusDi < minusDi) && (!UseEma || fast < slow);

		if (withinTime)
		{
			if (longCondition && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (shortCondition && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}
		else if (Position != 0)
		{
			ClosePosition();
		}

		UpdateTrailing(candle);
	}

	private void UpdateTrailing(ICandleMessage candle)
	{
		if (!UseTrailing || Position == 0)
		{
			_maxProfit = 0m;
			_trailingActive = false;
			return;
		}

		var dir = Math.Sign(Position);
		var entry = PositionAvgPrice;

		var currentProfit = dir > 0
			? (candle.HighPrice - entry) / entry * 100m
			: (entry - candle.LowPrice) / entry * 100m;

		if (currentProfit > _maxProfit)
			_maxProfit = currentProfit;

		if (!_trailingActive && currentProfit >= TrailingPercent)
			_trailingActive = true;

		if (_trailingActive && _maxProfit - currentProfit >= TrailingPercent)
		{
			ClosePosition();
			_trailingActive = false;
			_maxProfit = 0m;
		}
	}
}
