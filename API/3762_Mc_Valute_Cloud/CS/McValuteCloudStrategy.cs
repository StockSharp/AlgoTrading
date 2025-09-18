using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend-following strategy inspired by the "Mc_valute" MetaTrader expert advisor.
/// Combines smoothed moving averages, an Ichimoku cloud filter and a MACD confirmation.
/// </summary>
public class McValuteCloudStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _filterMaLength;
	private readonly StrategyParam<int> _blueMaLength;
	private readonly StrategyParam<int> _limeMaLength;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<int> _tenkanLength;
	private readonly StrategyParam<int> _kijunLength;
	private readonly StrategyParam<int> _senkouLength;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _stopLoss;

	private ExponentialMovingAverage _filterMa = null!;
	private SmoothedMovingAverage _blueMa = null!;
	private SmoothedMovingAverage _limeMa = null!;
	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private Ichimoku _ichimoku = null!;

	private decimal? _filterValue;
	private decimal? _blueValue;
	private decimal? _limeValue;
	private decimal? _senkouAValue;
	private decimal? _senkouBValue;
	private decimal? _macdMainValue;
	private decimal? _macdSignalValue;

	private DateTimeOffset _lastProcessedTime;

	/// <summary>
	/// Initializes a new instance of the <see cref="McValuteCloudStrategy"/> class.
	/// </summary>
	public McValuteCloudStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Candle Type", "Primary timeframe used for signals", "General");

		_filterMaLength = Param(nameof(FilterMaLength), 3)
		.SetDisplay("Filter EMA", "Length of the trend filter EMA", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(2, 20, 1);

		_blueMaLength = Param(nameof(BlueMaLength), 13)
		.SetDisplay("Blue SMMA", "Length of the slower smoothed MA", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(5, 60, 1);

		_limeMaLength = Param(nameof(LimeMaLength), 5)
		.SetDisplay("Lime SMMA", "Length of the faster smoothed MA", "Trend")
		.SetCanOptimize(true)
		.SetOptimize(3, 40, 1);

		_macdFastLength = Param(nameof(MacdFastLength), 12)
		.SetDisplay("MACD Fast", "Short EMA length for the MACD", "Momentum")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 1);

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
		.SetDisplay("MACD Slow", "Long EMA length for the MACD", "Momentum")
		.SetCanOptimize(true)
		.SetOptimize(10, 80, 1);

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
		.SetDisplay("MACD Signal", "Signal EMA length for the MACD", "Momentum")
		.SetCanOptimize(true)
		.SetOptimize(3, 30, 1);

		_tenkanLength = Param(nameof(TenkanLength), 12)
		.SetDisplay("Tenkan", "Tenkan-sen length for the Ichimoku cloud", "Ichimoku")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 1);

		_kijunLength = Param(nameof(KijunLength), 20)
		.SetDisplay("Kijun", "Kijun-sen length for the Ichimoku cloud", "Ichimoku")
		.SetCanOptimize(true)
		.SetOptimize(10, 60, 1);

		_senkouLength = Param(nameof(SenkouLength), 40)
		.SetDisplay("Senkou Span B", "Span B length for the Ichimoku cloud", "Ichimoku")
		.SetCanOptimize(true)
		.SetOptimize(20, 120, 1);

		_takeProfit = Param(nameof(TakeProfit), 30)
		.SetDisplay("Take Profit", "Take profit distance in points", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(10, 200, 5);

		_stopLoss = Param(nameof(StopLoss), 350)
		.SetDisplay("Stop Loss", "Stop loss distance in points", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(50, 600, 10);

		_lastProcessedTime = DateTimeOffset.MinValue;
	}

	/// <summary>
	/// Timeframe used for the working candles.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Length of the EMA filter.
	/// </summary>
	public int FilterMaLength
	{
		get => _filterMaLength.Value;
		set => _filterMaLength.Value = value;
	}

	/// <summary>
	/// Length of the slower smoothed moving average.
	/// </summary>
	public int BlueMaLength
	{
		get => _blueMaLength.Value;
		set => _blueMaLength.Value = value;
	}

	/// <summary>
	/// Length of the faster smoothed moving average.
	/// </summary>
	public int LimeMaLength
	{
		get => _limeMaLength.Value;
		set => _limeMaLength.Value = value;
	}

	/// <summary>
	/// Fast EMA length used by the MACD.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length used by the MACD.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// Signal EMA length used by the MACD.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Tenkan-sen length for the Ichimoku indicator.
	/// </summary>
	public int TenkanLength
	{
		get => _tenkanLength.Value;
		set => _tenkanLength.Value = value;
	}

	/// <summary>
	/// Kijun-sen length for the Ichimoku indicator.
	/// </summary>
	public int KijunLength
	{
		get => _kijunLength.Value;
		set => _kijunLength.Value = value;
	}

	/// <summary>
	/// Senkou Span B length for the Ichimoku indicator.
	/// </summary>
	public int SenkouLength
	{
		get => _senkouLength.Value;
		set => _senkouLength.Value = value;
	}

	/// <summary>
	/// Take profit distance in points.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss distance in points.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_filterValue = null;
		_blueValue = null;
		_limeValue = null;
		_senkouAValue = null;
		_senkouBValue = null;
		_macdMainValue = null;
		_macdSignalValue = null;
		_lastProcessedTime = DateTimeOffset.MinValue;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_filterMa = new ExponentialMovingAverage { Length = FilterMaLength };
		_blueMa = new SmoothedMovingAverage { Length = BlueMaLength };
		_limeMa = new SmoothedMovingAverage { Length = LimeMaLength };

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastLength },
				LongMa = { Length = MacdSlowLength },
			},
			SignalMa = { Length = MacdSignalLength }
		};

		_ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanLength },
			Kijun = { Length = KijunLength },
			SenkouB = { Length = SenkouLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_filterMa, _blueMa, _limeMa, ProcessMovingAverages)
		.BindEx(_macd, ProcessMacd)
		.BindEx(_ichimoku, ProcessIchimoku)
		.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _filterMa);
			DrawIndicator(area, _blueMa);
			DrawIndicator(area, _limeMa);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessMovingAverages(ICandleMessage candle, decimal filter, decimal blue, decimal lime)
	{
		if (candle.State != CandleStates.Finished)
		return;

		_filterValue = filter;
		_blueValue = blue;
		_limeValue = lime;

		TryTrade(candle);
	}

	private void ProcessMacd(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var typed = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (typed.Macd is not decimal macd || typed.Signal is not decimal signal)
		return;

		_macdMainValue = macd;
		_macdSignalValue = signal;

		TryTrade(candle);
	}

	private void ProcessIchimoku(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var ichimokuValue = (IchimokuValue)value;
		if (ichimokuValue.UpBand is not decimal spanA || ichimokuValue.DownBand is not decimal spanB)
		return;

		_senkouAValue = spanA;
		_senkouBValue = spanB;

		TryTrade(candle);
	}

	private void TryTrade(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (candle.State != CandleStates.Finished)
		return;

		if (_lastProcessedTime == candle.OpenTime)
		return;

		if (_filterValue is not decimal filter ||
		_blueValue is not decimal blue ||
		_limeValue is not decimal lime ||
		_senkouAValue is not decimal spanA ||
		_senkouBValue is not decimal spanB ||
		_macdMainValue is not decimal macd ||
		_macdSignalValue is not decimal signal)
		{
			return;
		}

		_lastProcessedTime = candle.OpenTime;

		var cloudTop = Math.Max(spanA, spanB);
		var cloudBottom = Math.Min(spanA, spanB);
		var maUpper = Math.Max(blue, lime);
		var maLower = Math.Min(blue, lime);

		var allowLong = filter > maUpper && filter > cloudTop && macd > signal;
		var allowShort = filter < maLower && filter < cloudBottom && macd < signal;

		var closePrice = candle.ClosePrice;

		if (allowLong && Position <= 0)
		{
			var volumeToBuy = Volume + Math.Max(0m, -Position);
			var resultingPosition = Position + volumeToBuy;
			BuyMarket(volumeToBuy);

			ApplyRisk(closePrice, resultingPosition);
		}
		else if (allowShort && Position >= 0)
		{
			var volumeToSell = Volume + Math.Max(0m, Position);
			var resultingPosition = Position - volumeToSell;
			SellMarket(volumeToSell);

			ApplyRisk(closePrice, resultingPosition);
		}
	}

	private void ApplyRisk(decimal price, decimal resultingPosition)
	{
		if (TakeProfit > 0)
		SetTakeProfit(TakeProfit, price, resultingPosition);

		if (StopLoss > 0)
		SetStopLoss(StopLoss, price, resultingPosition);
	}
}
