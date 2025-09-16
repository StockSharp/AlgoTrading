using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Liquidex strategy using moving average and Keltner Channel filter.
/// Trades are executed only during configured hours and can be confirmed by RSI direction.
/// </summary>
public class LiquidexKeltnerStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _rangeFilter;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<bool> _useKeltnerFilter;
	private readonly StrategyParam<int> _keltnerPeriod;
	private readonly StrategyParam<decimal> _keltnerMultiplier;
	private readonly StrategyParam<bool> _useRsiFilter;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _entryHourFrom;
	private readonly StrategyParam<int> _entryHourTo;
	private readonly StrategyParam<int> _fridayEndHour;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevPrice;

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }

	/// <summary>
	/// Minimum candle body size to trade.
	/// </summary>
	public decimal RangeFilter { get => _rangeFilter.Value; set => _rangeFilter.Value = value; }

	/// <summary>
	/// Stop-loss in percent.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Take-profit in percent.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Enable Keltner Channel filter.
	/// </summary>
	public bool UseKeltnerFilter { get => _useKeltnerFilter.Value; set => _useKeltnerFilter.Value = value; }

	/// <summary>
	/// Period for Keltner Channels.
	/// </summary>
	public int KeltnerPeriod { get => _keltnerPeriod.Value; set => _keltnerPeriod.Value = value; }

	/// <summary>
	/// Width multiplier for Keltner Channels.
	/// </summary>
	public decimal KeltnerMultiplier { get => _keltnerMultiplier.Value; set => _keltnerMultiplier.Value = value; }

	/// <summary>
	/// Enable RSI direction filter.
	/// </summary>
	public bool UseRsiFilter { get => _useRsiFilter.Value; set => _useRsiFilter.Value = value; }

	/// <summary>
	/// RSI indicator period.
	/// </summary>
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	/// <summary>
	/// Start trading hour.
	/// </summary>
	public int EntryHourFrom { get => _entryHourFrom.Value; set => _entryHourFrom.Value = value; }

	/// <summary>
	/// End trading hour.
	/// </summary>
	public int EntryHourTo { get => _entryHourTo.Value; set => _entryHourTo.Value = value; }

	/// <summary>
	/// Last trading hour on Friday.
	/// </summary>
	public int FridayEndHour { get => _fridayEndHour.Value; set => _fridayEndHour.Value = value; }

	/// <summary>
	/// Candle type for analysis.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="LiquidexKeltnerStrategy"/>.
	/// </summary>
	public LiquidexKeltnerStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 7)
			.SetRange(1, 100)
			.SetDisplay("MA Period", "Moving average period", "General");

		_rangeFilter = Param(nameof(RangeFilter), 10m)
			.SetRange(0m, 100m)
			.SetDisplay("Range Filter", "Minimum candle body", "General");

		_stopLoss = Param(nameof(StopLoss), 1m)
			.SetRange(0m, 10m)
			.SetDisplay("Stop Loss %", "Stop loss percent", "Risk Management");

		_takeProfit = Param(nameof(TakeProfit), 2m)
			.SetRange(0m, 20m)
			.SetDisplay("Take Profit %", "Take profit percent", "Risk Management");

		_useKeltnerFilter = Param(nameof(UseKeltnerFilter), true)
			.SetDisplay("Use Keltner", "Enable Keltner filter", "Filters");

		_keltnerPeriod = Param(nameof(KeltnerPeriod), 6)
			.SetRange(1, 100)
			.SetDisplay("Keltner Period", "Keltner period", "Filters");

		_keltnerMultiplier = Param(nameof(KeltnerMultiplier), 1m)
			.SetRange(0.5m, 5m)
			.SetDisplay("Keltner Multiplier", "Keltner width", "Filters");

		_useRsiFilter = Param(nameof(UseRsiFilter), false)
			.SetDisplay("Use RSI", "Enable RSI filter", "Filters");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetRange(1, 100)
			.SetDisplay("RSI Period", "RSI period", "Filters")
			.SetCanOptimize();

		_entryHourFrom = Param(nameof(EntryHourFrom), 2)
			.SetRange(0, 23)
			.SetDisplay("Entry From", "Start hour", "Time");

		_entryHourTo = Param(nameof(EntryHourTo), 24)
			.SetRange(0, 24)
			.SetDisplay("Entry To", "End hour", "Time");

		_fridayEndHour = Param(nameof(FridayEndHour), 22)
			.SetRange(0, 24)
			.SetDisplay("Friday End", "Friday closing hour", "Time");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ma = new MovingAverage
		{
			Length = MaPeriod,
		};

		var keltner = new KeltnerChannels
		{
			Length = KeltnerPeriod,
			Multiplier = KeltnerMultiplier,
		};

		var rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ma, keltner, rsi, ProcessCandle)
			.Start();

		StartProtection(
			stopLoss: new Unit(StopLoss, UnitTypes.Percent),
			takeProfit: new Unit(TakeProfit, UnitTypes.Percent)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			if (UseKeltnerFilter)
				DrawIndicator(area, keltner);
			if (UseRsiFilter)
				DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue maValue, IIndicatorValue keltnerValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.CloseTime.LocalDateTime;

		if (!IsTradingTime(time))
		{
			_prevPrice = candle.ClosePrice;
			return;
		}

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		if (body < RangeFilter)
		{
			_prevPrice = candle.ClosePrice;
			return;
		}

		var ma = maValue.GetValue<decimal>();
		var price = candle.ClosePrice;

		if (UseKeltnerFilter)
		{
			var kc = (KeltnerChannelsValue)keltnerValue;

			if (kc.UpBand is not decimal upper || kc.LowBand is not decimal lower)
			{
				_prevPrice = price;
				return;
			}

			var crossAbove = _prevPrice <= upper && price > upper;
			var crossBelow = _prevPrice >= lower && price < lower;

			if (crossAbove && price > ma && (!UseRsiFilter || rsiValue.GetValue<decimal>() > 50m) && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}
			else if (crossBelow && price < ma && (!UseRsiFilter || rsiValue.GetValue<decimal>() < 50m) && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
			}
		}
		else
		{
			if (price > ma && (!UseRsiFilter || rsiValue.GetValue<decimal>() > 50m) && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}
			else if (price < ma && (!UseRsiFilter || rsiValue.GetValue<decimal>() < 50m) && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
			}
		}

		_prevPrice = price;
	}

	private bool IsTradingTime(DateTime time)
	{
		var hour = time.Hour;

		if (time.DayOfWeek == DayOfWeek.Friday && hour >= FridayEndHour)
			return false;

		if (EntryHourFrom <= EntryHourTo)
			return hour >= EntryHourFrom && hour <= EntryHourTo;

		return hour >= EntryHourFrom || hour <= EntryHourTo;
	}
}
