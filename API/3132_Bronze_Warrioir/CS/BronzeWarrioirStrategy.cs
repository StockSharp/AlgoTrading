using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bronze Warrioir trading strategy converted from the MetaTrader 5 expert advisor.
/// Combines CCI, Williams %R and a double-smoothed DayImpuls oscillator to time entries.
/// </summary>
public class BronzeWarrioirStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _indicatorPeriod;
	private readonly StrategyParam<decimal> _cciLevel;
	private readonly StrategyParam<decimal> _williamsLevelUp;
	private readonly StrategyParam<decimal> _williamsLevelDown;
	private readonly StrategyParam<decimal> _dayImpulsLevel;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<decimal> _lossTarget;
	private readonly StrategyParam<decimal> _predTarget;
	private readonly StrategyParam<decimal> _lotCoefficient;
	private readonly StrategyParam<DataType> _candleType;

	private CommodityChannelIndex _cci;
	private WilliamsR _williams;
	private DayImpulsIndicator _dayImpuls;

	private decimal _longVolume;
	private decimal _shortVolume;
	private decimal _longAveragePrice;
	private decimal _shortAveragePrice;
	private decimal? _previousDayImpuls;
	private decimal _pipSize;

	/// <summary>
	/// Initializes strategy parameters with defaults from the original expert advisor.
	/// </summary>
	public BronzeWarrioirStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Trade Volume", "Base order volume used for every entry", "Trading")
			.SetGreaterThanZero();

		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetDisplay("Stop Loss (pips)", "Protective stop in pips converted to price distance", "Risk")
			.SetNotNegative();

		_takeProfitPips = Param(nameof(TakeProfitPips), 50)
			.SetDisplay("Take Profit (pips)", "Profit target in pips converted to price distance", "Risk")
			.SetNotNegative();

		_indicatorPeriod = Param(nameof(IndicatorPeriod), 14)
			.SetDisplay("Indicator Period", "Length applied to CCI, Williams %R and DayImpuls", "Indicators")
			.SetGreaterThanZero();

		_cciLevel = Param(nameof(CciLevel), 150m)
			.SetDisplay("CCI Level", "Absolute CCI threshold required for entries", "Indicators")
			.SetGreaterThanZero();

		_williamsLevelUp = Param(nameof(WilliamsLevelUp), -15m)
			.SetDisplay("Williams %R Upper", "Overbought level for short entries", "Indicators");

		_williamsLevelDown = Param(nameof(WilliamsLevelDown), -85m)
			.SetDisplay("Williams %R Lower", "Oversold level for long entries", "Indicators");

		_dayImpulsLevel = Param(nameof(DayImpulsLevel), 50m)
			.SetDisplay("DayImpuls Level", "Minimum oscillator value that confirms bearish pressure", "Indicators");

		_profitTarget = Param(nameof(ProfitTarget), 100m)
			.SetDisplay("Profit Target", "Floating profit at which all positions are closed", "Risk");

		_lossTarget = Param(nameof(LossTarget), -100m)
			.SetDisplay("Loss Target", "Floating loss that forces liquidation", "Risk");

		_predTarget = Param(nameof(PredTarget), 40m)
			.SetDisplay("Pred Target", "Profit band that triggers hedge style reversals", "Risk")
			.SetGreaterThanZero();

		_lotCoefficient = Param(nameof(LotCoefficient), 2m)
			.SetDisplay("Lot Coefficient", "Original EA parameter used to validate averaging entries", "Trading")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Time-frame used for indicator calculations", "Data");

		Volume = _tradeVolume.Value;
	}

	/// <summary>
	/// Trade volume for each new entry.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set
		{
			_tradeVolume.Value = value;
			Volume = value;
		}
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Shared period for the three oscillators.
	/// </summary>
	public int IndicatorPeriod
	{
		get => _indicatorPeriod.Value;
		set => _indicatorPeriod.Value = value;
	}

	/// <summary>
	/// Positive CCI level required for short entries.
	/// </summary>
	public decimal CciLevel
	{
		get => _cciLevel.Value;
		set => _cciLevel.Value = value;
	}

	/// <summary>
	/// Williams %R threshold that marks overbought conditions.
	/// </summary>
	public decimal WilliamsLevelUp
	{
		get => _williamsLevelUp.Value;
		set => _williamsLevelUp.Value = value;
	}

	/// <summary>
	/// Williams %R threshold that marks oversold conditions.
	/// </summary>
	public decimal WilliamsLevelDown
	{
		get => _williamsLevelDown.Value;
		set => _williamsLevelDown.Value = value;
	}

	/// <summary>
	/// DayImpuls level used to confirm bearish setups.
	/// </summary>
	public decimal DayImpulsLevel
	{
		get => _dayImpulsLevel.Value;
		set => _dayImpulsLevel.Value = value;
	}

	/// <summary>
	/// Profit objective on the aggregated floating PnL.
	/// </summary>
	public decimal ProfitTarget
	{
		get => _profitTarget.Value;
		set => _profitTarget.Value = value;
	}

	/// <summary>
	/// Loss limit on the aggregated floating PnL.
	/// </summary>
	public decimal LossTarget
	{
		get => _lossTarget.Value;
		set => _lossTarget.Value = value;
	}

	/// <summary>
	/// Profit band that enables the averaging style reversal.
	/// </summary>
	public decimal PredTarget
	{
		get => _predTarget.Value;
		set => _predTarget.Value = value;
	}

	/// <summary>
	/// Coefficient from the original EA used for validation only.
	/// </summary>
	public decimal LotCoefficient
	{
		get => _lotCoefficient.Value;
		set => _lotCoefficient.Value = value;
	}

	/// <summary>
	/// Candle type used to drive the calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_longVolume = 0m;
		_shortVolume = 0m;
		_longAveragePrice = 0m;
		_shortAveragePrice = 0m;
		_previousDayImpuls = null;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_cci = new CommodityChannelIndex { Length = IndicatorPeriod };
		_williams = new WilliamsR { Length = IndicatorPeriod };
		_dayImpuls = new DayImpulsIndicator
		{
			Length = IndicatorPeriod,
			PointValue = Security?.PriceStep ?? 0m
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_cci, _williams, _dayImpuls, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _cci);
			DrawIndicator(area, _williams);
			DrawIndicator(area, _dayImpuls);
			DrawOwnTrades(area);
		}

		_pipSize = CalculatePipSize();

		Unit? stopLossUnit = null;
		Unit? takeProfitUnit = null;

		if (_pipSize > 0m && StopLossPips > 0)
			stopLossUnit = new Unit(StopLossPips * _pipSize, UnitTypes.Price);

		if (_pipSize > 0m && TakeProfitPips > 0)
			takeProfitUnit = new Unit(TakeProfitPips * _pipSize, UnitTypes.Price);

		if (stopLossUnit.HasValue || takeProfitUnit.HasValue)
			StartProtection(stopLoss: stopLossUnit, takeProfit: takeProfitUnit);
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue, decimal wprValue, decimal dayImpulsValue)
	{
		if (candle.State != CandleStates.Finished)
		{
			_previousDayImpuls = dayImpulsValue;
			return;
		}

		var profit = GetOpenPnL(candle.ClosePrice);
		var previous = _previousDayImpuls;
		var handled = false;

		if (ProfitTarget > 0m && profit >= ProfitTarget)
		{
			CloseAllPositions();
			handled = true;
		}
		else if (LossTarget < 0m && profit <= LossTarget)
		{
			CloseAllPositions();
			handled = true;
		}
		else if (IsFormedAndOnlineAndAllowTrading() && previous.HasValue)
		{
			var hasLong = _longVolume > 0m;
			var hasShort = _shortVolume > 0m;

			var sellSignal = !hasShort &&
				dayImpulsValue > DayImpulsLevel &&
				previous.Value < dayImpulsValue &&
				wprValue > WilliamsLevelUp &&
				cciValue > CciLevel;

			if (sellSignal)
			{
				var volume = TradeVolume + (hasLong ? _longVolume : 0m);
				if (volume > 0m)
				{
					SellMarket(volume);
					handled = true;
				}
			}
			else
			{
				var buySignal = !hasLong &&
					dayImpulsValue < DayImpulsLevel &&
					previous.Value > dayImpulsValue &&
					wprValue < WilliamsLevelDown &&
					cciValue < -CciLevel;

				if (buySignal)
				{
					var volume = TradeVolume + (hasShort ? _shortVolume : 0m);
					if (volume > 0m)
					{
						BuyMarket(volume);
						handled = true;
					}
				}
			}

			if (!handled)
			{
				var reversalTrigger = profit <= -PredTarget / 2m || profit >= PredTarget;
				if (reversalTrigger && CanIncreaseVolume())
				{
					if (!hasLong && hasShort)
					{
						var volume = TradeVolume + _shortVolume;
						if (volume > 0m)
						{
							BuyMarket(volume);
							handled = true;
						}
					}
					else if (hasLong && !hasShort)
					{
						var volume = TradeVolume + _longVolume;
						if (volume > 0m)
						{
							SellMarket(volume);
							handled = true;
						}
					}
				}
			}
		}

		_previousDayImpuls = dayImpulsValue;
	}

	private void CloseAllPositions()
	{
		if (_longVolume > 0m)
			SellMarket(_longVolume);

		if (_shortVolume > 0m)
			BuyMarket(_shortVolume);
	}

	private decimal GetOpenPnL(decimal currentPrice)
	{
		var pnl = 0m;

		if (_longVolume > 0m)
			pnl += _longVolume * (currentPrice - _longAveragePrice);

		if (_shortVolume > 0m)
			pnl += _shortVolume * (_shortAveragePrice - currentPrice);

		return pnl;
	}

	private decimal CalculatePipSize()
	{
		if (Security == null)
			return 0m;

		var step = Security.PriceStep ?? 0m;
		if (step <= 0m)
			return 0m;

		var decimals = Security.Decimals;
		if (decimals == 3 || decimals == 5)
			return step * 10m;

		return step;
	}

	private bool CanIncreaseVolume()
	{
		var checkValue = LossTarget * LotCoefficient;
		return checkValue > 0m;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		if (trade.Order.Security != Security)
			return;

		var volume = trade.Trade.Volume;
		var price = trade.Trade.Price;

		if (trade.Order.Side == Sides.Buy)
		{
			if (_shortVolume > 0m)
			{
				var closingVolume = Math.Min(_shortVolume, volume);
				_shortVolume -= closingVolume;
				volume -= closingVolume;

				if (_shortVolume <= 0m)
				{
					_shortVolume = 0m;
					_shortAveragePrice = 0m;
				}
			}

			if (volume > 0m)
			{
				var newVolume = _longVolume + volume;
				_longAveragePrice = newVolume == 0m ? 0m : (_longAveragePrice * _longVolume + price * volume) / newVolume;
				_longVolume = newVolume;
			}
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			if (_longVolume > 0m)
			{
				var closingVolume = Math.Min(_longVolume, volume);
				_longVolume -= closingVolume;
				volume -= closingVolume;

				if (_longVolume <= 0m)
				{
					_longVolume = 0m;
					_longAveragePrice = 0m;
				}
			}

			if (volume > 0m)
			{
				var newVolume = _shortVolume + volume;
				_shortAveragePrice = newVolume == 0m ? 0m : (_shortAveragePrice * _shortVolume + price * volume) / newVolume;
				_shortVolume = newVolume;
			}
		}
	}

	private sealed class DayImpulsIndicator : Indicator<ICandleMessage>
	{
		private readonly ExponentialMovingAverage _first = new();
		private readonly ExponentialMovingAverage _second = new();
		private int _length = 14;

		public int Length
		{
			get => _length;
			set
			{
				if (value <= 0)
					throw new ArgumentOutOfRangeException(nameof(value));

				_length = value;
				_first.Length = value;
				_second.Length = value;
			}
		}

		public decimal PointValue { get; set; }

		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<ICandleMessage>();
			if (candle.State != CandleStates.Finished || PointValue <= 0m)
				return new DecimalIndicatorValue(this, 0m, input.Time);

			var raw = (candle.ClosePrice - candle.OpenPrice) / PointValue;
			var firstValue = _first.Process(raw, input.Time);
			if (!firstValue.IsFinal)
				return new DecimalIndicatorValue(this, 0m, input.Time);

			var secondValue = _second.Process(firstValue.ToDecimal(), input.Time);
			if (!secondValue.IsFinal)
				return new DecimalIndicatorValue(this, 0m, input.Time);

			IsFormed = true;
			return new DecimalIndicatorValue(this, secondValue.ToDecimal(), input.Time);
		}

		public override void Reset()
		{
			base.Reset();
			_first.Reset();
			_second.Reset();
		}
	}
}
