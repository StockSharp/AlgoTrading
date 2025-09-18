using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Zone recovery strategy that alternates long and short cycles to recover losses.
/// </summary>
public class ZoneRecoveryFormulaStrategy : Strategy
{
	private readonly StrategyParam<bool> _useTakeProfitMoney;
	private readonly StrategyParam<decimal> _takeProfitMoney;
	private readonly StrategyParam<bool> _useTakeProfitPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<decimal> _trailingTakeProfit;
	private readonly StrategyParam<decimal> _trailingStopLoss;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _zoneRecoveryPips;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _profitOffsetPips;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pipSize;
	private decimal _takeProfitDistance;
	private decimal _recoveryDistance;
	private decimal[] _volumeSequence = Array.Empty<decimal>();

	private bool _cycleActive;
	private bool _cycleIsLong;
	private int _currentStep;
	private decimal _targetPrice;
	private decimal _recoveryBoundary;
	private decimal _maxOpenProfit;

	/// <summary>
	/// Indicates whether take profit in money should be used.
	/// </summary>
	public bool UseTakeProfitMoney
	{
		get => _useTakeProfitMoney.Value;
		set => _useTakeProfitMoney.Value = value;
	}

	/// <summary>
	/// Take profit amount in money.
	/// </summary>
	public decimal TakeProfitMoney
	{
		get => _takeProfitMoney.Value;
		set => _takeProfitMoney.Value = value;
	}

	/// <summary>
	/// Indicates whether take profit in percent should be used.
	/// </summary>
	public bool UseTakeProfitPercent
	{
		get => _useTakeProfitPercent.Value;
		set => _useTakeProfitPercent.Value = value;
	}

	/// <summary>
	/// Take profit value in percent of the account balance.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Enables trailing profit management.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Trailing activation threshold in money.
	/// </summary>
	public decimal TrailingTakeProfit
	{
		get => _trailingTakeProfit.Value;
		set => _trailingTakeProfit.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in money.
	/// </summary>
	public decimal TrailingStopLoss
	{
		get => _trailingStopLoss.Value;
		set => _trailingStopLoss.Value = value;
	}

	/// <summary>
	/// Take profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Zone recovery distance in pips.
	/// </summary>
	public decimal ZoneRecoveryPips
	{
		get => _zoneRecoveryPips.Value;
		set => _zoneRecoveryPips.Value = value;
	}

	/// <summary>
	/// Volume multiplier used for the first order.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	/// <summary>
	/// Maximum amount of trades in the recovery cycle.
	/// </summary>
	public int MaxTrades
	{
		get => _maxTrades.Value;
		set => _maxTrades.Value = value;
	}

	/// <summary>
	/// Fast moving average length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow moving average length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Offset used in the profit formula.
	/// </summary>
	public decimal ProfitOffsetPips
	{
		get => _profitOffsetPips.Value;
		set => _profitOffsetPips.Value = value;
	}

	/// <summary>
	/// The candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ZoneRecoveryFormulaStrategy"/> class.
	/// </summary>
	public ZoneRecoveryFormulaStrategy()
	{
		_useTakeProfitMoney = Param(nameof(UseTakeProfitMoney), false)
		.SetDisplay("Use TP Money", "Enable closing by monetary profit", "Risk");

		_takeProfitMoney = Param(nameof(TakeProfitMoney), 10m)
		.SetGreaterThanZero()
		.SetDisplay("TP Money", "Monetary profit target", "Risk");

		_useTakeProfitPercent = Param(nameof(UseTakeProfitPercent), false)
		.SetDisplay("Use TP %", "Enable closing by percent profit", "Risk");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 10m)
		.SetGreaterThanZero()
		.SetDisplay("TP Percent", "Percent profit target", "Risk");

		_enableTrailing = Param(nameof(EnableTrailing), true)
		.SetDisplay("Enable Trailing", "Use trailing money management", "Risk");

		_trailingTakeProfit = Param(nameof(TrailingTakeProfit), 40m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing TP", "Profit level to activate trailing", "Risk");

		_trailingStopLoss = Param(nameof(TrailingStopLoss), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Trailing SL", "Distance for trailing stop", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 150m)
		.SetGreaterThanZero()
		.SetDisplay("TP Pips", "Take profit distance in pips", "Recovery");

		_zoneRecoveryPips = Param(nameof(ZoneRecoveryPips), 50m)
		.SetGreaterThanZero()
		.SetDisplay("Zone Pips", "Zone recovery distance in pips", "Recovery");

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Base Volume", "Base order volume", "Recovery");

		_maxTrades = Param(nameof(MaxTrades), 11)
		.SetGreaterThanZero()
		.SetDisplay("Max Trades", "Maximum number of recovery steps", "Recovery");

		_fastLength = Param(nameof(FastLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("Fast MA", "Fast moving average length", "Signals");

		_slowLength = Param(nameof(SlowLength), 200)
		.SetGreaterThanZero()
		.SetDisplay("Slow MA", "Slow moving average length", "Signals");

		_profitOffsetPips = Param(nameof(ProfitOffsetPips), 0m)
		.SetDisplay("Profit Offset", "Offset in the profit formula", "Recovery");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Candles used for analysis", "General");
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

		_cycleActive = false;
		_cycleIsLong = false;
		_currentStep = 0;
		_targetPrice = 0m;
		_recoveryBoundary = 0m;
		_maxOpenProfit = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_pipSize = Security.PriceStep ?? 0.0001m;
		_takeProfitDistance = TakeProfitPips * _pipSize;
		_recoveryDistance = ZoneRecoveryPips * _pipSize;
		_volumeSequence = BuildVolumeSequence();

		var fastMa = new SMA { Length = FastLength };
		var slowMa = new SMA { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
		.Bind(fastMa, slowMa, (candle, fastValue, slowValue) =>
		{
			if (candle.State != CandleStates.Finished)
			return;

			if (!IsFormedAndOnlineAndAllowTrading())
			return;

			if (!fastMa.IsFormed || !slowMa.IsFormed)
			return;

			ProcessCandle(candle, fastValue, slowValue);
		})
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		var price = candle.ClosePrice;

		if (!_cycleActive)
		{
			if (fastValue > slowValue)
			{
				StartCycle(true, price);
			}
			else if (fastValue < slowValue)
			{
				StartCycle(false, price);
			}

			return;
		}

		ManageCycle(price);
	}

	private void StartCycle(bool isLong, decimal price)
	{
		_cycleActive = true;
		_cycleIsLong = isLong;
		_currentStep = 0;
		_maxOpenProfit = 0m;

		UpdateTargets(price);

		var volume = GetVolumeForStep(_currentStep);

		if (isLong)
		{
			BuyMarket(volume);
		}
		else
		{
			SellMarket(volume);
		}

		LogInfo($"Zone recovery cycle started. Direction={(isLong ? "Long" : "Short")}, Volume={volume}");
	}

	private void ManageCycle(decimal price)
	{
		if (Position == 0m)
		{
			ResetCycle();
			return;
		}

		if (HandleMoneyTargets(price))
		return;

		if (_cycleIsLong)
		{
			if (price >= _targetPrice)
			{
				ClosePosition();
				ResetCycle();
				return;
			}

			if (price <= _recoveryBoundary)
			{
				AdvanceRecovery(price);
			}
		}
		else
		{
			if (price <= _targetPrice)
			{
				ClosePosition();
				ResetCycle();
				return;
			}

			if (price >= _recoveryBoundary)
			{
				AdvanceRecovery(price);
			}
		}
	}

	private void AdvanceRecovery(decimal price)
	{
		if (_currentStep + 1 >= _volumeSequence.Length || _currentStep + 1 >= MaxTrades)
		{
			LogWarning("Max recovery steps reached. Waiting for profit target.");
			return;
		}

		_currentStep++;
		_cycleIsLong = !_cycleIsLong;

		var desiredVolume = GetVolumeForStep(_currentStep);

		if (_cycleIsLong)
		{
			var volumeToBuy = Math.Abs(Position) + desiredVolume;
			BuyMarket(volumeToBuy);
		}
		else
		{
			var volumeToSell = Math.Abs(Position) + desiredVolume;
			SellMarket(volumeToSell);
		}

		UpdateTargets(price);

		LogInfo($"Recovery step {_currentStep} activated. Direction={(_cycleIsLong ? "Long" : "Short")}, Target={_targetPrice}, Boundary={_recoveryBoundary}");
	}

	private bool HandleMoneyTargets(decimal price)
	{
		if (Position == 0m || PositionPrice is not decimal entry)
		return false;

		var unrealized = Position * (price - entry);

		if (EnableTrailing)
		{
			if (unrealized > _maxOpenProfit)
			_maxOpenProfit = unrealized;

			if (_maxOpenProfit >= TrailingTakeProfit && _maxOpenProfit - unrealized >= TrailingStopLoss)
			{
				ClosePosition();
				ResetCycle();
				return true;
			}
		}

		if (UseTakeProfitMoney && unrealized >= TakeProfitMoney)
		{
			ClosePosition();
			ResetCycle();
			return true;
		}

		if (UseTakeProfitPercent && Portfolio is not null && Portfolio.CurrentValue != 0m)
		{
			var target = Portfolio.CurrentValue * (TakeProfitPercent / 100m);
			if (unrealized >= target)
			{
				ClosePosition();
				ResetCycle();
				return true;
			}
		}

		return false;
	}

	private void UpdateTargets(decimal price)
	{
		var entry = PositionPrice ?? price;

		if (_cycleIsLong)
		{
			_targetPrice = entry + _takeProfitDistance;
			_recoveryBoundary = entry - _recoveryDistance;
		}
		else
		{
			_targetPrice = entry - _takeProfitDistance;
			_recoveryBoundary = entry + _recoveryDistance;
		}
	}

	private void ResetCycle()
	{
		_cycleActive = false;
		_cycleIsLong = false;
		_currentStep = 0;
		_targetPrice = 0m;
		_recoveryBoundary = 0m;
		_maxOpenProfit = 0m;
	}

	private decimal[] BuildVolumeSequence()
	{
		var tp = _takeProfitDistance;
		var rl = _recoveryDistance;
		var offset = ProfitOffsetPips * _pipSize;

		if (tp <= 0m || rl <= 0m)
		return new[] { VolumeMultiplier };

		var tpr1 = tp - rl;
		var tpr2 = tp + rl;

		var result = new decimal[Math.Min(Math.Max(MaxTrades, 1), 20)];

		var l1 = VolumeMultiplier;
		result[0] = l1;

		decimal SafeVolume(decimal value, decimal denominator)
		{
			if (denominator == 0m)
			return 0m;

			return Math.Abs(value / denominator);
		}

		if (result.Length > 1)
		{
			var l2 = SafeVolume(l1 * tp, tpr1 - offset);
			result[1] = l2;

			if (result.Length > 2)
			{
				var l3 = SafeVolume(l1 * tp - l2 * tpr2, tp - offset);
				result[2] = l3;

				if (result.Length > 3)
				{
					var l4 = SafeVolume(-l1 * tp - l2 * tpr1 - l3 * tp, tpr1 - offset);
					result[3] = l4;

					if (result.Length > 4)
					{
						var l5 = SafeVolume(l1 * tp - l2 * tpr2 + l3 * tp - l4 * tpr2, tp - offset);
						result[4] = l5;

						if (result.Length > 5)
						{
							var l6 = SafeVolume(-l1 * tp + l2 * tpr1 - l3 * tp + l4 * tpr1 - l5 * tp, tpr1 - offset);
							result[5] = l6;

							if (result.Length > 6)
							{
								var l7 = SafeVolume(l1 * tp - l2 * tpr2 + l3 * tp - l4 * tpr2 + l5 * tp - l6 * tpr2, tp - offset);
								result[6] = l7;

								if (result.Length > 7)
								{
									var l8 = SafeVolume(-l1 * tp + l2 * tpr1 - l3 * tp + l4 * tpr1 - l5 * tp + l6 * tpr1 - l7 * tp, tpr1 - offset);
									result[7] = l8;

									if (result.Length > 8)
									{
										var l9 = SafeVolume(l1 * tp - l2 * tpr2 + l3 * tp - l4 * tpr2 + l5 * tp - l6 * tpr2 + l7 * tp - l8 * tpr2, tp - offset);
										result[8] = l9;

										if (result.Length > 9)
										{
											var l10 = SafeVolume(-l1 * tp + l2 * tpr1 - l3 * tp + l4 * tpr1 - l5 * tp + l6 * tpr1 - l7 * tp + l8 * tpr1 - l9 * tp, tpr1 - offset);
											result[9] = l10;
										}
									}
								}
							}
						}
					}
				}
			}
		}

		return result;
	}

	private decimal GetVolumeForStep(int step)
	{
		if (step < _volumeSequence.Length)
		return _volumeSequence[step];

		return _volumeSequence.Length > 0 ? _volumeSequence[^1] : VolumeMultiplier;
	}
}
