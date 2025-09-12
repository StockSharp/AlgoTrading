using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI + MACD long-only strategy with optional EMA trend filter and oversold context.
/// Enters long when RSI crosses above midline with MACD confirmation or MACD crosses above signal while RSI above
/// midline. Exits on RSI crossing below midline or MACD crossing below signal with non-positive histogram.
/// </summary>
public class RsiMacdLongOnlyStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiMidline;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<bool> _requireAboveZero;
	private readonly StrategyParam<bool> _useOversoldContext;
	private readonly StrategyParam<int> _oversoldWindowBars;
	private readonly StrategyParam<bool> _useEmaTrend;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<bool> _useTpSl;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private ExponentialMovingAverage _ema = null!;

	private bool _prevRsiAboveMid;
	private bool _prevMacdAboveSignal;
	private bool _inLong;
	private int _barsSinceOversold;

	/// <summary>
	/// RSI calculation length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI oversold threshold.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// RSI midline threshold.
	/// </summary>
	public decimal RsiMidline
	{
		get => _rsiMidline.Value;
		set => _rsiMidline.Value = value;
	}

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Signal line period for MACD.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Require MACD to be above zero.
	/// </summary>
	public bool RequireAboveZero
	{
		get => _requireAboveZero.Value;
		set => _requireAboveZero.Value = value;
	}

	/// <summary>
	/// Entry must happen within N bars after RSI dipped below oversold.
	/// </summary>
	public bool UseOversoldContext
	{
		get => _useOversoldContext.Value;
		set => _useOversoldContext.Value = value;
	}

	/// <summary>
	/// Bars allowed after RSI oversold condition.
	/// </summary>
	public int OversoldWindowBars
	{
		get => _oversoldWindowBars.Value;
		set => _oversoldWindowBars.Value = value;
	}

	/// <summary>
	/// Use EMA trend filter.
	/// </summary>
	public bool UseEmaTrend
	{
		get => _useEmaTrend.Value;
		set => _useEmaTrend.Value = value;
	}

	/// <summary>
	/// EMA length for trend filter.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Enable take profit and stop loss protection.
	/// </summary>
	public bool UseTpSl
	{
		get => _useTpSl.Value;
		set => _useTpSl.Value = value;
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
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="RsiMacdLongOnlyStrategy"/>.
	/// </summary>
	public RsiMacdLongOnlyStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
						 .SetGreaterThanZero()
						 .SetDisplay("RSI Length", "Period for RSI", "RSI")
						 .SetCanOptimize(true);

		_rsiOversold = Param(nameof(RsiOversold), 30m)
						   .SetRange(0, 50)
						   .SetDisplay("RSI Oversold", "Oversold threshold", "RSI")
						   .SetCanOptimize(true);

		_rsiMidline = Param(nameof(RsiMidline), 50m)
						  .SetRange(0, 100)
						  .SetDisplay("RSI Midline", "Midline level", "RSI")
						  .SetCanOptimize(true);

		_fastLength = Param(nameof(FastLength), 12)
						  .SetGreaterThanZero()
						  .SetDisplay("MACD Fast", "Fast EMA period", "MACD")
						  .SetCanOptimize(true);

		_slowLength = Param(nameof(SlowLength), 26)
						  .SetGreaterThanZero()
						  .SetDisplay("MACD Slow", "Slow EMA period", "MACD")
						  .SetCanOptimize(true);

		_signalLength = Param(nameof(SignalLength), 9)
							.SetGreaterThanZero()
							.SetDisplay("MACD Signal", "Signal line period", "MACD")
							.SetCanOptimize(true);

		_requireAboveZero =
			Param(nameof(RequireAboveZero), false).SetDisplay("Require MACD > 0", "Trend filter", "MACD");

		_useOversoldContext = Param(nameof(UseOversoldContext), false)
								  .SetDisplay("Use Oversold Context", "Entry within oversold window", "Signals");

		_oversoldWindowBars = Param(nameof(OversoldWindowBars), 10)
								  .SetGreaterThanZero()
								  .SetDisplay("Oversold Window", "Bars after RSI oversold", "Signals")
								  .SetCanOptimize(true);

		_useEmaTrend =
			Param(nameof(UseEmaTrend), false).SetDisplay("Use EMA Trend", "Only long when price above EMA", "Signals");

		_emaLength = Param(nameof(EmaLength), 200)
						 .SetGreaterThanZero()
						 .SetDisplay("EMA Length", "EMA period for trend", "Signals")
						 .SetCanOptimize(true);

		_useTpSl = Param(nameof(UseTpSl), true).SetDisplay("Use TP/SL", "Enable take profit and stop loss", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 11.5m)
								 .SetDisplay("Take Profit %", "Take profit percent", "Risk")
								 .SetCanOptimize(true);

		_stopLossPercent = Param(nameof(StopLossPercent), 2.5m)
							   .SetDisplay("Stop Loss %", "Stop loss percent", "Risk")
							   .SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
						  .SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_rsi = null!;
		_macd = null!;
		_ema = null!;
		_prevRsiAboveMid = false;
		_prevMacdAboveSignal = false;
		_inLong = false;
		_barsSinceOversold = int.MaxValue;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_macd = new MovingAverageConvergenceDivergenceSignal { Macd =
																   {
																	   ShortMa = { Length = FastLength },
																	   LongMa = { Length = SlowLength },
																   },
															   SignalMa = { Length = SignalLength } };
		_ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(_rsi, _macd, _ema, ProcessCandle).Start();

		if (UseTpSl)
		{
			StartProtection(takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
							stopLoss: new Unit(StopLossPercent, UnitTypes.Percent), isStopTrailing: false,
							useMarketOrders: true);
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			var macdArea = CreateChartArea();
			DrawIndicator(macdArea, _macd);
			var rsiArea = CreateChartArea();
			DrawIndicator(rsiArea, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue, IIndicatorValue macdValue,
							   IIndicatorValue emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var rsi = rsiValue.ToDecimal();
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		var macd = macdTyped.Macd;
		var macdSignal = macdTyped.Signal;
		var macdHist = macdTyped.Histogram;
		var ema = emaValue.ToDecimal();

		var rsiAboveMid = rsi > RsiMidline;
		var rsiCrossUpMid = rsiAboveMid && !_prevRsiAboveMid;
		var rsiCrossDownMid = !rsiAboveMid && _prevRsiAboveMid;
		_prevRsiAboveMid = rsiAboveMid;

		var macdAboveSignal = macd > macdSignal;
		var macdCrossUp = macdAboveSignal && !_prevMacdAboveSignal;
		var macdCrossDown = !macdAboveSignal && _prevMacdAboveSignal;
		_prevMacdAboveSignal = macdAboveSignal;

		if (rsi < RsiOversold)
			_barsSinceOversold = 0;
		else if (_barsSinceOversold < int.MaxValue)
			_barsSinceOversold++;

		var macdBull = macdAboveSignal && (!RequireAboveZero || macd > 0m);
		var longTrigger = (rsiCrossUpMid && macdBull) || (macdCrossUp && rsi >= RsiMidline);
		var recentlyOversold = _barsSinceOversold <= OversoldWindowBars;
		var trendOk = !UseEmaTrend || candle.ClosePrice > ema;
		var longEntry = longTrigger && (!UseOversoldContext || recentlyOversold) && trendOk;
		var exitSignal = rsiCrossDownMid || (macdCrossDown && macdHist <= 0m);
		var finalLongEntry = longEntry && !_inLong;
		var finalExit = exitSignal && _inLong;
		_inLong = (_inLong || finalLongEntry) && !finalExit;

		if (finalLongEntry && Position <= 0)
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			BuyMarket(volume);
		}
		else if (finalExit && Position > 0)
		{
			SellMarket(Position);
		}
	}
}
