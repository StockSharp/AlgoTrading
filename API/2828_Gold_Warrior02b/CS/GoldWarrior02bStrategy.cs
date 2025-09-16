using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader GoldWarrior02b expert advisor adapted for StockSharp.
/// Combines CCI, an impulse gauge and a ZigZag swing detector to trade near the end of 15 minute blocks.
/// </summary>
public class GoldWarrior02bStrategy : Strategy
{
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _trailingStepPoints;
	private readonly StrategyParam<int> _impulsePeriod;
	private readonly StrategyParam<int> _zigZagDepth;
	private readonly StrategyParam<decimal> _zigZagDeviation;
	private readonly StrategyParam<int> _zigZagBackstep;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<decimal> _impulseSellThreshold;
	private readonly StrategyParam<decimal> _impulseBuyThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private CommodityChannelIndex _cci = null!;
	private ImpulseIndicator _impulse = null!;

	private decimal? _lastZigZag;
	private decimal? _previousZigZag;
	private int _searchDirection;
	private decimal? _currentExtreme;
	private int _barsSinceExtreme;

	private decimal _previousCci;
	private decimal _previousImpulse;
	private bool _hasPreviousCci;
	private bool _hasPreviousImpulse;

	private DateTimeOffset _lastTradeTime;
	private decimal _entryPrice;
	private decimal _trailingStopPrice;
	private bool _trailingActive;
	private decimal _maxPriceSinceEntry;
	private decimal _minPriceSinceEntry;

	/// <summary>
	/// Base trading volume.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Additional offset before activating the trailing stop.
	/// </summary>
	public decimal TrailingStepPoints
	{
		get => _trailingStepPoints.Value;
		set => _trailingStepPoints.Value = value;
	}

	/// <summary>
	/// Period used both for CCI and impulse calculations.
	/// </summary>
	public int ImpulsePeriod
	{
		get => _impulsePeriod.Value;
		set => _impulsePeriod.Value = value;
	}

	/// <summary>
	/// Minimum bars between ZigZag turning points.
	/// </summary>
	public int ZigZagDepth
	{
		get => _zigZagDepth.Value;
		set => _zigZagDepth.Value = value;
	}

	/// <summary>
	/// Minimum price deviation to confirm a new ZigZag swing.
	/// </summary>
	public decimal ZigZagDeviation
	{
		get => _zigZagDeviation.Value;
		set => _zigZagDeviation.Value = value;
	}

	/// <summary>
	/// Minimum number of bars before accepting a new swing.
	/// </summary>
	public int ZigZagBackstep
	{
		get => _zigZagBackstep.Value;
		set => _zigZagBackstep.Value = value;
	}

	/// <summary>
	/// Profit target that forces an early exit from open positions.
	/// </summary>
	public decimal ProfitTarget
	{
		get => _profitTarget.Value;
		set => _profitTarget.Value = value;
	}

	/// <summary>
	/// Threshold applied to the impulse gauge before opening shorts.
	/// </summary>
	public decimal ImpulseSellThreshold
	{
		get => _impulseSellThreshold.Value;
		set => _impulseSellThreshold.Value = value;
	}

	/// <summary>
	/// Threshold applied to the impulse gauge before opening longs.
	/// </summary>
	public decimal ImpulseBuyThreshold
	{
		get => _impulseBuyThreshold.Value;
		set => _impulseBuyThreshold.Value = value;
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
	/// Initializes a new instance of the strategy.
	/// </summary>
	public GoldWarrior02bStrategy()
	{
		_baseVolume = Param(nameof(BaseVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Base trade size", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 100m)
		.SetGreaterOrEqual(0m)
		.SetDisplay("Stop Loss", "Stop-loss distance in points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 150m)
		.SetGreaterOrEqual(0m)
		.SetDisplay("Take Profit", "Take-profit distance in points", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 5m)
		.SetGreaterOrEqual(0m)
		.SetDisplay("Trailing Stop", "Trailing stop distance in points", "Risk");

		_trailingStepPoints = Param(nameof(TrailingStepPoints), 5m)
		.SetGreaterOrEqual(0m)
		.SetDisplay("Trailing Step", "Extra distance before trailing activates", "Risk");

		_impulsePeriod = Param(nameof(ImpulsePeriod), 21)
		.SetGreaterThanZero()
		.SetDisplay("Impulse Period", "Period for CCI and impulse averages", "Indicators");

		_zigZagDepth = Param(nameof(ZigZagDepth), 12)
		.SetGreaterThanZero()
		.SetDisplay("ZigZag Depth", "Minimum bars between swings", "Indicators");

		_zigZagDeviation = Param(nameof(ZigZagDeviation), 5m)
		.SetGreaterThanZero()
		.SetDisplay("ZigZag Deviation", "Required price move in points", "Indicators");

		_zigZagBackstep = Param(nameof(ZigZagBackstep), 3)
		.SetGreaterThanZero()
		.SetDisplay("ZigZag Backstep", "Bars before confirming a new swing", "Indicators");

		_profitTarget = Param(nameof(ProfitTarget), 300m)
		.SetGreaterOrEqual(0m)
		.SetDisplay("Profit Target", "Close all profit in account currency", "Risk");

		_impulseSellThreshold = Param(nameof(ImpulseSellThreshold), -30m)
		.SetDisplay("Impulse Sell", "Impulse threshold for shorts", "Indicators");

		_impulseBuyThreshold = Param(nameof(ImpulseBuyThreshold), 30m)
		.SetDisplay("Impulse Buy", "Impulse threshold for longs", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Working timeframe", "General");
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

		_cci?.Reset();
		_impulse?.Reset();

		_lastZigZag = null;
		_previousZigZag = null;
		_searchDirection = 1;
		_currentExtreme = null;
		_barsSinceExtreme = 0;

		_previousCci = 0m;
		_previousImpulse = 0m;
		_hasPreviousCci = false;
		_hasPreviousImpulse = false;

		_lastTradeTime = DateTimeOffset.MinValue;
		ResetPositionState();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = BaseVolume;

		_cci = new CommodityChannelIndex { Length = ImpulsePeriod };
		_impulse = new ImpulseIndicator
		{
			Length = ImpulsePeriod,
			PriceStep = GetPriceStep()
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_cci, _impulse, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _cci);
			DrawIndicator(area, _impulse);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue, decimal impulseValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		_impulse.PriceStep = GetPriceStep();

		if (!_cci.IsFormed || !_impulse.IsFormed)
		{
			_previousCci = cciValue;
			_previousImpulse = impulseValue;
			_hasPreviousCci = true;
			_hasPreviousImpulse = true;
			UpdateZigZag(candle);
			return;
		}

		UpdateZigZag(candle);

		var hasZigZag = _lastZigZag.HasValue && _previousZigZag.HasValue;
		var zigZagUp = hasZigZag && _lastZigZag.Value > _previousZigZag.Value;
		var zigZagDown = hasZigZag && _lastZigZag.Value < _previousZigZag.Value;

		if (!_hasPreviousCci || !_hasPreviousImpulse)
		{
			_previousCci = cciValue;
			_previousImpulse = impulseValue;
			_hasPreviousCci = true;
			_hasPreviousImpulse = true;
			return;
		}

		var now = candle.CloseTime;
		if ((now - _lastTradeTime).TotalSeconds < 15)
		{
			_previousCci = cciValue;
			_previousImpulse = impulseValue;
			return;
		}

		var sellCondition1 = cciValue < _previousCci && _previousCci > 50m && cciValue > 30m && impulseValue < 0m && _previousImpulse > 0m;
		var sellCondition2 = cciValue > 200m && _previousCci > cciValue && impulseValue > ImpulseSellThreshold && _previousImpulse > impulseValue;
		var buyCondition1 = cciValue > _previousCci && _previousCci < -50m && cciValue < -30m && impulseValue > 0m && _previousImpulse < 0m;
		var buyCondition2 = cciValue < -200m && _previousCci < cciValue && impulseValue < ImpulseBuyThreshold && _previousImpulse < impulseValue;

		var sellSignal = hasZigZag && zigZagUp && (sellCondition1 || sellCondition2);
		var buySignal = hasZigZag && zigZagDown && (buyCondition1 || buyCondition2);

		var lowerThreshold = Math.Min(ImpulseBuyThreshold, ImpulseSellThreshold);
		var upperThreshold = Math.Max(ImpulseBuyThreshold, ImpulseSellThreshold);
		if (!hasZigZag || Position != 0 || (_previousImpulse > lowerThreshold && _previousImpulse < upperThreshold))
		{
			sellSignal = false;
			buySignal = false;
		}

		if (Position == 0 && AllowEntryTime(now))
		{
			if (sellSignal)
			OpenShort(candle, BaseVolume);
			else if (buySignal)
			OpenLong(candle, BaseVolume);
		}

		if (Position != 0)
		{
			HandleActivePosition(candle, now);
		}

		_previousCci = cciValue;
		_previousImpulse = impulseValue;
	}

	private void HandleActivePosition(ICandleMessage candle, DateTimeOffset now)
	{
		var step = GetPriceStep();
		var stepPrice = GetStepPrice(step);

		var stopLossDistance = StopLossPoints * step;
		var takeProfitDistance = TakeProfitPoints * step;
		var trailingStopDistance = TrailingStopPoints * step;
		var trailingStepDistance = TrailingStepPoints * step;

		if (Position > 0)
		{
			_maxPriceSinceEntry = Math.Max(_maxPriceSinceEntry, candle.HighPrice);

			if (stopLossDistance > 0m && candle.LowPrice <= _entryPrice - stopLossDistance)
			{
				SellMarket(Position);
				_lastTradeTime = now;
				ResetPositionState();
				return;
			}

			if (takeProfitDistance > 0m && candle.HighPrice >= _entryPrice + takeProfitDistance)
			{
				SellMarket(Position);
				_lastTradeTime = now;
				ResetPositionState();
				return;
			}

			if (trailingStopDistance > 0m)
			{
				var move = candle.ClosePrice - _entryPrice;
				if (move >= trailingStopDistance + trailingStepDistance)
				{
					var newTrail = candle.ClosePrice - trailingStopDistance;
					if (!_trailingActive || newTrail > _trailingStopPrice)
					{
						_trailingStopPrice = newTrail;
						_trailingActive = true;
					}
				}

				if (_trailingActive && candle.LowPrice <= _trailingStopPrice)
				{
					SellMarket(Position);
					_lastTradeTime = now;
					ResetPositionState();
					return;
				}
			}
		}
		else if (Position < 0)
		{
			_minPriceSinceEntry = Math.Min(_minPriceSinceEntry, candle.LowPrice);

			if (stopLossDistance > 0m && candle.HighPrice >= _entryPrice + stopLossDistance)
			{
				BuyMarket(-Position);
				_lastTradeTime = now;
				ResetPositionState();
				return;
			}

			if (takeProfitDistance > 0m && candle.LowPrice <= _entryPrice - takeProfitDistance)
			{
				BuyMarket(-Position);
				_lastTradeTime = now;
				ResetPositionState();
				return;
			}

			if (trailingStopDistance > 0m)
			{
				var move = _entryPrice - candle.ClosePrice;
				if (move >= trailingStopDistance + trailingStepDistance)
				{
					var newTrail = candle.ClosePrice + trailingStopDistance;
					if (!_trailingActive || newTrail < _trailingStopPrice)
					{
						_trailingStopPrice = newTrail;
						_trailingActive = true;
					}
				}

				if (_trailingActive && candle.HighPrice >= _trailingStopPrice)
				{
					BuyMarket(-Position);
					_lastTradeTime = now;
					ResetPositionState();
					return;
				}
			}
		}

		var currentPnL = CalculateOpenPnL(candle.ClosePrice, step, stepPrice);
		if (ProfitTarget > 0m && currentPnL >= ProfitTarget)
		{
			if (Position > 0)
			SellMarket(Position);
			else if (Position < 0)
			BuyMarket(-Position);

			_lastTradeTime = now;
			ResetPositionState();
			return;
		}
	}

	private decimal CalculateOpenPnL(decimal closePrice, decimal step, decimal stepPrice)
	{
		if (Position == 0)
		return 0m;

		if (step <= 0m)
		step = 1m;
		if (stepPrice <= 0m)
		stepPrice = step;

		if (Position > 0)
		{
			var diff = closePrice - _entryPrice;
			return diff / step * stepPrice * Position;
		}
		else
		{
			var diff = _entryPrice - closePrice;
			return diff / step * stepPrice * -Position;
		}
	}

	private void OpenLong(ICandleMessage candle, decimal volume)
	{
		BuyMarket(volume);
		_entryPrice = candle.ClosePrice;
		_maxPriceSinceEntry = candle.ClosePrice;
		_minPriceSinceEntry = candle.ClosePrice;
		_trailingActive = false;
		_trailingStopPrice = 0m;
		_lastTradeTime = candle.CloseTime;
	}

	private void OpenShort(ICandleMessage candle, decimal volume)
	{
		SellMarket(volume);
		_entryPrice = candle.ClosePrice;
		_maxPriceSinceEntry = candle.ClosePrice;
		_minPriceSinceEntry = candle.ClosePrice;
		_trailingActive = false;
		_trailingStopPrice = 0m;
		_lastTradeTime = candle.CloseTime;
	}

	private void ResetPositionState()
	{
		_entryPrice = 0m;
		_maxPriceSinceEntry = 0m;
		_minPriceSinceEntry = 0m;
		_trailingActive = false;
		_trailingStopPrice = 0m;
	}

	private bool AllowEntryTime(DateTimeOffset time)
	{
		var minute = time.Minute;
		var second = time.Second;
		return minute % 15 == 14 && second >= 45;
	}

	private void UpdateZigZag(ICandleMessage candle)
	{
		var step = GetPriceStep();
		var deviation = ZigZagDeviation * step;
		var minBars = Math.Max(1, Math.Max(ZigZagDepth, ZigZagBackstep));

		if (_currentExtreme is null)
		{
			_currentExtreme = _searchDirection > 0 ? candle.HighPrice : candle.LowPrice;
			_barsSinceExtreme = 0;
			return;
		}

		if (_searchDirection > 0)
		{
			if (candle.HighPrice > _currentExtreme.Value)
			{
				_currentExtreme = candle.HighPrice;
				_barsSinceExtreme = 0;
			}
			else
			{
				_barsSinceExtreme++;
			}

			var drop = _currentExtreme.Value - candle.LowPrice;
			if (drop >= deviation && _barsSinceExtreme >= minBars)
			{
				_previousZigZag = _lastZigZag;
				_lastZigZag = _currentExtreme;
				_searchDirection = -1;
				_currentExtreme = candle.LowPrice;
				_barsSinceExtreme = 0;
			}
		}
		else
		{
			if (candle.LowPrice < _currentExtreme.Value)
			{
				_currentExtreme = candle.LowPrice;
				_barsSinceExtreme = 0;
			}
			else
			{
				_barsSinceExtreme++;
			}

			var rise = candle.HighPrice - _currentExtreme.Value;
			if (rise >= deviation && _barsSinceExtreme >= minBars)
			{
				_previousZigZag = _lastZigZag;
				_lastZigZag = _currentExtreme;
				_searchDirection = 1;
				_currentExtreme = candle.HighPrice;
				_barsSinceExtreme = 0;
			}
		}
	}

	private decimal GetPriceStep()
	{
		var step = Security?.PriceStep ?? 1m;
		return step > 0m ? step : 1m;
	}

	private decimal GetStepPrice(decimal step)
	{
		var stepPrice = Security?.StepPrice ?? step;
		return stepPrice > 0m ? stepPrice : step;
	}

	private sealed class ImpulseIndicator : Indicator<ICandleMessage>
	{
		public int Length { get; set; } = 21;
		public decimal PriceStep { get; set; } = 1m;

		private readonly Queue<decimal> _buffer = new();
		private decimal _sum;

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();
			var step = PriceStep > 0m ? PriceStep : 1m;
			var value = (candle.OpenPrice - candle.ClosePrice) / step;

			_buffer.Enqueue(value);
			_sum += value;

			if (_buffer.Count > Length)
			_sum -= _buffer.Dequeue();

			if (_buffer.Count < Length)
			{
				IsFormed = false;
				return new DecimalIndicatorValue(this, 0m, input.Time);
			}

			IsFormed = true;
			var average = _sum / Length;
			return new DecimalIndicatorValue(this, average, input.Time);
		}

		public override void Reset()
		{
			base.Reset();
			_buffer.Clear();
			_sum = 0m;
		}
	}
}
