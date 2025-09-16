using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Stochastic-based martingale strategy that scales positions using pip thresholds.
/// </summary>
public class MartingailExpertStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<decimal> _stepPips;
	private readonly StrategyParam<decimal> _profitPips;
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
	/// Initial order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Martingale multiplier for averaging orders.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Distance between averaging orders in pips.
	/// </summary>
	public decimal StepPips
	{
		get => _stepPips.Value;
		set => _stepPips.Value = value;
	}

	/// <summary>
	/// Profit target distance in pips.
	/// </summary>
	public decimal ProfitPips
	{
		get => _profitPips.Value;
		set => _profitPips.Value = value;
	}

	/// <summary>
	/// Base lookback period for the stochastic oscillator.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing period for the %D signal line.
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
	/// Stochastic threshold to allow long entries.
	/// </summary>
	public decimal BuyLevel
	{
		get => _buyLevel.Value;
		set => _buyLevel.Value = value;
	}

	/// <summary>
	/// Stochastic threshold to allow short entries.
	/// </summary>
	public decimal SellLevel
	{
		get => _sellLevel.Value;
		set => _sellLevel.Value = value;
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
	/// Initializes <see cref="MartingailExpertStrategy"/>.
	/// </summary>
	public MartingailExpertStrategy()
	{
		_volume = Param(nameof(Volume), 0.03m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Base order volume", "Trading");

		_multiplier = Param(nameof(Multiplier), 1.6m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Martingale multiplier for averaging", "Trading");

		_stepPips = Param(nameof(StepPips), 25m)
			.SetGreaterThanZero()
			.SetDisplay("Step (pips)", "Distance between martingale orders in pips", "Risk");

		_profitPips = Param(nameof(ProfitPips), 9m)
			.SetGreaterThanZero()
			.SetDisplay("Profit (pips)", "Pip distance required to close or add", "Risk");

		_kPeriod = Param(nameof(KPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("K Period", "Lookback length for %K", "Indicator");

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("D Period", "Smoothing length for %D", "Indicator");

		_slowing = Param(nameof(Slowing), 3)
			.SetGreaterThanZero()
			.SetDisplay("Slowing", "Smoothing applied to %K", "Indicator");

		_buyLevel = Param(nameof(BuyLevel), 20m)
			.SetDisplay("Buy Level", "%D level required for longs", "Indicator");

		_sellLevel = Param(nameof(SellLevel), 55m)
			.SetDisplay("Sell Level", "%D level required for shorts", "Indicator");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for processing", "General");
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

		if (TryClosePositions(candle))
		{
			_prevK = currentK;
			_prevD = currentD;
			return;
		}

		if (Position > 0 && _lastDirection == Sides.Buy && _lastEntryPrice != null)
		{
			HandleBuyScaling(candle);
		}
		else if (Position < 0 && _lastDirection == Sides.Sell && _lastEntryPrice != null)
		{
			HandleSellScaling(candle);
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
					BuyMarket(volume);
				}
			}
			else if (sellSignal)
			{
				var volume = NormalizeVolume(Volume);
				if (volume > 0m)
				{
					SellMarket(volume);
				}
			}
		}

		_prevK = currentK;
		_prevD = currentD;
	}

	private bool TryClosePositions(ICandleMessage candle)
	{
		if (_lastEntryPrice == null || _lastDirection == null)
			return false;

		var profitDistance = GetProfitDistance();
		if (profitDistance <= 0m)
			return false;

		if (_lastDirection == Sides.Buy && Position > 0)
		{
			var target = _lastEntryPrice.Value + profitDistance;
			if (candle.HighPrice >= target)
			{
				ClosePosition();
				return true;
			}
		}
		else if (_lastDirection == Sides.Sell && Position < 0)
		{
			var target = _lastEntryPrice.Value - profitDistance;
			if (candle.LowPrice <= target)
			{
				ClosePosition();
				return true;
			}
		}

		return false;
	}

	private void HandleBuyScaling(ICandleMessage candle)
	{
		var profitDistance = GetProfitDistance();
		var count = Math.Max(1, _positionCount);

		if (profitDistance > 0m)
		{
			var addTrigger = _lastEntryPrice!.Value + profitDistance * count;
			if (candle.HighPrice >= addTrigger)
			{
				var volume = NormalizeVolume(Volume);
				if (volume > 0m)
				{
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
					BuyMarket(volume);
				}
			}
		}
	}

	private void HandleSellScaling(ICandleMessage candle)
	{
		var profitDistance = GetProfitDistance();
		var count = Math.Max(1, _positionCount);

		if (profitDistance > 0m)
		{
			var addTrigger = _lastEntryPrice!.Value - profitDistance * count;
			if (candle.LowPrice <= addTrigger)
			{
				var volume = NormalizeVolume(Volume);
				if (volume > 0m)
				{
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

	private decimal GetProfitDistance()
	{
		if (_pipSize <= 0m)
			_pipSize = CalculatePipSize();

		return ProfitPips > 0m ? ProfitPips * _pipSize : 0m;
	}

	private decimal GetStepDistance()
	{
		if (_pipSize <= 0m)
			_pipSize = CalculatePipSize();

		return StepPips > 0m ? StepPips * _pipSize : 0m;
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
