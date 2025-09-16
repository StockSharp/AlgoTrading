namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class IbsRsiCciV4X2Strategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _trendCandleType;
	private readonly StrategyParam<int> _trendIbsPeriod;
	private readonly StrategyParam<IbsMovingAverageType> _trendIbsMaType;
	private readonly StrategyParam<int> _trendRsiPeriod;
	private readonly StrategyParam<AppliedPriceType> _trendRsiPrice;
	private readonly StrategyParam<int> _trendCciPeriod;
	private readonly StrategyParam<AppliedPriceType> _trendCciPrice;
	private readonly StrategyParam<decimal> _trendThreshold;
	private readonly StrategyParam<int> _trendRangePeriod;
	private readonly StrategyParam<int> _trendSmoothPeriod;
	private readonly StrategyParam<int> _trendSignalBar;
	private readonly StrategyParam<bool> _allowLongEntries;
	private readonly StrategyParam<bool> _allowShortEntries;
	private readonly StrategyParam<bool> _closeLongOnTrendFlip;
	private readonly StrategyParam<bool> _closeShortOnTrendFlip;

	private readonly StrategyParam<DataType> _signalCandleType;
	private readonly StrategyParam<int> _signalIbsPeriod;
	private readonly StrategyParam<IbsMovingAverageType> _signalIbsMaType;
	private readonly StrategyParam<int> _signalRsiPeriod;
	private readonly StrategyParam<AppliedPriceType> _signalRsiPrice;
	private readonly StrategyParam<int> _signalCciPeriod;
	private readonly StrategyParam<AppliedPriceType> _signalCciPrice;
	private readonly StrategyParam<decimal> _signalThreshold;
	private readonly StrategyParam<int> _signalRangePeriod;
	private readonly StrategyParam<int> _signalSmoothPeriod;
	private readonly StrategyParam<int> _signalSignalBar;
	private readonly StrategyParam<bool> _closeLongOnSignalCross;
	private readonly StrategyParam<bool> _closeShortOnSignalCross;

	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;

	private readonly List<IbsRsiCciValue> _trendValues = new();
	private readonly List<IbsRsiCciValue> _signalValues = new();

	private IbsRsiCciCalculator? _trendCalculator;
	private IbsRsiCciCalculator? _signalCalculator;

	private int _trendDirection;

	public IbsRsiCciV4X2Strategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Trend TF", "Trend timeframe", "Trend");

		_trendIbsPeriod = Param(nameof(TrendIbsPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Trend IBS", "IBS smoothing period", "Trend");

		_trendIbsMaType = Param(nameof(TrendIbsMaType), IbsMovingAverageType.Simple)
			.SetDisplay("Trend IBS MA", "IBS smoothing type", "Trend");

		_trendRsiPeriod = Param(nameof(TrendRsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Trend RSI", "RSI period", "Trend");

		_trendRsiPrice = Param(nameof(TrendRsiPrice), AppliedPriceType.Close)
			.SetDisplay("Trend RSI Price", "RSI price type", "Trend");

		_trendCciPeriod = Param(nameof(TrendCciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Trend CCI", "CCI period", "Trend");

		_trendCciPrice = Param(nameof(TrendCciPrice), AppliedPriceType.Median)
			.SetDisplay("Trend CCI Price", "CCI price type", "Trend");

		_trendThreshold = Param(nameof(TrendThreshold), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Trend Threshold", "Momentum clamp threshold", "Trend");

		_trendRangePeriod = Param(nameof(TrendRangePeriod), 25)
			.SetGreaterThanZero()
			.SetDisplay("Trend Range", "Range period", "Trend");

		_trendSmoothPeriod = Param(nameof(TrendSmoothPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Trend Smooth", "Range smoothing period", "Trend");

		_trendSignalBar = Param(nameof(TrendSignalBar), 1)
			.SetGreaterOrEqual(0)
			.SetDisplay("Trend Shift", "Shift used to read indicator", "Trend");

		_allowLongEntries = Param(nameof(AllowLongEntries), true)
			.SetDisplay("Allow Long", "Enable long entries", "Trading");

		_allowShortEntries = Param(nameof(AllowShortEntries), true)
			.SetDisplay("Allow Short", "Enable short entries", "Trading");

		_closeLongOnTrendFlip = Param(nameof(CloseLongOnTrendFlip), true)
			.SetDisplay("Close Long Trend", "Close longs on bearish trend", "Trading");

		_closeShortOnTrendFlip = Param(nameof(CloseShortOnTrendFlip), true)
			.SetDisplay("Close Short Trend", "Close shorts on bullish trend", "Trading");

		_signalCandleType = Param(nameof(SignalCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Signal TF", "Signal timeframe", "Signal");

		_signalIbsPeriod = Param(nameof(SignalIbsPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Signal IBS", "IBS smoothing period", "Signal");

		_signalIbsMaType = Param(nameof(SignalIbsMaType), IbsMovingAverageType.Simple)
			.SetDisplay("Signal IBS MA", "IBS smoothing type", "Signal");

		_signalRsiPeriod = Param(nameof(SignalRsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Signal RSI", "RSI period", "Signal");

		_signalRsiPrice = Param(nameof(SignalRsiPrice), AppliedPriceType.Close)
			.SetDisplay("Signal RSI Price", "RSI price type", "Signal");

		_signalCciPeriod = Param(nameof(SignalCciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Signal CCI", "CCI period", "Signal");

		_signalCciPrice = Param(nameof(SignalCciPrice), AppliedPriceType.Median)
			.SetDisplay("Signal CCI Price", "CCI price type", "Signal");

		_signalThreshold = Param(nameof(SignalThreshold), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Signal Threshold", "Momentum clamp threshold", "Signal");

		_signalRangePeriod = Param(nameof(SignalRangePeriod), 25)
			.SetGreaterThanZero()
			.SetDisplay("Signal Range", "Range period", "Signal");

		_signalSmoothPeriod = Param(nameof(SignalSmoothPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("Signal Smooth", "Range smoothing period", "Signal");

		_signalSignalBar = Param(nameof(SignalSignalBar), 1)
			.SetGreaterOrEqual(0)
			.SetDisplay("Signal Shift", "Shift used to read indicator", "Signal");

		_closeLongOnSignalCross = Param(nameof(CloseLongOnSignalCross), false)
			.SetDisplay("Close Long Signal", "Close longs on bearish cross", "Signal");

		_closeShortOnSignalCross = Param(nameof(CloseShortOnSignalCross), false)
			.SetDisplay("Close Short Signal", "Close shorts on bullish cross", "Signal");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000)
			.SetGreaterOrEqual(0)
			.SetDisplay("Stop Loss", "Stop loss in points", "Protection");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
			.SetGreaterOrEqual(0)
			.SetDisplay("Take Profit", "Take profit in points", "Protection");
	}


	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	public DataType TrendCandleType
	{
		get => _trendCandleType.Value;
		set => _trendCandleType.Value = value;
	}

	public int TrendIbsPeriod
	{
		get => _trendIbsPeriod.Value;
		set => _trendIbsPeriod.Value = value;
	}

	public IbsMovingAverageType TrendIbsMaType
	{
		get => _trendIbsMaType.Value;
		set => _trendIbsMaType.Value = value;
	}

	public int TrendRsiPeriod
	{
		get => _trendRsiPeriod.Value;
		set => _trendRsiPeriod.Value = value;
	}

	public AppliedPriceType TrendRsiPrice
	{
		get => _trendRsiPrice.Value;
		set => _trendRsiPrice.Value = value;
	}

	public int TrendCciPeriod
	{
		get => _trendCciPeriod.Value;
		set => _trendCciPeriod.Value = value;
	}

	public AppliedPriceType TrendCciPrice
	{
		get => _trendCciPrice.Value;
		set => _trendCciPrice.Value = value;
	}

	public decimal TrendThreshold
	{
		get => _trendThreshold.Value;
		set => _trendThreshold.Value = value;
	}

	public int TrendRangePeriod
	{
		get => _trendRangePeriod.Value;
		set => _trendRangePeriod.Value = value;
	}

	public int TrendSmoothPeriod
	{
		get => _trendSmoothPeriod.Value;
		set => _trendSmoothPeriod.Value = value;
	}

	public int TrendSignalBar
	{
		get => _trendSignalBar.Value;
		set => _trendSignalBar.Value = value;
	}

	public bool AllowLongEntries
	{
		get => _allowLongEntries.Value;
		set => _allowLongEntries.Value = value;
	}

	public bool AllowShortEntries
	{
		get => _allowShortEntries.Value;
		set => _allowShortEntries.Value = value;
	}

	public bool CloseLongOnTrendFlip
	{
		get => _closeLongOnTrendFlip.Value;
		set => _closeLongOnTrendFlip.Value = value;
	}

	public bool CloseShortOnTrendFlip
	{
		get => _closeShortOnTrendFlip.Value;
		set => _closeShortOnTrendFlip.Value = value;
	}

	public DataType SignalCandleType
	{
		get => _signalCandleType.Value;
		set => _signalCandleType.Value = value;
	}

	public int SignalIbsPeriod
	{
		get => _signalIbsPeriod.Value;
		set => _signalIbsPeriod.Value = value;
	}

	public IbsMovingAverageType SignalIbsMaType
	{
		get => _signalIbsMaType.Value;
		set => _signalIbsMaType.Value = value;
	}

	public int SignalRsiPeriod
	{
		get => _signalRsiPeriod.Value;
		set => _signalRsiPeriod.Value = value;
	}

	public AppliedPriceType SignalRsiPrice
	{
		get => _signalRsiPrice.Value;
		set => _signalRsiPrice.Value = value;
	}

	public int SignalCciPeriod
	{
		get => _signalCciPeriod.Value;
		set => _signalCciPeriod.Value = value;
	}

	public AppliedPriceType SignalCciPrice
	{
		get => _signalCciPrice.Value;
		set => _signalCciPrice.Value = value;
	}

	public decimal SignalThreshold
	{
		get => _signalThreshold.Value;
		set => _signalThreshold.Value = value;
	}

	public int SignalRangePeriod
	{
		get => _signalRangePeriod.Value;
		set => _signalRangePeriod.Value = value;
	}

	public int SignalSmoothPeriod
	{
		get => _signalSmoothPeriod.Value;
		set => _signalSmoothPeriod.Value = value;
	}

	public int SignalSignalBar
	{
		get => _signalSignalBar.Value;
		set => _signalSignalBar.Value = value;
	}

	public bool CloseLongOnSignalCross
	{
		get => _closeLongOnSignalCross.Value;
		set => _closeLongOnSignalCross.Value = value;
	}

	public bool CloseShortOnSignalCross
	{
		get => _closeShortOnSignalCross.Value;
		set => _closeShortOnSignalCross.Value = value;
	}

	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> new[]
		{
			(Security, TrendCandleType),
			(Security, SignalCandleType)
		};

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_trendValues.Clear();
		_signalValues.Clear();
		_trendDirection = 0;
		_trendCalculator?.Reset();
		_signalCalculator?.Reset();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var priceStep = Security?.PriceStep ?? 0.0001m;

		_trendCalculator = new IbsRsiCciCalculator(
			TrendIbsPeriod,
			TrendIbsMaType,
			TrendRsiPeriod,
			TrendRsiPrice,
			TrendCciPeriod,
			TrendCciPrice,
			TrendThreshold,
			TrendRangePeriod,
			TrendSmoothPeriod,
			priceStep);

		_signalCalculator = new IbsRsiCciCalculator(
			SignalIbsPeriod,
			SignalIbsMaType,
			SignalRsiPeriod,
			SignalRsiPrice,
			SignalCciPeriod,
			SignalCciPrice,
			SignalThreshold,
			SignalRangePeriod,
			SignalSmoothPeriod,
			priceStep);

		var trendSubscription = SubscribeCandles(TrendCandleType);
		trendSubscription.Bind(ProcessTrend).Start();

		var signalSubscription = SubscribeCandles(SignalCandleType);
		signalSubscription.Bind(ProcessSignal).Start();

		if (TakeProfitPoints > 0 || StopLossPoints > 0)
		{
			var takeProfit = new Unit(TakeProfitPoints * priceStep, UnitTypes.Absolute);
			var stopLoss = new Unit(StopLossPoints * priceStep, UnitTypes.Absolute);
			StartProtection(takeProfit: takeProfit, stopLoss: stopLoss);
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, signalSubscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessTrend(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || _trendCalculator == null)
			return;

		var value = _trendCalculator.Process(candle);
		if (value == null)
			return;

		_trendValues.Add(value.Value);

		var maxCount = Math.Max(TrendSignalBar + 5, 32);
		if (_trendValues.Count > maxCount)
			_trendValues.RemoveAt(0);

		if (_trendValues.Count <= TrendSignalBar)
			return;

		var index = _trendValues.Count - (TrendSignalBar + 1);
		if (index < 0)
			return;

		var selected = _trendValues[index];
		if (selected.Up > selected.Down)
			_trendDirection = 1;
		else if (selected.Up < selected.Down)
			_trendDirection = -1;
		else
			_trendDirection = 0;
	}

	private void ProcessSignal(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || _signalCalculator == null)
			return;

		var value = _signalCalculator.Process(candle);
		if (value == null)
			return;

		_signalValues.Add(value.Value);

		var maxCount = Math.Max(SignalSignalBar + 10, 48);
		if (_signalValues.Count > maxCount)
			_signalValues.RemoveAt(0);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_signalValues.Count <= SignalSignalBar + 1)
			return;

		var currentIndex = _signalValues.Count - (SignalSignalBar + 1);
		var previousIndex = currentIndex - 1;
		if (currentIndex < 0 || previousIndex < 0)
			return;

		var current = _signalValues[currentIndex];
		var previous = _signalValues[previousIndex];

		var closeLong = CloseLongOnSignalCross && previous.Up < previous.Down;
		var closeShort = CloseShortOnSignalCross && previous.Up > previous.Down;
		var openLong = false;
		var openShort = false;

		if (_trendDirection < 0)
		{
			if (CloseLongOnTrendFlip)
				closeLong = true;

			if (AllowShortEntries && current.Up >= current.Down && previous.Up < previous.Down)
				openShort = true;
		}
		else if (_trendDirection > 0)
		{
			if (CloseShortOnTrendFlip)
				closeShort = true;

			if (AllowLongEntries && current.Up <= current.Down && previous.Up > previous.Down)
				openLong = true;
		}

		if (closeLong && Position > 0)
			CloseLong();

		if (closeShort && Position < 0)
			CloseShort();

		if (openLong && Position <= 0 && AllowLongEntries)
			EnterLong();
		else if (openShort && Position >= 0 && AllowShortEntries)
			EnterShort();
	}

	private void CloseLong()
	{
		if (Position <= 0)
			return;

		CancelActiveOrders();
		SellMarket(Position);
	}

	private void CloseShort()
	{
		if (Position >= 0)
			return;

		CancelActiveOrders();
		BuyMarket(-Position);
	}

	private void EnterLong()
	{
		var volume = OrderVolume + Math.Abs(Position);
		if (volume <= 0)
			return;

		CancelActiveOrders();
		BuyMarket(volume);
	}

	private void EnterShort()
	{
		var volume = OrderVolume + Math.Abs(Position);
		if (volume <= 0)
			return;

		CancelActiveOrders();
		SellMarket(volume);
	}

	private readonly record struct IbsRsiCciValue(decimal Up, decimal Down);

	private sealed class IbsRsiCciCalculator
	{
		private const decimal KoefIbs = 7m;
		private const decimal KoefRsi = 9m;
		private const decimal KoefCci = 1m;
		private const decimal Kibs = -1m;
		private const decimal Kcci = -1m;
		private const decimal Krsi = -1m;
		private const decimal Posit = -1m;

		private readonly int _ibsPeriod;
		private readonly AppliedPriceType _rsiPrice;
		private readonly AppliedPriceType _cciPrice;
		private readonly decimal _threshold;
		private readonly decimal _priceStep;
		private readonly LengthIndicator<decimal> _ibsMa;
		private readonly RelativeStrengthIndex _rsi;
		private readonly CommodityChannelIndexCalculator _cci;
		private readonly Highest _highest;
		private readonly Lowest _lowest;
		private readonly LengthIndicator<decimal> _rangeHighMa;
		private readonly LengthIndicator<decimal> _rangeLowMa;

		private decimal? _previousUp;

		public IbsRsiCciCalculator(
			int ibsPeriod,
			IbsMovingAverageType ibsType,
			int rsiPeriod,
			AppliedPriceType rsiPrice,
			int cciPeriod,
			AppliedPriceType cciPrice,
			decimal threshold,
			int rangePeriod,
			int smoothPeriod,
			decimal priceStep)
		{
			_ibsPeriod = ibsPeriod;
			_rsiPrice = rsiPrice;
			_cciPrice = cciPrice;
			_threshold = threshold;
			_priceStep = priceStep;

			_ibsMa = CreateMovingAverage(ibsType, ibsPeriod);
			_rsi = new RelativeStrengthIndex { Length = rsiPeriod };
			_cci = new CommodityChannelIndexCalculator(cciPeriod);
			_highest = new Highest { Length = rangePeriod };
			_lowest = new Lowest { Length = rangePeriod };
			_rangeHighMa = CreateMovingAverage(IbsMovingAverageType.Smoothed, smoothPeriod);
			_rangeLowMa = CreateMovingAverage(IbsMovingAverageType.Smoothed, smoothPeriod);
		}

		public IbsRsiCciValue? Process(ICandleMessage candle)
		{
			var range = Math.Abs(candle.HighPrice - candle.LowPrice);
			if (range == 0m)
				range = _priceStep;

			if (range == 0m)
				return null;

			var ibsRaw = (candle.ClosePrice - candle.LowPrice) / range;
			var ibsValue = _ibsMa.Process(ibsRaw, candle.OpenTime, true);
			if (!ibsValue.IsFinal)
				return null;

			var rsiInput = GetPrice(candle, _rsiPrice);
			var rsiValue = _rsi.Process(rsiInput, candle.OpenTime, true);
			if (!rsiValue.IsFinal)
				return null;

			var cciInput = GetPrice(candle, _cciPrice);
			var cciValue = _cci.Process(cciInput, candle.OpenTime, true);
			if (cciValue == null)
				return null;

			var ibs = ibsValue.GetValue<decimal>();
			var rsi = rsiValue.GetValue<decimal>();
			var cci = cciValue.Value;

			var sum = 0m;
			sum += Kibs * (ibs - 0.5m) * 100m * KoefIbs;
			sum += Kcci * cci * KoefCci;
			sum += Krsi * (rsi - 50m) * KoefRsi;
			sum /= 3m;

			var target = Posit * sum;
			var up = _previousUp ?? target;
			var diff = target - up;

			if (Math.Abs(diff) > _threshold)
			{
				if (diff > 0m)
					up = target - _threshold;
				else
					up = target + _threshold;
			}
			else
			{
				up = target;
			}

			_previousUp = up;

			var highestValue = _highest.Process(up, candle.OpenTime, true);
			var lowestValue = _lowest.Process(up, candle.OpenTime, true);
			if (!highestValue.IsFinal || !lowestValue.IsFinal)
				return null;

			var highest = highestValue.GetValue<decimal>();
			var lowest = lowestValue.GetValue<decimal>();

			var highSmooth = _rangeHighMa.Process(highest, candle.OpenTime, true);
			var lowSmooth = _rangeLowMa.Process(lowest, candle.OpenTime, true);
			if (!highSmooth.IsFinal || !lowSmooth.IsFinal)
				return null;

			var upBand = highSmooth.GetValue<decimal>();
			var lowBand = lowSmooth.GetValue<decimal>();
			var signal = (upBand + lowBand) / 2m;

			return new IbsRsiCciValue(up, signal);
		}

		public void Reset()
		{
			_previousUp = null;
			_ibsMa.Reset();
			_rsi.Reset();
			_cci.Reset();
			_highest.Reset();
			_lowest.Reset();
			_rangeHighMa.Reset();
			_rangeLowMa.Reset();
		}

		private static LengthIndicator<decimal> CreateMovingAverage(IbsMovingAverageType type, int length)
		{
			return type switch
			{
				IbsMovingAverageType.Simple => new SimpleMovingAverage { Length = length },
				IbsMovingAverageType.Exponential => new ExponentialMovingAverage { Length = length },
				IbsMovingAverageType.Weighted => new WeightedMovingAverage { Length = length },
				IbsMovingAverageType.Smoothed => new SmoothedMovingAverage { Length = length },
				_ => new SimpleMovingAverage { Length = length }
			};
		}

		private static decimal GetPrice(ICandleMessage candle, AppliedPriceType type)
		{
			return type switch
			{
				AppliedPriceType.Close => candle.ClosePrice,
				AppliedPriceType.Open => candle.OpenPrice,
				AppliedPriceType.High => candle.HighPrice,
				AppliedPriceType.Low => candle.LowPrice,
				AppliedPriceType.Median => (candle.HighPrice + candle.LowPrice) / 2m,
				AppliedPriceType.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
				AppliedPriceType.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice + candle.ClosePrice) / 4m,
				_ => candle.ClosePrice
			};
		}
	}

	public enum IbsMovingAverageType
	{
		Simple,
		Exponential,
		Smoothed,
		Weighted
	}

	public enum AppliedPriceType
	{
		Close,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted
	}

	private sealed class CommodityChannelIndexCalculator
	{
		private readonly int _period;
		private readonly SimpleMovingAverage _sma;
		private readonly Queue<decimal> _buffer = new();

		public CommodityChannelIndexCalculator(int period)
		{
			_period = period;
			_sma = new SimpleMovingAverage { Length = period };
		}

		public decimal? Process(decimal price, DateTimeOffset time, bool isFinal)
		{
			var maValue = _sma.Process(price, time, isFinal);
			_buffer.Enqueue(price);
			if (_buffer.Count > _period)
				_buffer.Dequeue();

			if (!maValue.IsFinal || _buffer.Count < _period)
				return null;

			var ma = maValue.GetValue<decimal>();
			decimal sum = 0m;
			foreach (var value in _buffer)
				sum += Math.Abs(value - ma);

			if (sum == 0m)
				return 0m;

			var meanDeviation = sum / _period;
			if (meanDeviation == 0m)
				return 0m;

			var cci = (price - ma) / (0.015m * meanDeviation);
			return cci;
		}

		public void Reset()
		{
			_buffer.Clear();
			_sma.Reset();
		}
	}
}
