	using System;
	using System.Collections.Generic;

	using StockSharp.Algo.Strategies;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	namespace StockSharp.Samples.Strategies;

	public class ExpXPeriodCandleStrategy : Strategy
	{
		public enum SmoothingMethod
		{
			Simple,
			Exponential,
			Smoothed,
			LinearWeighted,
			JurikLike,
			JurxLike,
			ParabolicLike,
			TillsonT3Like,
			VidyaLike,
			AdaptiveLike
		}

		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _period;
		private readonly StrategyParam<SmoothingMethod> _smoothingMethod;
		private readonly StrategyParam<int> _smoothingLength;
		private readonly StrategyParam<int> _smoothingPhase;
		private readonly StrategyParam<int> _signalBar;
		private readonly StrategyParam<bool> _enableLongEntry;
		private readonly StrategyParam<bool> _enableShortEntry;
		private readonly StrategyParam<bool> _enableLongExit;
		private readonly StrategyParam<bool> _enableShortExit;
		private readonly StrategyParam<int> _stopLossPoints;
		private readonly StrategyParam<int> _takeProfitPoints;
		private readonly StrategyParam<int> _slippagePoints;

		private Smoother? _openSmoother;
		private Smoother? _highSmoother;
		private Smoother? _lowSmoother;
		private Smoother? _closeSmoother;

		private readonly List<int> _colorHistory = new();
		private readonly Queue<decimal> _smoothedHighs = new();
		private readonly Queue<decimal> _smoothedLows = new();

		public ExpXPeriodCandleStrategy()
		{
			_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
				.SetDisplay("Candle Type", "Time frame used for calculations", "General");

			_period = Param(nameof(Period), 5)
				.SetGreaterThanZero()
				.SetDisplay("Smoothing Window", "Depth of the price smoothing window", "Indicator")
				.SetCanOptimize(true);

			_smoothingMethod = Param(nameof(SmoothingMethod), SmoothingMethod.JurikLike)
				.SetDisplay("Smoothing Method", "Type of moving average approximation", "Indicator");

			_smoothingLength = Param(nameof(SmoothingLength), 3)
				.SetGreaterThanZero()
				.SetDisplay("Smoothing Length", "Length used by the smoother", "Indicator")
				.SetCanOptimize(true);

			_smoothingPhase = Param(nameof(SmoothingPhase), 100)
				.SetDisplay("Smoothing Phase", "Phase parameter for adaptive smoothers", "Indicator");

			_signalBar = Param(nameof(SignalBar), 1)
				.SetGreaterThanZero()
				.SetDisplay("Signal Shift", "Which completed candle to evaluate", "Trading");

			_enableLongEntry = Param(nameof(EnableLongEntry), true)
				.SetDisplay("Enable Long Entry", "Allow opening buy positions", "Trading");

			_enableShortEntry = Param(nameof(EnableShortEntry), true)
				.SetDisplay("Enable Short Entry", "Allow opening sell positions", "Trading");

			_enableLongExit = Param(nameof(EnableLongExit), true)
				.SetDisplay("Close Longs On Opposite", "Close long positions on opposite signals", "Trading");

			_enableShortExit = Param(nameof(EnableShortExit), true)
				.SetDisplay("Close Shorts On Opposite", "Close short positions on opposite signals", "Trading");

			_stopLossPoints = Param(nameof(StopLossPoints), 1000)
				.SetGreaterOrEqualZero()
				.SetDisplay("Stop Loss (pts)", "Protective stop loss in price points", "Risk");

			_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000)
				.SetGreaterOrEqualZero()
				.SetDisplay("Take Profit (pts)", "Protective take profit in price points", "Risk");

			_slippagePoints = Param(nameof(SlippagePoints), 10)
				.SetGreaterOrEqualZero()
				.SetDisplay("Slippage (pts)", "Allowed slippage in price points", "Trading");
		}

		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		public int Period
		{
			get => _period.Value;
			set => _period.Value = value;
		}

		public SmoothingMethod Smoothing
		{
			get => _smoothingMethod.Value;
			set => _smoothingMethod.Value = value;
		}

		public int SmoothingLength
		{
			get => _smoothingLength.Value;
			set => _smoothingLength.Value = value;
		}

		public int SmoothingPhase
		{
			get => _smoothingPhase.Value;
			set => _smoothingPhase.Value = value;
		}

		public int SignalBar
		{
			get => _signalBar.Value;
			set => _signalBar.Value = value;
		}

		public bool EnableLongEntry
		{
			get => _enableLongEntry.Value;
			set => _enableLongEntry.Value = value;
		}

		public bool EnableShortEntry
		{
			get => _enableShortEntry.Value;
			set => _enableShortEntry.Value = value;
		}

		public bool EnableLongExit
		{
			get => _enableLongExit.Value;
			set => _enableLongExit.Value = value;
		}

		public bool EnableShortExit
		{
			get => _enableShortExit.Value;
			set => _enableShortExit.Value = value;
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

		public int SlippagePoints
		{
			get => _slippagePoints.Value;
			set => _slippagePoints.Value = value;
		}

		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return [(Security, CandleType)];
		}

		protected override void OnReseted()
		{
			base.OnReseted();

			_colorHistory.Clear();
			_smoothedHighs.Clear();
			_smoothedLows.Clear();
			_openSmoother = null;
			_highSmoother = null;
			_lowSmoother = null;
			_closeSmoother = null;
		}

		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			_openSmoother = CreateSmoother(Smoothing, SmoothingLength, SmoothingPhase);
			_highSmoother = CreateSmoother(Smoothing, SmoothingLength, SmoothingPhase);
			_lowSmoother = CreateSmoother(Smoothing, SmoothingLength, SmoothingPhase);
			_closeSmoother = CreateSmoother(Smoothing, SmoothingLength, SmoothingPhase);

			_colorHistory.Clear();
			_smoothedHighs.Clear();
			_smoothedLows.Clear();

			ApplySlippage();
			ApplyProtection();

			var subscription = SubscribeCandles(CandleType);
			subscription.WhenNew(ProcessCandle).Start();

			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;

			var openValue = _openSmoother?.Process(candle.OpenPrice);
			var highValue = _highSmoother?.Process(candle.HighPrice);
			var lowValue = _lowSmoother?.Process(candle.LowPrice);
			var closeValue = _closeSmoother?.Process(candle.ClosePrice);

			if (openValue is null || highValue is null || lowValue is null || closeValue is null)
				return;

			UpdateQueue(_smoothedHighs, highValue.Value, Period);
			UpdateQueue(_smoothedLows, lowValue.Value, Period);

			if (_smoothedHighs.Count < Period || _smoothedLows.Count < Period)
				return;


			var color = openValue.Value <= closeValue.Value ? 0 : 2;
			_colorHistory.Add(color);
			var maxHistory = Math.Max(Period * 4, SignalBar + 4);
			if (_colorHistory.Count > maxHistory)
				_colorHistory.RemoveAt(0);

			if (!IsFormed)
			{
				if (_colorHistory.Count >= SignalBar + 1)
					IsFormed = true;
				else
					return;
			}

			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			if (_colorHistory.Count <= SignalBar)
				return;

			var index0 = _colorHistory.Count - SignalBar;
			if (index0 >= _colorHistory.Count)
				index0 = _colorHistory.Count - 1;
			var index1 = index0 - 1;
			if (index1 < 0)
				return;

			var value0 = _colorHistory[index0];
			var value1 = _colorHistory[index1];

			var baseLongCondition = value1 < 1;
			var baseShortCondition = value1 > 1;
			var openLong = EnableLongEntry && baseLongCondition && value0 > 0;
			var openShort = EnableShortEntry && baseShortCondition && value0 < 2;
			var closeShort = EnableShortExit && baseLongCondition;
			var closeLong = EnableLongExit && baseShortCondition;

			if (closeLong && Position > 0)
				SellMarket(Position);

			if (closeShort && Position < 0)
				BuyMarket(-Position);

			if (openLong && Position <= 0)
			{
				var volume = Volume + (Position < 0 ? -Position : 0m);
				BuyMarket(volume);
			}
			else if (openShort && Position >= 0)
			{
				var volume = Volume + (Position > 0 ? Position : 0m);
				SellMarket(volume);
			}
		}

		private void ApplySlippage()
		{
			if (SlippagePoints <= 0)
				return;

			var step = Security?.PriceStep ?? 0m;
			var slippage = step > 0 ? step * SlippagePoints : SlippagePoints;
			Slippage = slippage;
		}

		private void ApplyProtection()
		{
			var step = Security?.PriceStep ?? 0m;
			var stop = StopLossPoints > 0 ? step > 0 ? step * StopLossPoints : StopLossPoints : (decimal?)null;
			var take = TakeProfitPoints > 0 ? step > 0 ? step * TakeProfitPoints : TakeProfitPoints : (decimal?)null;

			if (stop is null && take is null)
				return;

			StartProtection(
				stopLoss: stop is null ? null : new Unit(stop.Value, UnitTypes.Price),
				takeProfit: take is null ? null : new Unit(take.Value, UnitTypes.Price));
		}

		private static void UpdateQueue(Queue<decimal> queue, decimal value, int maxCount)
		{
			queue.Enqueue(value);
			if (queue.Count > maxCount)
				queue.Dequeue();
		}

		private static decimal GetMax(IEnumerable<decimal> source)
		{
			var max = decimal.MinValue;
			foreach (var value in source)
			{
				if (value > max)
					max = value;
			}
			return max;
		}

		private static decimal GetMin(IEnumerable<decimal> source)
		{
			var min = decimal.MaxValue;
			foreach (var value in source)
			{
				if (value < min)
					min = value;
			}
			return min;
		}

		private static Smoother CreateSmoother(SmoothingMethod method, int length, int phase)
		{
			switch (method)
			{
				case SmoothingMethod.Simple:
					return new SmaSmoother(length);
				case SmoothingMethod.Exponential:
					return new EmaSmoother(length);
				case SmoothingMethod.Smoothed:
					return new SmmaSmoother(length);
				case SmoothingMethod.LinearWeighted:
					return new LwmaSmoother(length);
				default:
					// Approximate advanced smoothing modes (JJMA, JurX, Parabolic, T3, VIDYA, AMA) with EMA.
					return new EmaSmoother(length);
			}
		}

		private abstract class Smoother
		{
			protected Smoother(int length)
			{
				Length = Math.Max(1, length);
			}

			protected int Length { get; }

			public abstract decimal? Process(decimal value);
		}

		private sealed class SmaSmoother : Smoother
		{
			private readonly Queue<decimal> _values = new();
			private decimal _sum;

			public SmaSmoother(int length)
				: base(length)
			{
			}

			public override decimal? Process(decimal value)
			{
				_values.Enqueue(value);
				_sum += value;

				if (_values.Count > Length)
				{
					_sum -= _values.Dequeue();
				}

				if (_values.Count < Length)
					return null;

				return _sum / _values.Count;
			}
		}

		private sealed class EmaSmoother : Smoother
		{
			private decimal? _ema;
			private readonly decimal _alpha;

			public EmaSmoother(int length)
				: base(length)
			{
				_alpha = 2m / (Length + 1m);
			}

			public override decimal? Process(decimal value)
			{
				if (_ema is null)
					_ema = value;
				else
					_ema += _alpha * (value - _ema.Value);

				return _ema;
			}
		}

		private sealed class SmmaSmoother : Smoother
		{
			private decimal? _smma;

			public SmmaSmoother(int length)
				: base(length)
			{
			}

			public override decimal? Process(decimal value)
			{
				if (_smma is null)
					_smma = value;
				else
					_smma = ((_smma.Value * (Length - 1)) + value) / Length;

				return _smma;
			}
		}

		private sealed class LwmaSmoother : Smoother
		{
			private readonly Queue<decimal> _values = new();

			public LwmaSmoother(int length)
				: base(length)
			{
			}

			public override decimal? Process(decimal value)
			{
				_values.Enqueue(value);
				if (_values.Count > Length)
					_values.Dequeue();

				if (_values.Count < Length)
					return null;

				var weightSum = 0m;
				var weightedTotal = 0m;
				var weight = 1m;

				foreach (var item in _values)
				{
					weightedTotal += item * weight;
					weightSum += weight;
					weight += 1m;
				}

				return weightedTotal / weightSum;
			}
		}
	}
