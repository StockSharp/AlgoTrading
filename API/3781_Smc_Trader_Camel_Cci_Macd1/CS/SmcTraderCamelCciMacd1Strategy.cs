namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that mirrors the "Steve Cartwright Trader Camel CCI MACD" expert advisor.
/// Uses EMA envelopes on highs and lows together with MACD and CCI filters.
/// </summary>
public class SmcTraderCamelCciMacd1Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _camelLength;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<decimal> _cciThreshold;

	private ExponentialMovingAverage _camelHighEma;
	private ExponentialMovingAverage _camelLowEma;
	private CommodityChannelIndex _cci;
	private MovingAverageConvergenceDivergence _macd;

	private decimal? _previousClose;
	private decimal? _previousCamelHigh;
	private decimal? _previousCamelLow;
	private decimal? _previousMacdMain;
	private decimal? _previousMacdSignal;
	private decimal? _previousCci;

	private TimeSpan? _timeFrame;
	private DateTimeOffset? _lastExitTime;

	public SmcTraderCamelCciMacd1Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for indicator calculations", "General");

		_camelLength = Param(nameof(CamelLength), 34)
			.SetGreaterThanZero()
			.SetDisplay("Camel EMA Length", "Period for exponential moving averages of highs and lows", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 80, 2);

		_cciPeriod = Param(nameof(CciPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Commodity Channel Index period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 2);

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast Period", "Short EMA length for MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(6, 18, 1);

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow Period", "Long EMA length for MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(16, 40, 2);

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal Period", "Signal line smoothing period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(4, 18, 1);

		_cciThreshold = Param(nameof(CciThreshold), 100m)
			.SetGreaterThanZero()
			.SetDisplay("CCI Threshold", "Absolute CCI level required for entries", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(60m, 160m, 10m);
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int CamelLength
	{
		get => _camelLength.Value;
		set => _camelLength.Value = value;
	}

	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	public decimal CciThreshold
	{
		get => _cciThreshold.Value;
		set => _cciThreshold.Value = value;
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

		_previousClose = null;
		_previousCamelHigh = null;
		_previousCamelLow = null;
		_previousMacdMain = null;
		_previousMacdSignal = null;
		_previousCci = null;
		_lastExitTime = null;
		_timeFrame = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_timeFrame = CandleType.Arg as TimeSpan?;

		_camelHighEma = new ExponentialMovingAverage
		{
			Length = CamelLength,
			CandlePrice = CandlePrice.High
		};

		_camelLowEma = new ExponentialMovingAverage
		{
			Length = CamelLength,
			CandlePrice = CandlePrice.Low
		};

		_cci = new CommodityChannelIndex
		{
			Length = CciPeriod
		};

		_macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = MacdFastPeriod,
			LongPeriod = MacdSlowPeriod,
			SignalPeriod = MacdSignalPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_camelHighEma, _camelLowEma, _macd, _cci, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _camelHighEma);
			DrawIndicator(area, _camelLowEma);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);

			var cciArea = CreateChartArea();
			if (cciArea != null)
				DrawIndicator(cciArea, _cci);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal camelHigh, decimal camelLow, decimal macdMain, decimal macdSignal, decimal macdHistogram, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var previousMacdMain = _previousMacdMain;
		var previousMacdSignal = _previousMacdSignal;
		var previousCci = _previousCci;
		var previousClose = _previousClose;
		var previousCamelHigh = _previousCamelHigh;
		var previousCamelLow = _previousCamelLow;

		var exitExecuted = false;

		if (previousMacdMain is decimal prevMain && previousMacdSignal is decimal prevSignal && previousCci is decimal prevCci)
		{
			if (Position > 0m && (prevMain < prevSignal || prevCci < CciThreshold))
			{
				SellMarket(Position);
				_lastExitTime = candle.CloseTime;
				exitExecuted = true;
			}
			else if (Position < 0m && prevMain > prevSignal)
			{
				BuyMarket(-Position);
				_lastExitTime = candle.CloseTime;
				exitExecuted = true;
			}
		}

		if (!exitExecuted && Position == 0m && IsFormedAndOnlineAndAllowTrading() &&
			previousMacdMain is decimal entryMain && previousMacdSignal is decimal entrySignal &&
			previousCci is decimal entryCci && previousClose is decimal entryClose &&
			previousCamelHigh is decimal entryCamelHigh && previousCamelLow is decimal entryCamelLow)
		{
			var enoughTimePassed = true;

			if (_timeFrame is TimeSpan frame && _lastExitTime is DateTimeOffset exitTime)
			{
				var timeSinceExit = candle.CloseTime - exitTime;
				if (timeSinceExit < frame)
					enoughTimePassed = false;
			}

			if (enoughTimePassed)
			{
				if (entryCci > CciThreshold && entryMain > 0m && entryMain > entrySignal && entryClose > entryCamelHigh)
				{
					BuyMarket();
					exitExecuted = true;
				}
				else if (entryCci < -CciThreshold && entryMain < 0m && entryMain < entrySignal && entryClose < entryCamelLow)
				{
					SellMarket();
					exitExecuted = true;
				}
			}
		}

		_previousClose = candle.ClosePrice;
		_previousCamelHigh = camelHigh;
		_previousCamelLow = camelLow;
		_previousMacdMain = macdMain;
		_previousMacdSignal = macdSignal;
		_previousCci = cciValue;

		if (exitExecuted && Position == 0m)
			_lastExitTime = candle.CloseTime;
	}
}
