using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class ExpFineTuningMaCandleStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _rank1;
	private readonly StrategyParam<decimal> _rank2;
	private readonly StrategyParam<decimal> _rank3;
	private readonly StrategyParam<decimal> _shift1;
	private readonly StrategyParam<decimal> _shift2;
	private readonly StrategyParam<decimal> _shift3;
	private readonly StrategyParam<decimal> _gapPoints;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _priceShiftPoints;

	private FineTuningMaCandleIndicator _indicator;
	private readonly List<decimal> _colorHistory = new();
	private Order _stopOrder;
	private Order _takeOrder;
	private Sides? _pendingEntryDirection;
	private Sides? _pendingProtectionDirection;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	public decimal Rank1
	{
		get => _rank1.Value;
		set => _rank1.Value = value;
	}

	public decimal Rank2
	{
		get => _rank2.Value;
		set => _rank2.Value = value;
	}

	public decimal Rank3
	{
		get => _rank3.Value;
		set => _rank3.Value = value;
	}

	public decimal Shift1
	{
		get => _shift1.Value;
		set => _shift1.Value = value;
	}

	public decimal Shift2
	{
		get => _shift2.Value;
		set => _shift2.Value = value;
	}

	public decimal Shift3
	{
		get => _shift3.Value;
		set => _shift3.Value = value;
	}

	public decimal GapPoints
	{
		get => _gapPoints.Value;
		set => _gapPoints.Value = value;
	}

	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	public bool BuyPosOpen
	{
		get => _buyPosOpen.Value;
		set => _buyPosOpen.Value = value;
	}

	public bool SellPosOpen
	{
		get => _sellPosOpen.Value;
		set => _sellPosOpen.Value = value;
	}

	public bool BuyPosClose
	{
		get => _buyPosClose.Value;
		set => _buyPosClose.Value = value;
	}

	public bool SellPosClose
	{
		get => _sellPosClose.Value;
		set => _sellPosClose.Value = value;
	}

	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	public decimal PriceShiftPoints
	{
		get => _priceShiftPoints.Value;
		set => _priceShiftPoints.Value = value;
	}

	public ExpFineTuningMaCandleStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for the indicator", "General");

		_length = Param(nameof(Length), 10)
			.SetDisplay("Length", "Number of candles in the weighted window", "Indicator")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(5, 40, 1);

		_rank1 = Param(nameof(Rank1), 2m)
			.SetDisplay("Rank #1", "Weight curvature parameter for the first stage", "Indicator");

		_rank2 = Param(nameof(Rank2), 2m)
			.SetDisplay("Rank #2", "Weight curvature parameter for the second stage", "Indicator");

		_rank3 = Param(nameof(Rank3), 2m)
			.SetDisplay("Rank #3", "Weight curvature parameter for the symmetric stage", "Indicator");

		_shift1 = Param(nameof(Shift1), 1m)
			.SetDisplay("Shift #1", "Initial blend factor for the first stage", "Indicator");

		_shift2 = Param(nameof(Shift2), 1m)
			.SetDisplay("Shift #2", "Initial blend factor for the second stage", "Indicator");

		_shift3 = Param(nameof(Shift3), 1m)
			.SetDisplay("Shift #3", "Initial blend factor for the symmetric stage", "Indicator");

		_gapPoints = Param(nameof(GapPoints), 10m)
			.SetDisplay("Gap Points", "Maximum gap that is replaced by the previous close", "Indicator")
			.SetNotNegative();

		_signalBar = Param(nameof(SignalBar), 1)
			.SetDisplay("Signal Bar", "How many closed candles to skip before generating a signal", "Trading")
			.SetGreaterThanZero();

		_buyPosOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Buy Open", "Allow opening long positions", "Trading");

		_sellPosOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Sell Open", "Allow opening short positions", "Trading");

		_buyPosClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Buy Close", "Allow closing long positions on bearish signals", "Trading");

		_sellPosClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Sell Close", "Allow closing short positions on bullish signals", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
			.SetDisplay("Stop Loss", "Protective stop distance expressed in points", "Risk")
			.SetNotNegative();

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
			.SetDisplay("Take Profit", "Profit target distance expressed in points", "Risk")
			.SetNotNegative();

		_priceShiftPoints = Param(nameof(PriceShiftPoints), 0m)
			.SetDisplay("Price Shift", "Vertical displacement of the synthetic candle", "Indicator");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_indicator?.Reset();
		_colorHistory.Clear();
		CancelProtection();
		_pendingEntryDirection = null;
		_pendingProtectionDirection = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_indicator = new FineTuningMaCandleIndicator
		{
			Length = Length,
			Rank1 = Rank1,
			Rank2 = Rank2,
			Rank3 = Rank3,
			Shift1 = Shift1,
			Shift2 = Shift2,
			Shift3 = Shift3,
			Gap = ConvertPointsToPrice(GapPoints),
			PriceShift = ConvertPointsToPrice(PriceShiftPoints),
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(_indicator, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
	{
		if (!indicatorValue.IsFinal || candle.State != CandleStates.Finished)
			return;

		if (!_indicator.IsFormed)
			return;

		var value = (FineTuningMaCandleValue)indicatorValue;
		var color = value.Color;

		_colorHistory.Add(color);

		var maxHistory = Math.Max(SignalBar + 2, 3);
		if (_colorHistory.Count > maxHistory)
			_colorHistory.RemoveRange(0, _colorHistory.Count - maxHistory);

		var offset = Math.Max(SignalBar, 1);
		var signalIndex = _colorHistory.Count - offset;
		if (signalIndex < 1 || signalIndex >= _colorHistory.Count)
			return;

		var previousIndex = signalIndex - 1;
		var currentColor = _colorHistory[signalIndex];
		var previousColor = _colorHistory[previousIndex];

		var buyOpen = BuyPosOpen && currentColor == 2m && previousColor != 2m;
		var sellOpen = SellPosOpen && currentColor == 0m && previousColor != 0m;
		var buyClose = BuyPosClose && currentColor == 0m;
		var sellClose = SellPosClose && currentColor == 2m;

		if (buyClose && Position > 0m)
		{
			CancelProtection();
			ClosePosition();
		}

		if (sellClose && Position < 0m)
		{
			CancelProtection();
			ClosePosition();
		}

		if (buyOpen)
		{
			if (Position < 0m)
			{
				if (SellPosClose)
				{
					CancelProtection();
					ClosePosition();
					_pendingEntryDirection = Sides.Buy;
				}
				return;
			}

			if (Position == 0m)
			{
				var volume = Volume;
				if (volume > 0m)
				{
					BuyMarket(volume);
					_pendingProtectionDirection = Sides.Buy;
					_pendingEntryDirection = null;
				}
			}
		}
		else if (sellOpen)
		{
			if (Position > 0m)
			{
				if (BuyPosClose)
				{
					CancelProtection();
					ClosePosition();
					_pendingEntryDirection = Sides.Sell;
				}
				return;
			}

			if (Position == 0m)
			{
				var volume = Volume;
				if (volume > 0m)
				{
					SellMarket(volume);
					_pendingProtectionDirection = Sides.Sell;
					_pendingEntryDirection = null;
				}
			}
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0m)
		{
			CancelProtection();

			if (_pendingEntryDirection is { } direction)
			{
				var volume = Volume;
				if (volume > 0m)
				{
					if (direction == Sides.Buy)
					{
						BuyMarket(volume);
						_pendingProtectionDirection = Sides.Buy;
					}
					else
					{
						SellMarket(volume);
						_pendingProtectionDirection = Sides.Sell;
					}
				}

				_pendingEntryDirection = null;
			}
		}
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (_stopOrder != null && order == _stopOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
			_stopOrder = null;

		if (_takeOrder != null && order == _takeOrder && order.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
			_takeOrder = null;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var order = trade.Order;
		if (order == null)
			return;

		if (_pendingProtectionDirection is not { } direction)
			return;

		var price = trade.Trade.Price;

		if (direction == Sides.Buy && order.Direction == Sides.Buy && Position > 0m)
		{
			RegisterProtection(true, Position, price);
			_pendingProtectionDirection = null;
		}
		else if (direction == Sides.Sell && order.Direction == Sides.Sell && Position < 0m)
		{
			RegisterProtection(false, Math.Abs(Position), price);
			_pendingProtectionDirection = null;
		}
	}

	private void RegisterProtection(bool isLong, decimal position, decimal entryPrice)
	{
		CancelProtection();

		if (position <= 0m)
			return;

		var stopDistance = ConvertPointsToPrice(StopLossPoints);
		var takeDistance = ConvertPointsToPrice(TakeProfitPoints);

		if (stopDistance > 0m)
		{
			var stopPrice = isLong ? entryPrice - stopDistance : entryPrice + stopDistance;
			stopPrice = AlignPrice(stopPrice);
			_stopOrder = isLong
				? SellStop(position, stopPrice)
				: BuyStop(position, stopPrice);
		}

		if (takeDistance > 0m)
		{
			var takePrice = isLong ? entryPrice + takeDistance : entryPrice - takeDistance;
			takePrice = AlignPrice(takePrice);
			_takeOrder = isLong
				? SellLimit(position, takePrice)
				: BuyLimit(position, takePrice);
		}
	}

	private void CancelProtection()
	{
		_pendingProtectionDirection = null;

		if (_stopOrder != null)
		{
			if (_stopOrder.State is OrderStates.Active or OrderStates.Pending)
				CancelOrder(_stopOrder);

			if (_stopOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled || _stopOrder.State is OrderStates.Active or OrderStates.Pending)
				_stopOrder = null;
		}

		if (_takeOrder != null)
		{
			if (_takeOrder.State is OrderStates.Active or OrderStates.Pending)
				CancelOrder(_takeOrder);

			if (_takeOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled || _takeOrder.State is OrderStates.Active or OrderStates.Pending)
				_takeOrder = null;
		}
	}

	private decimal ConvertPointsToPrice(decimal points)
	{
		if (points <= 0m)
			return 0m;

		var step = Security?.PriceStep;
		if (step == null || step.Value <= 0m)
			return points;

		return points * step.Value;
	}

	private decimal AlignPrice(decimal price)
	{
		return Security?.ShrinkPrice(price) ?? price;
	}

	private sealed class FineTuningMaCandleIndicator : Indicator<ICandleMessage>
	{
		private readonly List<decimal> _openValues = new();
		private readonly List<decimal> _highValues = new();
		private readonly List<decimal> _lowValues = new();
		private readonly List<decimal> _closeValues = new();

		private FineTuningMaCandleValue? _lastValue;
		private decimal[] _weights = Array.Empty<decimal>();
		private bool _weightsDirty = true;
		private decimal? _previousClose;

		public int Length { get; set; } = 10;
		public decimal Rank1 { get; set; } = 2m;
		public decimal Rank2 { get; set; } = 2m;
		public decimal Rank3 { get; set; } = 2m;
		public decimal Shift1 { get; set; } = 1m;
		public decimal Shift2 { get; set; } = 1m;
		public decimal Shift3 { get; set; } = 1m;
		public decimal Gap { get; set; } = 0m;
		public decimal PriceShift { get; set; } = 0m;

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();

			if (!input.IsFinal)
				return _lastValue ?? new FineTuningMaCandleValue(this, input, candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice, 1m);

			if (Length <= 0)
				throw new InvalidOperationException("Length must be greater than zero.");

			EnsureWeights();

			Append(_openValues, candle.OpenPrice);
			Append(_highValues, candle.HighPrice);
			Append(_lowValues, candle.LowPrice);
			Append(_closeValues, candle.ClosePrice);

			if (_openValues.Count < Length || _highValues.Count < Length || _lowValues.Count < Length || _closeValues.Count < Length)
			{
				IsFormed = false;
				_lastValue = new FineTuningMaCandleValue(this, input, candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice, 1m);
				return _lastValue;
			}

			var smoothOpen = WeightedSum(_openValues);
			var smoothHigh = WeightedSum(_highValues);
			var smoothLow = WeightedSum(_lowValues);
			var smoothClose = WeightedSum(_closeValues);

			var high = Math.Max(Math.Max(smoothOpen, smoothClose), Math.Max(smoothHigh, smoothLow));
			var low = Math.Min(Math.Min(smoothOpen, smoothClose), Math.Min(smoothHigh, smoothLow));

			if (Gap > 0m && _previousClose is { } prevClose && Math.Abs(smoothOpen - smoothClose) <= Gap)
				smoothOpen = prevClose;

			smoothOpen += PriceShift;
			smoothHigh = high + PriceShift;
			smoothLow = low + PriceShift;
			smoothClose += PriceShift;

			var color = smoothOpen < smoothClose ? 2m : smoothOpen > smoothClose ? 0m : 1m;

			_previousClose = smoothClose;
			IsFormed = true;
			_lastValue = new FineTuningMaCandleValue(this, input, smoothOpen, smoothHigh, smoothLow, smoothClose, color);
			return _lastValue;
		}

		public override void Reset()
		{
			base.Reset();

			_openValues.Clear();
			_highValues.Clear();
			_lowValues.Clear();
			_closeValues.Clear();
			_previousClose = null;
			_weights = Array.Empty<decimal>();
			_weightsDirty = true;
			_lastValue = null;
		}

		private void EnsureWeights()
		{
			if (!_weightsDirty && _weights.Length == Length)
				return;

			_weights = new decimal[Length];

			if (Length == 1)
			{
				_weights[0] = 1m;
				_weightsDirty = false;
				return;
			}

			var lenMinusOne = Length - 1m;
			var sum = 0m;

			for (var h = 0; h < Length; h++)
			{
				var ratio = h / lenMinusOne;
				var weight = Shift1 + (decimal)Math.Pow((double)ratio, (double)Rank1) * (1m - Shift1);
				weight *= Shift2 + (decimal)Math.Pow((double)(1m - ratio), (double)Rank2) * (1m - Shift2);

				decimal symmetric;
				if (ratio < 0.5m)
				{
					symmetric = 1m - 2m * ratio;
				}
				else
				{
					symmetric = 2m * ratio - 1m;
				}

				weight *= Shift3 + (decimal)Math.Pow((double)Math.Abs(symmetric), (double)Rank3) * (1m - Shift3);

				_weights[h] = weight;
				sum += weight;
			}

			if (sum == 0m)
				sum = 1m;

			for (var h = 0; h < Length; h++)
				_weights[h] /= sum;

			_weightsDirty = false;
		}

		private decimal WeightedSum(List<decimal> values)
		{
			decimal sum = 0m;
			var count = values.Count;

			for (var i = 0; i < Length; i++)
			{
				var index = count - 1 - i;
				sum += _weights[i] * values[index];
			}

			return sum;
		}

		private void Append(List<decimal> values, decimal value)
		{
			values.Add(value);
			if (values.Count > Length)
				values.RemoveAt(0);
		}
	}

	private sealed record FineTuningMaCandleValue : ComplexIndicatorValue
	{
		public FineTuningMaCandleValue(IIndicator indicator, IIndicatorValue input, decimal open, decimal high, decimal low, decimal close, decimal color)
			: base(indicator, input, (nameof(Open), open), (nameof(High), high), (nameof(Low), low), (nameof(Close), close), (nameof(Color), color))
		{
		}

		public decimal Open => (decimal)GetValue(nameof(Open));
		public decimal High => (decimal)GetValue(nameof(High));
		public decimal Low => (decimal)GetValue(nameof(Low));
		public decimal Close => (decimal)GetValue(nameof(Close));
		public decimal Color => (decimal)GetValue(nameof(Color));
	}
}

	private sealed class FineTuningMaCandleIndicator : Indicator<ICandleMessage>
	{
		private readonly List<decimal> _openValues = new();
		private readonly List<decimal> _highValues = new();
		private readonly List<decimal> _lowValues = new();
		private readonly List<decimal> _closeValues = new();

		private FineTuningMaCandleValue? _lastValue;
		private decimal[] _weights = Array.Empty<decimal>();
		private bool _weightsDirty = true;
		private decimal? _previousClose;

		public int Length { get; set; } = 10;
		public decimal Rank1 { get; set; } = 2m;
		public decimal Rank2 { get; set; } = 2m;
		public decimal Rank3 { get; set; } = 2m;
		public decimal Shift1 { get; set; } = 1m;
		public decimal Shift2 { get; set; } = 1m;
		public decimal Shift3 { get; set; } = 1m;
		public decimal Gap { get; set; } = 0m;
		public decimal PriceShift { get; set; } = 0m;

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();

			if (!input.IsFinal)
				return _lastValue ?? new FineTuningMaCandleValue(this, input, candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice, 1m);

			if (Length <= 0)
				throw new InvalidOperationException("Length must be greater than zero.");

			EnsureWeights();

			Append(_openValues, candle.OpenPrice);
			Append(_highValues, candle.HighPrice);
			Append(_lowValues, candle.LowPrice);
			Append(_closeValues, candle.ClosePrice);

			if (_openValues.Count < Length || _highValues.Count < Length || _lowValues.Count < Length || _closeValues.Count < Length)
			{
				IsFormed = false;
				_lastValue = new FineTuningMaCandleValue(this, input, candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice, 1m);
				return _lastValue;
			}

			var smoothOpen = WeightedSum(_openValues);
			var smoothHigh = WeightedSum(_highValues);
			var smoothLow = WeightedSum(_lowValues);
			var smoothClose = WeightedSum(_closeValues);

			var high = Math.Max(Math.Max(smoothOpen, smoothClose), Math.Max(smoothHigh, smoothLow));
			var low = Math.Min(Math.Min(smoothOpen, smoothClose), Math.Min(smoothHigh, smoothLow));

			if (Gap > 0m && _previousClose is { } prevClose && Math.Abs(smoothOpen - smoothClose) <= Gap)
				smoothOpen = prevClose;

			smoothOpen += PriceShift;
			smoothHigh = high + PriceShift;
			smoothLow = low + PriceShift;
			smoothClose += PriceShift;

			var color = smoothOpen < smoothClose ? 2m : smoothOpen > smoothClose ? 0m : 1m;

			_previousClose = smoothClose;
			IsFormed = true;
			_lastValue = new FineTuningMaCandleValue(this, input, smoothOpen, smoothHigh, smoothLow, smoothClose, color);
			return _lastValue;
		}

		public override void Reset()
		{
			base.Reset();

			_openValues.Clear();
			_highValues.Clear();
			_lowValues.Clear();
			_closeValues.Clear();
			_previousClose = null;
			_weights = Array.Empty<decimal>();
			_weightsDirty = true;
			_lastValue = null;
		}

		private void EnsureWeights()
		{
			if (!_weightsDirty && _weights.Length == Length)
				return;

			_weights = new decimal[Length];

			if (Length == 1)
			{
				_weights[0] = 1m;
				_weightsDirty = false;
				return;
			}

			var lenMinusOne = Length - 1m;
			var sum = 0m;

			for (var h = 0; h < Length; h++)
			{
				var ratio = h / lenMinusOne;
				var weight = Shift1 + (decimal)Math.Pow((double)ratio, (double)Rank1) * (1m - Shift1);
				weight *= Shift2 + (decimal)Math.Pow((double)(1m - ratio), (double)Rank2) * (1m - Shift2);

				decimal symmetric;
				if (ratio < 0.5m)
				{
					symmetric = 1m - 2m * ratio;
				}
				else
				{
					symmetric = 2m * ratio - 1m;
				}

				weight *= Shift3 + (decimal)Math.Pow((double)Math.Abs(symmetric), (double)Rank3) * (1m - Shift3);

				_weights[h] = weight;
				sum += weight;
			}

			if (sum == 0m)
				sum = 1m;

			for (var h = 0; h < Length; h++)
				_weights[h] /= sum;

			_weightsDirty = false;
		}

		private decimal WeightedSum(List<decimal> values)
		{
			decimal sum = 0m;
			var count = values.Count;

			for (var i = 0; i < Length; i++)
			{
				var index = count - 1 - i;
				sum += _weights[i] * values[index];
			}

			return sum;
		}

		private void Append(List<decimal> values, decimal value)
		{
			values.Add(value);
			if (values.Count > Length)
				values.RemoveAt(0);
		}
	}

	private sealed record FineTuningMaCandleValue : ComplexIndicatorValue
	{
		public FineTuningMaCandleValue(IIndicator indicator, IIndicatorValue input, decimal open, decimal high, decimal low, decimal close, decimal color)
			: base(indicator, input, (nameof(Open), open), (nameof(High), high), (nameof(Low), low), (nameof(Close), close), (nameof(Color), color))
		{
		}

		public decimal Open => (decimal)GetValue(nameof(Open));
		public decimal High => (decimal)GetValue(nameof(High));
		public decimal Low => (decimal)GetValue(nameof(Low));
		public decimal Close => (decimal)GetValue(nameof(Close));
		public decimal Color => (decimal)GetValue(nameof(Color));
	}
}
