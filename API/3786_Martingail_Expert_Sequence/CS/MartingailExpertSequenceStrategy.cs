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
/// Martingale strategy driven by stochastic oscillator crossovers.
/// </summary>
public class MartingailExpertSequenceStrategy : Strategy
{
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<decimal> _stepPoints;
	private readonly StrategyParam<decimal> _profitFactor;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _slowing;
	private readonly StrategyParam<decimal> _buyLevel;
	private readonly StrategyParam<decimal> _sellLevel;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private decimal? _lastEntryPrice;
	private decimal _lastVolume;
	private Sides? _lastDirection;
	private int _positionCount;
	private decimal? _prevK;
	private decimal? _prevD;


	/// <summary>
	/// Multiplication factor for martingale averaging.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Distance between averaging orders in points.
	/// </summary>
	public decimal StepPoints
	{
		get => _stepPoints.Value;
		set => _stepPoints.Value = value;
	}

	/// <summary>
	/// Profit factor multiplied by the number of open orders.
	/// </summary>
	public decimal ProfitFactor
	{
		get => _profitFactor.Value;
		set => _profitFactor.Value = value;
	}

	/// <summary>
	/// Lookback length for the stochastic oscillator.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// Signal line length for the stochastic oscillator.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing applied to the %K line.
	/// </summary>
	public int Slowing
	{
		get => _slowing.Value;
		set => _slowing.Value = value;
	}

	/// <summary>
	/// Threshold that allows new long entries.
	/// </summary>
	public decimal BuyLevel
	{
		get => _buyLevel.Value;
		set => _buyLevel.Value = value;
	}

	/// <summary>
	/// Threshold that allows new short entries.
	/// </summary>
	public decimal SellLevel
	{
		get => _sellLevel.Value;
		set => _sellLevel.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="MartingailExpertSequenceStrategy"/>.
	/// </summary>
	public MartingailExpertSequenceStrategy()
	{

		_multiplier = Param(nameof(Multiplier), 1.6m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Factor for martingale averaging", "Trading");

		_stepPoints = Param(nameof(StepPoints), 25m)
			.SetGreaterThanZero()
			.SetDisplay("Step (points)", "Distance before averaging", "Risk");

		_profitFactor = Param(nameof(ProfitFactor), 9m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Factor", "Multiplier for dynamic take profit", "Risk");

		_kPeriod = Param(nameof(KPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("%K Period", "Lookback for stochastic", "Indicator");

		_dPeriod = Param(nameof(DPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("%D Period", "Smoothing length for %D", "Indicator");

		_slowing = Param(nameof(Slowing), 6)
			.SetGreaterThanZero()
			.SetDisplay("Slowing", "Additional smoothing for %K", "Indicator");

		_buyLevel = Param(nameof(BuyLevel), 50m)
			.SetDisplay("Buy Level", "Minimum %D level for longs", "Indicator");

		_sellLevel = Param(nameof(SellLevel), 50m)
			.SetDisplay("Sell Level", "Maximum %D level for shorts", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");
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

		_pipSize = 0m;
		_lastEntryPrice = null;
		_lastVolume = 0m;
		_lastDirection = null;
		_positionCount = 0;
		_prevK = null;
		_prevD = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		var stochastic = new StochasticOscillator
		{
			Length = KPeriod,
			K = { Length = Slowing },
			D = { Length = DPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!stochValue.IsFinal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (stochValue is not StochasticOscillatorValue stoch)
			return;

		if (stoch.K is not decimal currentK || stoch.D is not decimal currentD)
			return;

		var currentCount = Math.Max(1, _positionCount);
		if (TryClosePositions(candle, currentCount))
		{
			_prevK = currentK;
			_prevD = currentD;
			return;
		}

		if (Position > 0 && _lastDirection == Sides.Buy && _lastEntryPrice != null)
		{
			HandleBuyScaling(candle, currentCount);
		}
		else if (Position < 0 && _lastDirection == Sides.Sell && _lastEntryPrice != null)
		{
			HandleSellScaling(candle, currentCount);
		}

		if (Position == 0 && _positionCount == 0 && _prevK.HasValue && _prevD.HasValue)
		{
			var buySignal = _prevK.Value > _prevD.Value && _prevD.Value > BuyLevel;
			var sellSignal = _prevK.Value < _prevD.Value && _prevD.Value < SellLevel;

			if (buySignal)
			{
				var volume = NormalizeVolume(Volume);
				if (volume > 0m)
				{
					// Enter new long sequence when %K crosses above %D above the buy level.
					BuyMarket(volume);
				}
			}
			else if (sellSignal)
			{
				var volume = NormalizeVolume(Volume);
				if (volume > 0m)
				{
					// Enter new short sequence when %K crosses below %D below the sell level.
					SellMarket(volume);
				}
			}
		}

		_prevK = currentK;
		_prevD = currentD;
	}

	private bool TryClosePositions(ICandleMessage candle, int count)
	{
		if (_lastEntryPrice == null || _lastDirection == null)
			return false;

		var profitDistance = GetProfitDistance(count);
		if (profitDistance <= 0m)
			return false;

		if (_lastDirection == Sides.Buy && Position > 0)
		{
			var target = _lastEntryPrice.Value + profitDistance;
			if (candle.HighPrice >= target)
			{
				// Close all long positions once the dynamic profit target is reached.
				ClosePosition();
				return true;
			}
		}
		else if (_lastDirection == Sides.Sell && Position < 0)
		{
			var target = _lastEntryPrice.Value - profitDistance;
			if (candle.LowPrice <= target)
			{
				// Close all short positions once the dynamic profit target is reached.
				ClosePosition();
				return true;
			}
		}

		return false;
	}

	private void HandleBuyScaling(ICandleMessage candle, int count)
	{
		var profitDistance = GetProfitDistance(count);
		if (profitDistance > 0m)
		{
			var addTrigger = _lastEntryPrice!.Value + profitDistance;
			if (candle.HighPrice >= addTrigger)
			{
				var volume = NormalizeVolume(Volume);
				if (volume > 0m)
				{
					// Add base volume when price advances by the target distance.
					BuyMarket(volume);
				}
				return;
			}
		}

		var stepDistance = GetStepDistance();
		if (stepDistance > 0m)
		{
			var martingaleTrigger = _lastEntryPrice!.Value - stepDistance;
			if (candle.LowPrice <= martingaleTrigger)
			{
				var volume = NormalizeVolume(_lastVolume * Multiplier);
				if (volume > 0m)
				{
					// Increase exposure after an adverse move by the configured step size.
					BuyMarket(volume);
				}
			}
		}
	}

	private void HandleSellScaling(ICandleMessage candle, int count)
	{
		var profitDistance = GetProfitDistance(count);
		if (profitDistance > 0m)
		{
			var addTrigger = _lastEntryPrice!.Value - profitDistance;
			if (candle.LowPrice <= addTrigger)
			{
				var volume = NormalizeVolume(Volume);
				if (volume > 0m)
				{
					// Add base volume when price declines by the target distance.
					SellMarket(volume);
				}
				return;
			}
		}

		var stepDistance = GetStepDistance();
		if (stepDistance > 0m)
		{
			var martingaleTrigger = _lastEntryPrice!.Value + stepDistance;
			if (candle.HighPrice >= martingaleTrigger)
			{
				var volume = NormalizeVolume(_lastVolume * Multiplier);
				if (volume > 0m)
				{
					// Increase exposure after an adverse move by the configured step size.
					SellMarket(volume);
				}
			}
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (trade.Order == null)
			return;

		var side = trade.Order.Side;
		var volume = trade.Trade.Volume;
		var price = trade.Trade.Price;

		if (side == Sides.Buy)
		{
			if (Position > 0)
			{
				if (_lastDirection != Sides.Buy)
					_positionCount = 0;

				_lastDirection = Sides.Buy;
				_lastEntryPrice = price;
				_lastVolume = volume;
				_positionCount++;
			}
			else if (Position == 0 && _lastDirection == Sides.Sell)
			{
				ResetPositionState();
			}
		}
		else if (side == Sides.Sell)
		{
			if (Position < 0)
			{
				if (_lastDirection != Sides.Sell)
					_positionCount = 0;

				_lastDirection = Sides.Sell;
				_lastEntryPrice = price;
				_lastVolume = volume;
				_positionCount++;
			}
			else if (Position == 0 && _lastDirection == Sides.Buy)
			{
				ResetPositionState();
			}
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
			ResetPositionState();
	}

	private decimal GetProfitDistance(int count)
	{
		if (_pipSize <= 0m)
			_pipSize = CalculatePipSize();

		if (ProfitFactor <= 0m)
			return 0m;

		return ProfitFactor * count * _pipSize;
	}

	private decimal GetStepDistance()
	{
		if (_pipSize <= 0m)
			_pipSize = CalculatePipSize();

		return StepPoints > 0m ? StepPoints * _pipSize : 0m;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			step = 0.0001m;

		var current = step;
		var digits = 0;

		while (current < 1m && digits < 10)
		{
			current *= 10m;
			digits++;
		}

		return digits == 3 || digits == 5 ? step * 10m : step;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var step = Security?.VolumeStep ?? 0m;
		if (step > 0m)
		{
			var steps = Math.Floor(volume / step);
			volume = steps * step;
		}

		return volume > 0m ? volume : 0m;
	}

	private void ResetPositionState()
	{
		_lastEntryPrice = null;
		_lastVolume = 0m;
		_lastDirection = null;
		_positionCount = 0;
	}
}

