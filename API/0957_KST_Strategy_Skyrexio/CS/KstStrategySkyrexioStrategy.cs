using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// KST strategy with ATR-based exits and choppiness filter.
/// Goes long when KST crosses above its signal and price is above filter MA and Alligator jaw.
/// </summary>
public class KstStrategySkyrexioStrategy : Strategy
{
	private readonly StrategyParam<decimal> _atrStopLoss;
	private readonly StrategyParam<decimal> _atrTakeProfit;
	private readonly StrategyParam<MaType> _filterMaType;
	private readonly StrategyParam<int> _filterMaLength;
	private readonly StrategyParam<bool> _enableChopFilter;
	private readonly StrategyParam<decimal> _chopThreshold;
	private readonly StrategyParam<int> _chopLength;
	private readonly StrategyParam<int> _rocLen1;
	private readonly StrategyParam<int> _rocLen2;
	private readonly StrategyParam<int> _rocLen3;
	private readonly StrategyParam<int> _rocLen4;
	private readonly StrategyParam<int> _smaLen1;
	private readonly StrategyParam<int> _smaLen2;
	private readonly StrategyParam<int> _smaLen3;
	private readonly StrategyParam<int> _smaLen4;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevKst;
	private decimal _prevSig;
	private decimal _stopLoss;
	private decimal _takeProfit;

	/// <summary>
	/// Moving average type for filter.
	/// </summary>
	public enum MaType
	{
		SMA,
		EMA,
		WMA,
		HMA,
		SMMA,
		ALMA,
		LSMA,
		VWMA
	}

	/// <summary>
	/// ATR stop-loss multiplier.
	/// </summary>
	public decimal AtrStopLoss { get => _atrStopLoss.Value; set => _atrStopLoss.Value = value; }

	/// <summary>
	/// ATR take-profit multiplier.
	/// </summary>
	public decimal AtrTakeProfit { get => _atrTakeProfit.Value; set => _atrTakeProfit.Value = value; }

	/// <summary>
	/// Filter moving average type.
	/// </summary>
	public MaType FilterMaType { get => _filterMaType.Value; set => _filterMaType.Value = value; }

	/// <summary>
	/// Filter moving average length.
	/// </summary>
	public int FilterMaLength { get => _filterMaLength.Value; set => _filterMaLength.Value = value; }

	/// <summary>
	/// Enable choppiness filter.
	/// </summary>
	public bool EnableChopFilter { get => _enableChopFilter.Value; set => _enableChopFilter.Value = value; }

	/// <summary>
	/// Choppiness threshold.
	/// </summary>
	public decimal ChopThreshold { get => _chopThreshold.Value; set => _chopThreshold.Value = value; }

	/// <summary>
	/// Choppiness period.
	/// </summary>
	public int ChopLength { get => _chopLength.Value; set => _chopLength.Value = value; }

	/// <summary>
	/// ROC length #1.
	/// </summary>
	public int RocLen1 { get => _rocLen1.Value; set => _rocLen1.Value = value; }

	/// <summary>
	/// ROC length #2.
	/// </summary>
	public int RocLen2 { get => _rocLen2.Value; set => _rocLen2.Value = value; }

	/// <summary>
	/// ROC length #3.
	/// </summary>
	public int RocLen3 { get => _rocLen3.Value; set => _rocLen3.Value = value; }

	/// <summary>
	/// ROC length #4.
	/// </summary>
	public int RocLen4 { get => _rocLen4.Value; set => _rocLen4.Value = value; }

	/// <summary>
	/// SMA length #1.
	/// </summary>
	public int SmaLen1 { get => _smaLen1.Value; set => _smaLen1.Value = value; }

	/// <summary>
	/// SMA length #2.
	/// </summary>
	public int SmaLen2 { get => _smaLen2.Value; set => _smaLen2.Value = value; }

	/// <summary>
	/// SMA length #3.
	/// </summary>
	public int SmaLen3 { get => _smaLen3.Value; set => _smaLen3.Value = value; }

	/// <summary>
	/// SMA length #4.
	/// </summary>
	public int SmaLen4 { get => _smaLen4.Value; set => _smaLen4.Value = value; }

	/// <summary>
	/// KST signal length.
	/// </summary>
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public KstStrategySkyrexioStrategy()
	{
		_atrStopLoss = Param(nameof(AtrStopLoss), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Stop Loss", "ATR stop-loss multiplier", "Stops");

		_atrTakeProfit = Param(nameof(AtrTakeProfit), 3.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Take Profit", "ATR take-profit multiplier", "Stops");

		_filterMaType = Param(nameof(FilterMaType), MaType.LSMA)
			.SetDisplay("Filter MA Type", "Type of trend filter", "Filter");

		_filterMaLength = Param(nameof(FilterMaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("Filter MA Length", "Length of trend filter", "Filter");

		_enableChopFilter = Param(nameof(EnableChopFilter), true)
			.SetDisplay("Enable Choppiness", "Use choppiness filter", "Choppiness");

		_chopThreshold = Param(nameof(ChopThreshold), 50m)
			.SetDisplay("Choppiness Threshold", "Threshold for choppiness index", "Choppiness");

		_chopLength = Param(nameof(ChopLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Choppiness Length", "Choppiness index period", "Choppiness");

		_rocLen1 = Param(nameof(RocLen1), 10)
			.SetGreaterThanZero()
			.SetDisplay("ROC Length #1", "First ROC length", "KST");

		_rocLen2 = Param(nameof(RocLen2), 15)
			.SetGreaterThanZero()
			.SetDisplay("ROC Length #2", "Second ROC length", "KST");

		_rocLen3 = Param(nameof(RocLen3), 20)
			.SetGreaterThanZero()
			.SetDisplay("ROC Length #3", "Third ROC length", "KST");

		_rocLen4 = Param(nameof(RocLen4), 30)
			.SetGreaterThanZero()
			.SetDisplay("ROC Length #4", "Fourth ROC length", "KST");

		_smaLen1 = Param(nameof(SmaLen1), 10)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length #1", "First SMA length", "KST");

		_smaLen2 = Param(nameof(SmaLen2), 10)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length #2", "Second SMA length", "KST");

		_smaLen3 = Param(nameof(SmaLen3), 10)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length #3", "Third SMA length", "KST");

		_smaLen4 = Param(nameof(SmaLen4), 15)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length #4", "Fourth SMA length", "KST");

		_signalLength = Param(nameof(SignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "KST signal SMA length", "KST");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		var roc1 = new RateOfChange { Length = RocLen1 };
		var sma1 = new SimpleMovingAverage { Length = SmaLen1 };
		var roc2 = new RateOfChange { Length = RocLen2 };
		var sma2 = new SimpleMovingAverage { Length = SmaLen2 };
		var roc3 = new RateOfChange { Length = RocLen3 };
		var sma3 = new SimpleMovingAverage { Length = SmaLen3 };
		var roc4 = new RateOfChange { Length = RocLen4 };
		var sma4 = new SimpleMovingAverage { Length = SmaLen4 };
		var signal = new SimpleMovingAverage { Length = SignalLength };
		var atr = new AverageTrueRange { Length = 14 };
		var choppiness = new ChoppinessIndex { Length = ChopLength };
		var jaw = new SmoothedMovingAverage { Length = 13 };
		var jawShift = new Shift { Length = 8 };
		var filter = CreateFilterMa();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, filter);
			DrawIndicator(area, jaw);
			DrawIndicator(area, choppiness);
			DrawOwnTrades(area);
		}

		void ProcessCandle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;

			var close = candle.ClosePrice;
			var median = (candle.HighPrice + candle.LowPrice) / 2m;

			var r1 = sma1.Process(roc1.Process(close));
			var r2 = sma2.Process(roc2.Process(close));
			var r3 = sma3.Process(roc3.Process(close));
			var r4 = sma4.Process(roc4.Process(close));
			var jawVal = jawShift.Process(jaw.Process(median));
			var filterVal = filter.Process(close);
			var atrVal = atr.Process(candle);
			var chopVal = choppiness.Process(candle);

			if (!r1.IsFinal || !r2.IsFinal || !r3.IsFinal || !r4.IsFinal || !jawVal.IsFinal || !filterVal.IsFinal || !atrVal.IsFinal || !chopVal.IsFinal)
				return;

			var kst = r1.GetValue<decimal>() + 2m * r2.GetValue<decimal>() + 3m * r3.GetValue<decimal>() + 4m * r4.GetValue<decimal>();
			var sigVal = signal.Process(kst);
			if (!sigVal.IsFinal)
				return;

			var sig = sigVal.GetValue<decimal>();
			var jawValue = jawVal.GetValue<decimal>();
			var filterValue = filterVal.GetValue<decimal>();
			var atrValue = atrVal.GetValue<decimal>();
			var chop = chopVal.GetValue<decimal>();
			var chopCond = !EnableChopFilter || chop < ChopThreshold;

			var crossUp = _prevKst <= _prevSig && kst > sig;
			if (crossUp && close > filterValue && close > jawValue && chopCond && Position == 0)
			{
				_stopLoss = candle.LowPrice - AtrStopLoss * atrValue;
				_takeProfit = close + AtrTakeProfit * atrValue;
				BuyMarket();
			}

			if (Position > 0 && (candle.LowPrice <= _stopLoss || candle.HighPrice >= _takeProfit))
				SellMarket(Position);

			_prevKst = kst;
			_prevSig = sig;
		}
	}

	private IndicatorBase<decimal> CreateFilterMa()
	{
		return FilterMaType switch
		{
			MaType.SMA => new SimpleMovingAverage { Length = FilterMaLength },
			MaType.EMA => new ExponentialMovingAverage { Length = FilterMaLength },
			MaType.WMA => new WeightedMovingAverage { Length = FilterMaLength },
			MaType.HMA => new HullMovingAverage { Length = FilterMaLength },
			MaType.SMMA => new SmoothedMovingAverage { Length = FilterMaLength },
			MaType.ALMA => new ArnaudLegouxMovingAverage { Length = FilterMaLength },
			MaType.LSMA => new LinearRegression { Length = FilterMaLength },
			MaType.VWMA => new VolumeWeightedMovingAverage { Length = FilterMaLength },
			_ => new SimpleMovingAverage { Length = FilterMaLength }
		};
	}
}
