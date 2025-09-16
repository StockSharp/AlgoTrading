namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on Ichimoku oscillator smoothed by Jurik moving average.
/// Opens a long position when the oscillator turns up and crosses above its previous value.
/// Opens a short position when the oscillator turns down and crosses below its previous value.
/// </summary>
public class IchimokuOscillatorStrategy : Strategy
{
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanBPeriod;
	private readonly StrategyParam<int> _smoothingPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _enableStopLoss;
	private readonly StrategyParam<decimal> _takeProfitPercent;

	private Ichimoku _ichimoku;
	private JurikMovingAverage _jma;

	private decimal? _prevValue;
	private decimal? _prevPrevValue;

	/// <summary>
	/// Tenkan-sen period for the Ichimoku indicator.
	/// </summary>
	public int TenkanPeriod
	{
		get => _tenkanPeriod.Value;
		set => _tenkanPeriod.Value = value;
	}

	/// <summary>
	/// Kijun-sen period for the Ichimoku indicator.
	/// </summary>
	public int KijunPeriod
	{
		get => _kijunPeriod.Value;
		set => _kijunPeriod.Value = value;
	}

	/// <summary>
	/// Senkou Span B period for the Ichimoku indicator.
	/// </summary>
	public int SenkouSpanBPeriod
	{
		get => _senkouSpanBPeriod.Value;
		set => _senkouSpanBPeriod.Value = value;
	}

	/// <summary>
	/// Period used for smoothing the oscillator with Jurik moving average.
	/// </summary>
	public int SmoothingPeriod
	{
		get => _smoothingPeriod.Value;
		set => _smoothingPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Enable stop loss.
	/// </summary>
	public bool EnableStopLoss
	{
		get => _enableStopLoss.Value;
		set => _enableStopLoss.Value = value;
	}

	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="IchimokuOscillatorStrategy"/>.
	/// </summary>
	public IchimokuOscillatorStrategy()
	{
		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
		.SetGreaterThanZero()
		.SetDisplay("Tenkan Period", "Period for Tenkan-sen line", "Ichimoku")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
		.SetGreaterThanZero()
		.SetDisplay("Kijun Period", "Period for Kijun-sen line", "Ichimoku")
		.SetCanOptimize(true)
		.SetOptimize(10, 40, 1);

		_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
		.SetGreaterThanZero()
		.SetDisplay("Senkou Span B Period", "Period for Senkou Span B", "Ichimoku")
		.SetCanOptimize(true)
		.SetOptimize(20, 80, 1);

		_smoothingPeriod = Param(nameof(SmoothingPeriod), 7)
		.SetGreaterThanZero()
		.SetDisplay("Smoothing Period", "Period for Jurik moving average", "Oscillator")
		.SetCanOptimize(true)
		.SetOptimize(3, 20, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for calculation", "Main");

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss %", "Stop loss in percent", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 3m, 0.5m);

		_enableStopLoss = Param(nameof(EnableStopLoss), true)
		.SetDisplay("Enable Stop Loss", "Use stop loss protection", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit %", "Take profit in percent", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 1m);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ichimoku = new Ichimoku
		{
			TenkanSen = TenkanPeriod,
			KijunSen = KijunPeriod,
			SenkouSpanB = SenkouSpanBPeriod
		};

		_jma = new JurikMovingAverage { Length = SmoothingPeriod };

		SubscribeCandles(CandleType)
			.BindEx(_ichimoku, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPercent * 100m, UnitTypes.Percent),
			stopLoss: EnableStopLoss ? new Unit(StopLossPercent * 100m, UnitTypes.Percent) : null);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichimokuValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var ich = (IchimokuValue)ichimokuValue;

		if (ich.ChinkouSpan is not decimal chikou ||
			ich.SenkouSpanB is not decimal spanB ||
			ich.TenkanSen is not decimal tenkan ||
			ich.KijunSen is not decimal kijun)
		return;

		var osc = (chikou - spanB) - (tenkan - kijun);
		var jmaVal = _jma.Process(new DecimalIndicatorValue(_jma, osc));
		if (!jmaVal.IsFinal)
		return;

		var current = jmaVal.ToDecimal();

		if (_prevValue is decimal prev && _prevPrevValue is decimal prevPrev)
		{
			var rising = prev < prevPrev;
			var falling = prev > prevPrev;

			if (rising)
			{
				if (Position < 0)
				BuyMarket();

				if (current >= prev && Position <= 0)
				BuyMarket();
			}
			else if (falling)
			{
				if (Position > 0)
				SellMarket();

				if (current <= prev && Position >= 0)
				SellMarket();
			}
		}

		_prevPrevValue = _prevValue;
		_prevValue = current;
	}
}