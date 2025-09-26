using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bronze Pan trading strategy converted from the MetaTrader 4 expert advisor.
/// Replicates the DayImpuls momentum filter combined with Williams %R and CCI thresholds.
/// </summary>
public class BronzePanStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _longStopLossPips;
	private readonly StrategyParam<int> _shortStopLossPips;
	private readonly StrategyParam<int> _longTakeProfitPips;
	private readonly StrategyParam<int> _shortTakeProfitPips;
	private readonly StrategyParam<int> _indicatorPeriod;
	private readonly StrategyParam<decimal> _cciLevel;
	private readonly StrategyParam<decimal> _williamsLevelUp;
	private readonly StrategyParam<decimal> _williamsLevelDown;
	private readonly StrategyParam<decimal> _dayImpulsShortLevel;
	private readonly StrategyParam<decimal> _dayImpulsLongLevel;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<decimal> _lossTarget;
	private readonly StrategyParam<decimal> _predBand;
	private readonly StrategyParam<decimal> _lotMultiplier;
	private readonly StrategyParam<decimal> _minimumBalance;
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
	/// Initializes strategy parameters using defaults from the original expert advisor.
	/// </summary>
	public BronzePanStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetDisplay("Trade Volume", "Base order size used for entries", "Trading")
			.SetGreaterThanZero();

		_longStopLossPips = Param(nameof(LongStopLossPips), 0)
			.SetDisplay("Long Stop Loss", "Protective stop for long positions expressed in pips", "Risk")
			.SetNotNegative();

		_shortStopLossPips = Param(nameof(ShortStopLossPips), 0)
			.SetDisplay("Short Stop Loss", "Protective stop for short positions expressed in pips", "Risk")
			.SetNotNegative();

		_longTakeProfitPips = Param(nameof(LongTakeProfitPips), 0)
			.SetDisplay("Long Take Profit", "Profit target for long positions expressed in pips", "Risk")
			.SetNotNegative();

		_shortTakeProfitPips = Param(nameof(ShortTakeProfitPips), 0)
			.SetDisplay("Short Take Profit", "Profit target for short positions expressed in pips", "Risk")
			.SetNotNegative();

		_indicatorPeriod = Param(nameof(IndicatorPeriod), 14)
			.SetDisplay("Indicator Period", "Length applied to DayImpuls, Williams %R and CCI", "Indicators")
			.SetGreaterThanZero();

		_cciLevel = Param(nameof(CciLevel), 150m)
			.SetDisplay("CCI Level", "Absolute CCI threshold confirming overbought/oversold conditions", "Indicators")
			.SetGreaterThanZero();

		_williamsLevelUp = Param(nameof(WilliamsLevelUp), -15m)
			.SetDisplay("Williams %R Upper", "Williams %R level used for short signals", "Indicators");

		_williamsLevelDown = Param(nameof(WilliamsLevelDown), -85m)
			.SetDisplay("Williams %R Lower", "Williams %R level used for long signals", "Indicators");

		_dayImpulsShortLevel = Param(nameof(DayImpulsShortLevel), 50m)
			.SetDisplay("DayImpuls Short Level", "Minimum DayImpuls value that validates bearish entries", "Indicators");

		_dayImpulsLongLevel = Param(nameof(DayImpulsLongLevel), -50m)
			.SetDisplay("DayImpuls Long Level", "Maximum DayImpuls value that validates bullish entries", "Indicators");

		_profitTarget = Param(nameof(ProfitTarget), 500m)
			.SetDisplay("Profit Target", "Floating profit that forces liquidation", "Risk");

		_lossTarget = Param(nameof(LossTarget), -2000m)
			.SetDisplay("Loss Target", "Floating loss that forces liquidation", "Risk");

		_predBand = Param(nameof(PredBand), 100m)
			.SetDisplay("Reversal Band", "Profit band enabling averaging reversals", "Risk")
			.SetGreaterThanZero();

		_lotMultiplier = Param(nameof(LotMultiplier), 30m)
			.SetDisplay("Lot Multiplier", "Volume multiplier for averaging entries", "Trading")
			.SetGreaterThanZero();

		_minimumBalance = Param(nameof(MinimumBalance), 3000m)
			.SetDisplay("Minimum Balance", "Stop opening trades below this account balance", "Risk")
			.SetGreaterThanOrEqualZero();

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
	/// Stop-loss in pips applied to long positions.
	/// </summary>
	public int LongStopLossPips
	{
		get => _longStopLossPips.Value;
		set => _longStopLossPips.Value = value;
	}

	/// <summary>
	/// Stop-loss in pips applied to short positions.
	/// </summary>
	public int ShortStopLossPips
	{
		get => _shortStopLossPips.Value;
		set => _shortStopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit in pips applied to long positions.
	/// </summary>
	public int LongTakeProfitPips
	{
		get => _longTakeProfitPips.Value;
		set => _longTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Take-profit in pips applied to short positions.
	/// </summary>
	public int ShortTakeProfitPips
	{
		get => _shortTakeProfitPips.Value;
		set => _shortTakeProfitPips.Value = value;
	}

	/// <summary>
	/// Period used by DayImpuls, Williams %R and CCI.
	/// </summary>
	public int IndicatorPeriod
	{
		get => _indicatorPeriod.Value;
		set => _indicatorPeriod.Value = value;
	}

	/// <summary>
	/// Absolute CCI threshold from the original expert advisor.
	/// </summary>
	public decimal CciLevel
	{
		get => _cciLevel.Value;
		set => _cciLevel.Value = value;
	}

	/// <summary>
	/// Upper Williams %R threshold confirming overbought conditions.
	/// </summary>
	public decimal WilliamsLevelUp
	{
		get => _williamsLevelUp.Value;
		set => _williamsLevelUp.Value = value;
	}

	/// <summary>
	/// Lower Williams %R threshold confirming oversold conditions.
	/// </summary>
	public decimal WilliamsLevelDown
	{
		get => _williamsLevelDown.Value;
		set => _williamsLevelDown.Value = value;
	}

	/// <summary>
	/// DayImpuls level that confirms bearish momentum.
	/// </summary>
	public decimal DayImpulsShortLevel
	{
		get => _dayImpulsShortLevel.Value;
		set => _dayImpulsShortLevel.Value = value;
	}

	/// <summary>
	/// DayImpuls level that confirms bullish momentum.
	/// </summary>
	public decimal DayImpulsLongLevel
	{
		get => _dayImpulsLongLevel.Value;
		set => _dayImpulsLongLevel.Value = value;
	}

	/// <summary>
	/// Floating profit that triggers full liquidation.
	/// </summary>
	public decimal ProfitTarget
	{
		get => _profitTarget.Value;
		set => _profitTarget.Value = value;
	}

	/// <summary>
	/// Floating loss that triggers full liquidation.
	/// </summary>
	public decimal LossTarget
	{
		get => _lossTarget.Value;
		set => _lossTarget.Value = value;
	}

	/// <summary>
	/// Profit band enabling the aggressive averaging trade.
	/// </summary>
	public decimal PredBand
	{
		get => _predBand.Value;
		set => _predBand.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the base trade volume during reversals.
	/// </summary>
	public decimal LotMultiplier
	{
		get => _lotMultiplier.Value;
		set => _lotMultiplier.Value = value;
	}

	/// <summary>
	/// Minimum account balance required to open new positions.
	/// </summary>
	public decimal MinimumBalance
	{
		get => _minimumBalance.Value;
		set => _minimumBalance.Value = value;
	}

	/// <summary>
	/// Candle type used to drive calculations.
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
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue, decimal wprValue, decimal dayImpulsValue)
	{
		if (candle.State != CandleStates.Finished)
		{
			_previousDayImpuls = dayImpulsValue;
			return;
		}

		ApplyDirectionalStops(candle);

		var profit = GetOpenPnL(candle.ClosePrice);
		var previous = _previousDayImpuls;

		if (ProfitTarget > 0m && profit >= ProfitTarget)
		{
			CloseAllPositions();
			_previousDayImpuls = dayImpulsValue;
			return;
		}

		if (LossTarget < 0m && profit <= LossTarget)
		{
			CloseAllPositions();
			_previousDayImpuls = dayImpulsValue;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading() || !HasSufficientBalance())
		{
			_previousDayImpuls = dayImpulsValue;
			return;
		}

		var openTrades = (_longVolume > 0m ? 1 : 0) + (_shortVolume > 0m ? 1 : 0);
		var hasLong = _longVolume > 0m;
		var hasShort = _shortVolume > 0m;

		var handled = false;

		if (previous.HasValue && openTrades < 2)
		{
			var sellSignal = !hasShort &&
				dayImpulsValue > DayImpulsShortLevel &&
				previous.Value > dayImpulsValue &&
				wprValue > WilliamsLevelUp &&
				cciValue > CciLevel;

			if (sellSignal)
			{
				var volume = TradeVolume;
				if (hasLong)
					volume += _longVolume;

				if (volume > 0m)
				{
					SellMarket(volume);
					handled = true;
				}
			}
			else
			{
				var buySignal = !hasLong &&
					dayImpulsValue < DayImpulsLongLevel &&
					previous.Value < dayImpulsValue &&
					wprValue < WilliamsLevelDown &&
					cciValue < -CciLevel;

				if (buySignal)
				{
					var volume = TradeVolume;
					if (hasShort)
						volume += _shortVolume;

					if (volume > 0m)
					{
						BuyMarket(volume);
						handled = true;
					}
				}
			}
		}

		if (!handled && previous.HasValue)
		{
			var trigger = profit <= -PredBand / 2m || profit >= PredBand;
			if (trigger)
			{
				if (!hasLong && hasShort)
				{
					var volume = TradeVolume * LotMultiplier;
					if (volume > 0m)
						BuyMarket(volume);
				}
				else if (hasLong && !hasShort)
				{
					var volume = TradeVolume * LotMultiplier;
					if (volume > 0m)
						SellMarket(volume);
				}
			}
		}

		_previousDayImpuls = dayImpulsValue;
	}

	private void ApplyDirectionalStops(ICandleMessage candle)
	{
		if (_pipSize <= 0m)
			return;

		var close = candle.ClosePrice;

		if (_longVolume > 0m)
		{
			var stopDistance = LongStopLossPips > 0 ? LongStopLossPips * _pipSize : (decimal?)null;
			var takeDistance = LongTakeProfitPips > 0 ? LongTakeProfitPips * _pipSize : (decimal?)null;

			if (stopDistance.HasValue && _longAveragePrice - close >= stopDistance.Value)
			{
				SellMarket(_longVolume);
			}
			else if (takeDistance.HasValue && close - _longAveragePrice >= takeDistance.Value)
			{
				SellMarket(_longVolume);
			}
		}

		if (_shortVolume > 0m)
		{
			var stopDistance = ShortStopLossPips > 0 ? ShortStopLossPips * _pipSize : (decimal?)null;
			var takeDistance = ShortTakeProfitPips > 0 ? ShortTakeProfitPips * _pipSize : (decimal?)null;

			if (stopDistance.HasValue && close - _shortAveragePrice >= stopDistance.Value)
			{
				BuyMarket(_shortVolume);
			}
			else if (takeDistance.HasValue && _shortAveragePrice - close >= takeDistance.Value)
			{
				BuyMarket(_shortVolume);
			}
		}
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

	private bool HasSufficientBalance()
	{
		if (MinimumBalance <= 0m)
			return true;

		var balance = Portfolio?.CurrentValue;
		return !balance.HasValue || balance.Value >= MinimumBalance;
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
