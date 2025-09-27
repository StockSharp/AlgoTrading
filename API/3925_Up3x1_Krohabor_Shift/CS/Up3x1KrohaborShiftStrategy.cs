using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triple moving-average crossover strategy converted from the MetaTrader expert advisor "up3x1_Krohabor_D".
/// </summary>
public class Up3x1KrohaborShiftStrategy : Strategy
{
		private readonly StrategyParam<int> _fastPeriod;
		private readonly StrategyParam<int> _mediumPeriod;
		private readonly StrategyParam<int> _slowPeriod;
		private readonly StrategyParam<int> _maShift;
		private readonly StrategyParam<decimal> _minVolume;
		private readonly StrategyParam<decimal> _lossReductionFactor;
		private readonly StrategyParam<decimal> _stopLossPoints;
		private readonly StrategyParam<decimal> _takeProfitPoints;
		private readonly StrategyParam<decimal> _trailingPoints;
		private readonly StrategyParam<DataType> _candleType;

		private readonly List<decimal> _fastHistory = new();
		private readonly List<decimal> _mediumHistory = new();
		private readonly List<decimal> _slowHistory = new();

		private decimal _priceStep;
		private decimal? _entryPrice;
		private Sides? _entrySide;
		private decimal? _stopPrice;
		private decimal? _takePrice;
		private int _consecutiveLosses;

		/// <summary>
		/// Initializes a new instance of <see cref="Up3x1KrohaborShiftStrategy"/>.
		/// </summary>
		public Up3x1KrohaborShiftStrategy()
		{
				_fastPeriod = Param(nameof(FastPeriod), 24)
						.SetRange(1, int.MaxValue)
						.SetDisplay("Fast MA", "Length of the fast moving average", "Indicator");

				_mediumPeriod = Param(nameof(MediumPeriod), 60)
						.SetRange(1, int.MaxValue)
						.SetDisplay("Medium MA", "Length of the medium moving average", "Indicator");

				_slowPeriod = Param(nameof(SlowPeriod), 120)
						.SetRange(1, int.MaxValue)
						.SetDisplay("Slow MA", "Length of the slow moving average", "Indicator");

				_maShift = Param(nameof(MaShift), 6)
						.SetRange(0, 100)
						.SetDisplay("Indicator Shift", "Number of completed bars used to shift all moving averages", "Indicator");


				_minVolume = Param(nameof(MinVolume), 0.1m)
						.SetGreaterThanZero()
						.SetDisplay("Minimum Volume", "Lower bound applied after loss-based volume reduction", "Trading");

				_lossReductionFactor = Param(nameof(LossReductionFactor), 3m)
						.SetGreaterThanZero()
						.SetDisplay("Loss Reduction Factor", "Divisor applied when reducing the volume after consecutive losses", "Risk");

				_stopLossPoints = Param(nameof(StopLossPoints), 110m)
						.SetNotNegative()
						.SetDisplay("Stop Loss (points)", "Initial protective stop distance expressed in price steps", "Risk");

				_takeProfitPoints = Param(nameof(TakeProfitPoints), 5m)
						.SetNotNegative()
						.SetDisplay("Take Profit (points)", "Initial take-profit distance expressed in price steps", "Risk");

				_trailingPoints = Param(nameof(TrailingPoints), 10m)
						.SetNotNegative()
						.SetDisplay("Trailing (points)", "Trailing distance activated once the position is in profit", "Risk");

				_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
						.SetDisplay("Candle Type", "Market data type used for calculations", "General");
		}

		public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
		public int MediumPeriod { get => _mediumPeriod.Value; set => _mediumPeriod.Value = value; }
		public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
		public int MaShift { get => _maShift.Value; set => _maShift.Value = value; }
		public decimal MinVolume { get => _minVolume.Value; set => _minVolume.Value = value; }
		public decimal LossReductionFactor { get => _lossReductionFactor.Value; set => _lossReductionFactor.Value = value; }
		public decimal StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }
		public decimal TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }
		public decimal TrailingPoints { get => _trailingPoints.Value; set => _trailingPoints.Value = value; }
		public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
				return [(Security, CandleType)];
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
				base.OnStarted(time);

				_priceStep = Security?.Step ?? 0.0001m;
				ResetPositionState();
				_consecutiveLosses = 0;
				_fastHistory.Clear();
				_mediumHistory.Clear();
				_slowHistory.Clear();

				var fastMa = new SimpleMovingAverage { Length = FastPeriod };
				var mediumMa = new SimpleMovingAverage { Length = MediumPeriod };
				var slowMa = new SimpleMovingAverage { Length = SlowPeriod };

				var subscription = SubscribeCandles(CandleType);
				subscription
						.Bind(fastMa, mediumMa, slowMa, ProcessCandle)
						.Start();

				var area = CreateChartArea();
				if (area != null)
				{
						DrawCandles(area, subscription);
						DrawIndicator(area, fastMa);
						DrawIndicator(area, mediumMa);
						DrawIndicator(area, slowMa);
						DrawOwnTrades(area);
				}

				StartProtection();
		}

		private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal mediumValue, decimal slowValue)
		{
				if (candle.State != CandleStates.Finished)
						return;

				UpdateHistory(_fastHistory, fastValue);
				UpdateHistory(_mediumHistory, mediumValue);
				UpdateHistory(_slowHistory, slowValue);

				if (!TryGetShiftedValue(_fastHistory, MaShift, out var fastCurrent) ||
						!TryGetShiftedValue(_fastHistory, MaShift + 1, out var fastPrevious) ||
						!TryGetShiftedValue(_mediumHistory, MaShift, out var mediumCurrent) ||
						!TryGetShiftedValue(_mediumHistory, MaShift + 1, out var mediumPrevious) ||
						!TryGetShiftedValue(_slowHistory, MaShift, out var slowCurrent) ||
						!TryGetShiftedValue(_slowHistory, MaShift + 1, out var slowPrevious))
				{
						return;
				}

				if (Position == 0m)
				{
						TryOpenPosition(candle, fastPrevious, fastCurrent, mediumPrevious, mediumCurrent, slowPrevious, slowCurrent);
						return;
				}

				ManageOpenPosition(candle, fastPrevious, fastCurrent, mediumPrevious, mediumCurrent, slowPrevious, slowCurrent);
		}

		private void TryOpenPosition(
				ICandleMessage candle,
				decimal fastPrevious,
				decimal fastCurrent,
				decimal mediumPrevious,
				decimal mediumCurrent,
				decimal slowPrevious,
				decimal slowCurrent)
		{
				var buySignal = slowPrevious < mediumCurrent &&
						slowPrevious < mediumPrevious &&
						slowPrevious < fastCurrent &&
						slowPrevious < fastPrevious &&
						slowCurrent < mediumCurrent &&
						slowCurrent < mediumPrevious &&
						slowCurrent < fastCurrent &&
						slowCurrent < fastPrevious &&
						mediumPrevious > fastPrevious &&
						mediumCurrent < fastCurrent;

				if (buySignal)
				{
						var volume = CalculateVolume();
						if (volume > 0m)
						{
								BuyMarket(volume);
								InitializePositionState(Sides.Buy, candle.ClosePrice);
						}
						return;
				}

				var sellSignal = slowPrevious > mediumCurrent &&
						slowPrevious > mediumPrevious &&
						slowPrevious > fastCurrent &&
						slowPrevious > fastPrevious &&
						slowCurrent > mediumCurrent &&
						slowCurrent > mediumPrevious &&
						slowCurrent > fastCurrent &&
						slowCurrent > fastPrevious &&
						mediumPrevious < fastPrevious &&
						mediumCurrent > fastCurrent;

				if (sellSignal)
				{
						var volume = CalculateVolume();
						if (volume > 0m)
						{
								SellMarket(volume);
								InitializePositionState(Sides.Sell, candle.ClosePrice);
						}
				}
		}

		private void ManageOpenPosition(
				ICandleMessage candle,
				decimal fastPrevious,
				decimal fastCurrent,
				decimal mediumPrevious,
				decimal mediumCurrent,
				decimal slowPrevious,
				decimal slowCurrent)
		{
				if (_entryPrice is null || _entrySide is null)
						return;

				if (_entrySide == Sides.Buy)
				{
						var exitByMa = fastPrevious > mediumPrevious &&
								mediumPrevious > slowPrevious &&
								slowCurrent < fastCurrent &&
								fastCurrent < mediumCurrent;

						var trailingDistance = TrailingPoints * _priceStep;

						if (trailingDistance > 0m)
						{
								var profit = candle.ClosePrice - _entryPrice.Value;
								if (profit >= trailingDistance)
								{
										var newStop = candle.ClosePrice - trailingDistance;
										if (_stopPrice is null || newStop > _stopPrice.Value)
												_stopPrice = newStop;
								}
						}

						if (StopLossPoints > 0m)
								_stopPrice ??= _entryPrice.Value - StopLossPoints * _priceStep;

						if (TakeProfitPoints > 0m)
								_takePrice ??= _entryPrice.Value + TakeProfitPoints * _priceStep;

						if (_stopPrice is decimal stop && candle.LowPrice <= stop)
						{
								SellMarket(Position);
								FinalizePosition(stop);
								return;
						}

						if (_takePrice is decimal take && candle.HighPrice >= take)
						{
								SellMarket(Position);
								FinalizePosition(take);
								return;
						}

						if (exitByMa)
						{
								SellMarket(Position);
								FinalizePosition(candle.ClosePrice);
						}

						return;
				}

				var exitByMaShort = fastPrevious < mediumPrevious &&
						mediumPrevious < slowPrevious &&
						slowCurrent > fastCurrent &&
						fastCurrent > mediumCurrent;

				var trailingDistanceShort = TrailingPoints * _priceStep;

				if (trailingDistanceShort > 0m)
				{
						var profitShort = _entryPrice.Value - candle.ClosePrice;
						if (profitShort >= trailingDistanceShort)
						{
								var newStopShort = candle.ClosePrice + trailingDistanceShort;
								if (_stopPrice is null || newStopShort < _stopPrice.Value)
										_stopPrice = newStopShort;
						}
				}

				if (StopLossPoints > 0m)
						_stopPrice ??= _entryPrice.Value + StopLossPoints * _priceStep;

				if (TakeProfitPoints > 0m)
						_takePrice ??= _entryPrice.Value - TakeProfitPoints * _priceStep;

				if (_stopPrice is decimal stopShort && candle.HighPrice >= stopShort)
				{
						BuyMarket(-Position);
						FinalizePosition(stopShort);
						return;
				}

				if (_takePrice is decimal takeShort && candle.LowPrice <= takeShort)
				{
						BuyMarket(-Position);
						FinalizePosition(takeShort);
						return;
				}

				if (exitByMaShort)
				{
						BuyMarket(-Position);
						FinalizePosition(candle.ClosePrice);
				}
		}

		private decimal CalculateVolume()
	{
		var volume = Volume;

		if (_consecutiveLosses > 1 && LossReductionFactor > 0m)
		{
			var reduction = volume * _consecutiveLosses / LossReductionFactor;
			volume -= reduction;
		}

		if (volume < MinVolume)
			volume = MinVolume;

		return volume;
	}

		private void InitializePositionState(Sides side, decimal price)
	{
		_entrySide = side;
		_entryPrice = price;
		_stopPrice = null;
		_takePrice = null;
	}

		private void FinalizePosition(decimal exitPrice)
	{
		if (_entryPrice is decimal entry && _entrySide is Sides side)
		{
			var direction = side == Sides.Buy ? 1m : -1m;
			var profit = (exitPrice - entry) * direction;
			_consecutiveLosses = profit < 0m ? _consecutiveLosses + 1 : 0;
		}
		else
		{
			_consecutiveLosses = 0;
		}

		ResetPositionState();
	}

		private void ResetPositionState()
	{
		_entrySide = null;
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
	}

		private void UpdateHistory(List<decimal> history, decimal value)
	{
		history.Add(value);

		var maxSize = Math.Max(MaShift + 2, 8);
		if (history.Count > maxSize)
			history.RemoveAt(0);
	}

		private static bool TryGetShiftedValue(IReadOnlyList<decimal> history, int shift, out decimal value)
	{
		var index = history.Count - 1 - shift;
		if (index < 0)
		{
			value = default;
			return false;
		}

		value = history[index];
		return true;
	}
}

