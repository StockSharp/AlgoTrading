using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy Stats [presentTrading] strategy.
/// Uses two Vegas adjusted SuperTrends and multi-step take profits.
/// </summary>
public class StrategyStatsPresentTradingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod1;
	private readonly StrategyParam<int> _vegasWindow1;
	private readonly StrategyParam<decimal> _multiplier1;
	private readonly StrategyParam<decimal> _volatilityAdjustment1;
	private readonly StrategyParam<int> _atrPeriod2;
	private readonly StrategyParam<int> _vegasWindow2;
	private readonly StrategyParam<decimal> _multiplier2;
	private readonly StrategyParam<decimal> _volatilityAdjustment2;
private readonly StrategyParam<Sides?> _direction;
	private readonly StrategyParam<bool> _useHoldDays;
	private readonly StrategyParam<int> _holdDays;
	private readonly StrategyParam<string> _tpslCondition;
	private readonly StrategyParam<decimal> _stopLossPerc;

	private readonly StrategyParam<int> _atrLengthTp;
	private readonly StrategyParam<decimal> _atrMultiplierTp1;
	private readonly StrategyParam<decimal> _atrMultiplierTp2;
	private readonly StrategyParam<decimal> _atrMultiplierTp3;
	private readonly StrategyParam<decimal> _tpLevelPercent1;
	private readonly StrategyParam<decimal> _tpLevelPercent2;
	private readonly StrategyParam<decimal> _tpLevelPercent3;
	private readonly StrategyParam<decimal> _tpPercent1;
	private readonly StrategyParam<decimal> _tpPercent2;
	private readonly StrategyParam<decimal> _tpPercent3;
	private readonly StrategyParam<decimal> _tpPercentAtr1;
	private readonly StrategyParam<decimal> _tpPercentAtr2;
	private readonly StrategyParam<decimal> _tpPercentAtr3;
	private readonly StrategyParam<decimal> _shortTpMultiplier;

	private decimal _prevUpper1;
	private decimal _prevLower1;
	private int _trend1 = 1;
	private bool _hasTrend1;

	private decimal _prevUpper2;
	private decimal _prevLower2;
	private int _trend2 = 1;
	private bool _hasTrend2;

	private DateTimeOffset? _longEntryTime;
	private DateTimeOffset? _shortEntryTime;

	public StrategyStatsPresentTradingStrategy()
		{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
						  .SetDisplay("Candle Type", "Type of candles", "General");

		_atrPeriod1 =
			Param(nameof(AtrPeriod1), 10).SetDisplay("ATR Period 1", "ATR length for first SuperTrend", "SuperTrend 1");

		_vegasWindow1 =
			Param(nameof(VegasWindow1), 100).SetDisplay("Vegas Window 1", "Vegas channel SMA period", "SuperTrend 1");

		_multiplier1 = Param(nameof(Multiplier1), 5m)
						   .SetDisplay("Multiplier 1", "Base multiplier for first SuperTrend", "SuperTrend 1");

		_volatilityAdjustment1 =
			Param(nameof(VolatilityAdjustment1), 5m)
				.SetDisplay("Volatility Adjustment 1", "Volatility factor for first SuperTrend", "SuperTrend 1");

		_atrPeriod2 =
			Param(nameof(AtrPeriod2), 5).SetDisplay("ATR Period 2", "ATR length for second SuperTrend", "SuperTrend 2");

		_vegasWindow2 =
			Param(nameof(VegasWindow2), 200).SetDisplay("Vegas Window 2", "Vegas channel SMA period", "SuperTrend 2");

		_multiplier2 = Param(nameof(Multiplier2), 7m)
						   .SetDisplay("Multiplier 2", "Base multiplier for second SuperTrend", "SuperTrend 2");

		_volatilityAdjustment2 =
			Param(nameof(VolatilityAdjustment2), 7m)
				.SetDisplay("Volatility Adjustment 2", "Volatility factor for second SuperTrend", "SuperTrend 2");

_direction =
Param(nameof(Direction), (Sides?)null).SetDisplay("Trade Direction", "Allowed trading direction", "General");

		_useHoldDays = Param(nameof(UseHoldDays), true).SetDisplay("Use Hold Days", "Enable holding period", "Exits");

		_holdDays = Param(nameof(HoldDays), 5).SetDisplay("Hold Days", "Number of days to hold position", "Exits");

				_tpslCondition = Param(nameof(TpslCondition), "Both")
														 .SetDisplay("TPSL Condition", "Enable take profit and/or stop loss", "Protection");

				_stopLossPerc =
						Param(nameof(StopLossPerc), 20m).SetDisplay("Stop Loss (%)", "Stop loss percentage", "Protection");

				_atrLengthTp = Param(nameof(AtrLengthTp), 14).SetDisplay("ATR Length TP", "ATR length for take profit", "Take Profit");
				_atrMultiplierTp1 = Param(nameof(AtrMultiplierTp1), 2.618m).SetDisplay("ATR Multiplier TP1", "ATR multiplier for TP1", "Take Profit");
				_atrMultiplierTp2 = Param(nameof(AtrMultiplierTp2), 5m).SetDisplay("ATR Multiplier TP2", "ATR multiplier for TP2", "Take Profit");
				_atrMultiplierTp3 = Param(nameof(AtrMultiplierTp3), 10m).SetDisplay("ATR Multiplier TP3", "ATR multiplier for TP3", "Take Profit");
				_tpLevelPercent1 = Param(nameof(TpLevelPercent1), 3m).SetDisplay("TP Level %1", "Percent level TP1", "Take Profit");
				_tpLevelPercent2 = Param(nameof(TpLevelPercent2), 8m).SetDisplay("TP Level %2", "Percent level TP2", "Take Profit");
				_tpLevelPercent3 = Param(nameof(TpLevelPercent3), 17m).SetDisplay("TP Level %3", "Percent level TP3", "Take Profit");
				_tpPercent1 = Param(nameof(TpPercent1), 12m).SetDisplay("TP Percent1", "Volume percent TP1", "Take Profit");
				_tpPercent2 = Param(nameof(TpPercent2), 8m).SetDisplay("TP Percent2", "Volume percent TP2", "Take Profit");
				_tpPercent3 = Param(nameof(TpPercent3), 10m).SetDisplay("TP Percent3", "Volume percent TP3", "Take Profit");
				_tpPercentAtr1 = Param(nameof(TpPercentAtr1), 10m).SetDisplay("ATR TP Percent1", "Volume percent ATR TP1", "Take Profit");
				_tpPercentAtr2 = Param(nameof(TpPercentAtr2), 10m).SetDisplay("ATR TP Percent2", "Volume percent ATR TP2", "Take Profit");
				_tpPercentAtr3 = Param(nameof(TpPercentAtr3), 10m).SetDisplay("ATR TP Percent3", "Volume percent ATR TP3", "Take Profit");
				_shortTpMultiplier = Param(nameof(ShortTpMultiplier), 1.5m).SetDisplay("Short TP Multiplier", "Multiplier for short TP volume", "Take Profit");
		}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AtrPeriod1
	{
		get => _atrPeriod1.Value;
		set => _atrPeriod1.Value = value;
	}

	public int VegasWindow1
	{
		get => _vegasWindow1.Value;
		set => _vegasWindow1.Value = value;
	}

	public decimal Multiplier1
	{
		get => _multiplier1.Value;
		set => _multiplier1.Value = value;
	}

	public decimal VolatilityAdjustment1
	{
		get => _volatilityAdjustment1.Value;
		set => _volatilityAdjustment1.Value = value;
	}

	public int AtrPeriod2
	{
		get => _atrPeriod2.Value;
		set => _atrPeriod2.Value = value;
	}

	public int VegasWindow2
	{
		get => _vegasWindow2.Value;
		set => _vegasWindow2.Value = value;
	}

	public decimal Multiplier2
	{
		get => _multiplier2.Value;
		set => _multiplier2.Value = value;
	}

	public decimal VolatilityAdjustment2
	{
		get => _volatilityAdjustment2.Value;
		set => _volatilityAdjustment2.Value = value;
	}

public Sides? Direction
{
get => _direction.Value;
set => _direction.Value = value;
}

	public bool UseHoldDays
	{
		get => _useHoldDays.Value;
		set => _useHoldDays.Value = value;
	}

	public int HoldDays
	{
		get => _holdDays.Value;
		set => _holdDays.Value = value;
	}

	public string TpslCondition
		{
				get => _tpslCondition.Value;
				set => _tpslCondition.Value = value;
		}

	public decimal StopLossPerc
		{
				get => _stopLossPerc.Value;
				set => _stopLossPerc.Value = value;
		}

	public int AtrLengthTp { get => _atrLengthTp.Value; set => _atrLengthTp.Value = value; }
	public decimal AtrMultiplierTp1 { get => _atrMultiplierTp1.Value; set => _atrMultiplierTp1.Value = value; }
	public decimal AtrMultiplierTp2 { get => _atrMultiplierTp2.Value; set => _atrMultiplierTp2.Value = value; }
	public decimal AtrMultiplierTp3 { get => _atrMultiplierTp3.Value; set => _atrMultiplierTp3.Value = value; }
	public decimal TpLevelPercent1 { get => _tpLevelPercent1.Value; set => _tpLevelPercent1.Value = value; }
	public decimal TpLevelPercent2 { get => _tpLevelPercent2.Value; set => _tpLevelPercent2.Value = value; }
	public decimal TpLevelPercent3 { get => _tpLevelPercent3.Value; set => _tpLevelPercent3.Value = value; }
	public decimal TpPercent1 { get => _tpPercent1.Value; set => _tpPercent1.Value = value; }
	public decimal TpPercent2 { get => _tpPercent2.Value; set => _tpPercent2.Value = value; }
	public decimal TpPercent3 { get => _tpPercent3.Value; set => _tpPercent3.Value = value; }
	public decimal TpPercentAtr1 { get => _tpPercentAtr1.Value; set => _tpPercentAtr1.Value = value; }
	public decimal TpPercentAtr2 { get => _tpPercentAtr2.Value; set => _tpPercentAtr2.Value = value; }
	public decimal TpPercentAtr3 { get => _tpPercentAtr3.Value; set => _tpPercentAtr3.Value = value; }
	public decimal ShortTpMultiplier { get => _shortTpMultiplier.Value; set => _shortTpMultiplier.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevUpper1 = _prevLower1 = 0m;
		_prevUpper2 = _prevLower2 = 0m;
		_trend1 = _trend2 = 1;
		_hasTrend1 = _hasTrend2 = false;
		_longEntryTime = _shortEntryTime = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var vegasSma1 = new SimpleMovingAverage { Length = VegasWindow1 };
		var vegasStd1 = new StandardDeviation { Length = VegasWindow1 };
		var atr1 = new AverageTrueRange { Length = AtrPeriod1 };

				var vegasSma2 = new SimpleMovingAverage { Length = VegasWindow2 };
				var vegasStd2 = new StandardDeviation { Length = VegasWindow2 };
				var atr2 = new AverageTrueRange { Length = AtrPeriod2 };
				var atrTp = new AverageTrueRange { Length = AtrLengthTp };

				var subscription = SubscribeCandles(CandleType);
				subscription.Bind(vegasSma1, vegasStd1, atr1, vegasSma2, vegasStd2, atr2, atrTp, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

				if (TpslCondition == "SL" || TpslCondition == "Both")
						StartProtection(null, StopLossPerc.Percents());
		}

	private void ProcessCandle(ICandleMessage candle, decimal vegasMa1, decimal vegasStd1, decimal atr1,
														   decimal vegasMa2, decimal vegasStd2, decimal atr2, decimal atrTp)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (vegasMa1 == 0m || vegasMa2 == 0m)
			return;

		var hlc3 = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;

		var channelWidth1 = vegasStd1 * 2m;
		var adjusted1 = Multiplier1 + VolatilityAdjustment1 * (channelWidth1 / vegasMa1);
		var upper1 = hlc3 - adjusted1 * atr1;
		var lower1 = hlc3 + adjusted1 * atr1;

		if (!_hasTrend1)
		{
			_prevUpper1 = upper1;
			_prevLower1 = lower1;
			_hasTrend1 = true;
		}

		_trend1 = candle.ClosePrice > _prevLower1 ? 1 : candle.ClosePrice < _prevUpper1 ? -1 : _trend1;
		upper1 = _trend1 == 1 ? Math.Max(upper1, _prevUpper1) : upper1;
		lower1 = _trend1 == -1 ? Math.Min(lower1, _prevLower1) : lower1;
		_prevUpper1 = upper1;
		_prevLower1 = lower1;

		var channelWidth2 = vegasStd2 * 2m;
		var adjusted2 = Multiplier2 + VolatilityAdjustment2 * (channelWidth2 / vegasMa2);
		var upper2 = hlc3 - adjusted2 * atr2;
		var lower2 = hlc3 + adjusted2 * atr2;

		if (!_hasTrend2)
		{
			_prevUpper2 = upper2;
			_prevLower2 = lower2;
			_hasTrend2 = true;
		}

		_trend2 = candle.ClosePrice > _prevLower2 ? 1 : candle.ClosePrice < _prevUpper2 ? -1 : _trend2;
		upper2 = _trend2 == 1 ? Math.Max(upper2, _prevUpper2) : upper2;
		lower2 = _trend2 == -1 ? Math.Min(lower2, _prevLower2) : lower2;
		_prevUpper2 = upper2;
		_prevLower2 = lower2;

		var enterLong = _trend1 == 1 && _trend2 == 1;
		var enterShort = _trend1 == -1 && _trend2 == -1;
		var exitLong = _trend1 == -1 || _trend2 == -1;
		var exitShort = _trend1 == 1 || _trend2 == 1;

var allowLong = Direction is null or Sides.Buy;
var allowShort = Direction is null or Sides.Sell;
		var now = candle.OpenTime;

				if (UseHoldDays)
				{
						if (_longEntryTime != null && now >= _longEntryTime + TimeSpan.FromDays(HoldDays) && Position > 0)
						{
								SellMarket(Position);
								CancelActiveOrders();
								_longEntryTime = null;
						}

						if (_shortEntryTime != null && now >= _shortEntryTime + TimeSpan.FromDays(HoldDays) && Position < 0)
						{
								BuyMarket(Position.Abs());
								CancelActiveOrders();
								_shortEntryTime = null;
						}
		}
		else
		{
						if (exitLong && Position > 0)
						{
								SellMarket(Position);
								CancelActiveOrders();
								_longEntryTime = null;
						}

						if (exitShort && Position < 0)
						{
								BuyMarket(Position.Abs());
								CancelActiveOrders();
								_shortEntryTime = null;
						}
				}

				if (enterLong && allowLong && Position <= 0)
				{
						var volume = Volume + Position.Abs();
						CancelActiveOrders();
						BuyMarket(volume);
						PlaceTakeProfits(true, candle.ClosePrice, volume, atrTp);
						_longEntryTime = now;
				}
				else if (enterShort && allowShort && Position >= 0)
				{
						var volume = Volume + Position.Abs();
						CancelActiveOrders();
						SellMarket(volume);
						PlaceTakeProfits(false, candle.ClosePrice, volume, atrTp);
						_shortEntryTime = now;
				}
		}

	private void PlaceTakeProfits(bool isLong, decimal entryPrice, decimal volume, decimal atr)
		{
				if (TpslCondition == "SL")
						return;

				var factor = isLong ? 1m : ShortTpMultiplier;

				void PlacePercent(decimal level, decimal percent)
				{
						if (percent <= 0m)
								return;

						var qty = volume * percent / 100m;
						var price = isLong ? entryPrice * (1m + level / 100m) : entryPrice * (1m - level / 100m);
						if (isLong)
								SellLimit(qty, price);
						else
								BuyLimit(qty, price);
				}

				void PlaceAtr(decimal mult, decimal percent)
				{
						if (percent <= 0m)
								return;

						var qty = volume * percent / 100m;
						var price = isLong ? entryPrice + atr * mult : entryPrice - atr * mult;
						if (isLong)
								SellLimit(qty, price);
						else
								BuyLimit(qty, price);
				}

				PlacePercent(TpLevelPercent1, TpPercent1 * factor);
				PlacePercent(TpLevelPercent2, TpPercent2 * factor);
				PlacePercent(TpLevelPercent3, TpPercent3 * factor);

				PlaceAtr(AtrMultiplierTp1, TpPercentAtr1 * factor);
				PlaceAtr(AtrMultiplierTp2, TpPercentAtr2 * factor);
				PlaceAtr(AtrMultiplierTp3, TpPercentAtr3 * factor);
		}
}
