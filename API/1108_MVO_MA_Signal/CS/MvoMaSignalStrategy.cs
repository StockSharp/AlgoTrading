using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Heikin Ashi based strategy that trades on moving average crossover or extreme MFI values.
/// Implements ATR based stop, take profit, optional breakeven and trailing stop.
/// </summary>
public class MvoMaSignalStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _mfiPeriod;
	private readonly StrategyParam<decimal> _riskMult;
	private readonly StrategyParam<decimal> _rewardMult;
	private readonly StrategyParam<decimal> _breakevenTicks;
	private readonly StrategyParam<decimal> _trailAtrMult;
	private readonly StrategyParam<bool> _enableTrailing;
	private readonly StrategyParam<bool> _enableBreakeven;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal _breakevenLevel;
	private bool _movedToBreakeven;

	private decimal? _haOpen;
	private decimal? _haClose;
	private decimal _prevHaClose;
	private decimal _prevMa;
	private bool _initialized;

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// MFI period.
	/// </summary>
	public int MfiPeriod
	{
		get => _mfiPeriod.Value;
		set => _mfiPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss multiplier in ATR units.
	/// </summary>
	public decimal RiskMult
	{
		get => _riskMult.Value;
		set => _riskMult.Value = value;
	}

	/// <summary>
	/// Take profit multiplier in ATR units.
	/// </summary>
	public decimal RewardMult
	{
		get => _rewardMult.Value;
		set => _rewardMult.Value = value;
	}

	/// <summary>
	/// Breakeven trigger in ATR units.
	/// </summary>
	public decimal BreakevenTicks
	{
		get => _breakevenTicks.Value;
		set => _breakevenTicks.Value = value;
	}

	/// <summary>
	/// Trailing stop multiplier in ATR units.
	/// </summary>
	public decimal TrailAtrMult
	{
		get => _trailAtrMult.Value;
		set => _trailAtrMult.Value = value;
	}

	/// <summary>
	/// Enable trailing stop.
	/// </summary>
	public bool EnableTrailing
	{
		get => _enableTrailing.Value;
		set => _enableTrailing.Value = value;
	}

	/// <summary>
	/// Enable breakeven.
	/// </summary>
	public bool EnableBreakeven
	{
		get => _enableBreakeven.Value;
		set => _enableBreakeven.Value = value;
	}

	/// <summary>
	/// Allow long trades.
	/// </summary>
	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	/// <summary>
	/// Allow short trades.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public MvoMaSignalStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 55)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period for moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 5);

		_atrPeriod = Param(nameof(AtrPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR calculation period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_mfiPeriod = Param(nameof(MfiPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("MFI Period", "Period for MFI", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_riskMult = Param(nameof(RiskMult), 1m)
			.SetGreaterThanZero()
			.SetDisplay("SL Multiplier", "Stop loss in ATR", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3m, 0.5m);

		_rewardMult = Param(nameof(RewardMult), 5m)
			.SetGreaterThanZero()
			.SetDisplay("TP Multiplier", "Take profit in ATR", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 10m, 1m);

		_breakevenTicks = Param(nameof(BreakevenTicks), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Breakeven ATR", "Move to breakeven after", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_trailAtrMult = Param(nameof(TrailAtrMult), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Trail ATR", "Trailing stop after breakeven", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_enableTrailing = Param(nameof(EnableTrailing), true)
			.SetDisplay("Enable Trailing", "Use trailing stop", "Risk");

		_enableBreakeven = Param(nameof(EnableBreakeven), true)
			.SetDisplay("Enable Breakeven", "Move stop to breakeven", "Risk");

		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Allow Long", "Allow long trades", "General");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Allow Short", "Allow short trades", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_breakevenLevel = 0m;
		_movedToBreakeven = false;
		_haOpen = null;
		_haClose = null;
		_prevHaClose = 0m;
		_prevMa = 0m;
		_initialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ma = new SMA { Length = MaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		var mfi = new MoneyFlowIndex { Length = MfiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ma, atr, mfi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal atrValue, decimal mfiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		var haOpen = _haOpen.HasValue && _haClose.HasValue ? (_haOpen.Value + _haClose.Value) / 2m : (candle.OpenPrice + candle.ClosePrice) / 2m;
		var haHigh = Math.Max(candle.HighPrice, Math.Max(haOpen, haClose));
		var haLow = Math.Min(candle.LowPrice, Math.Min(haOpen, haClose));

		if (!_initialized)
		{
			_prevHaClose = haClose;
			_prevMa = maValue;
			_haOpen = haOpen;
			_haClose = haClose;
			_initialized = true;
			return;
		}

		var crossUp = _prevHaClose <= _prevMa && haClose > maValue;
		var crossDown = _prevHaClose >= _prevMa && haClose < maValue;

		var longSignal = EnableLong && (crossUp || (mfiValue < 20m && haClose > maValue));
		var shortSignal = EnableShort && (crossDown || (mfiValue > 90m && haClose < maValue));

		if (Position == 0)
		{
			if (longSignal)
			{
				_entryPrice = candle.ClosePrice;
				_stopPrice = candle.ClosePrice - RiskMult * atrValue;
				_takePrice = candle.ClosePrice + RewardMult * atrValue;
				_breakevenLevel = candle.ClosePrice + BreakevenTicks * atrValue;
				_movedToBreakeven = false;
				BuyMarket(Volume);
			}
			else if (shortSignal)
			{
				_entryPrice = candle.ClosePrice;
				_stopPrice = candle.ClosePrice + RiskMult * atrValue;
				_takePrice = candle.ClosePrice - RewardMult * atrValue;
				_breakevenLevel = candle.ClosePrice - BreakevenTicks * atrValue;
				_movedToBreakeven = false;
				SellMarket(Volume);
			}
		}
		else if (Position > 0)
		{
			if (!_movedToBreakeven && EnableBreakeven && candle.HighPrice >= _breakevenLevel)
			{
				_stopPrice = _entryPrice;
				_movedToBreakeven = true;
			}

			if (_movedToBreakeven && EnableTrailing)
			{
				var trail = Math.Max(_stopPrice, candle.ClosePrice - TrailAtrMult * atrValue);
				_stopPrice = trail;
			}

			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			{
				SellMarket(Position);
				ResetTrade();
			}
		}
		else
		{
			if (!_movedToBreakeven && EnableBreakeven && candle.LowPrice <= _breakevenLevel)
			{
				_stopPrice = _entryPrice;
				_movedToBreakeven = true;
			}

			if (_movedToBreakeven && EnableTrailing)
			{
				var trail = Math.Min(_stopPrice, candle.ClosePrice + TrailAtrMult * atrValue);
				_stopPrice = trail;
			}

			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
			{
				BuyMarket(Math.Abs(Position));
				ResetTrade();
			}
		}

		_prevHaClose = haClose;
		_prevMa = maValue;
		_haOpen = haOpen;
		_haClose = haClose;
	}

	private void ResetTrade()
	{
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_breakevenLevel = 0m;
		_movedToBreakeven = false;
	}
}
