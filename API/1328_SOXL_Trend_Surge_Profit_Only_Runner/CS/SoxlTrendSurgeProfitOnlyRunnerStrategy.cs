using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class SoxlTrendSurgeProfitOnlyRunnerStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultTarget;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<decimal> _supertrendFactor;
	private readonly StrategyParam<int> _supertrendAtrPeriod;
	private readonly StrategyParam<int> _minBarsHeld;
	private readonly StrategyParam<int> _volFilterLen;
	private readonly StrategyParam<decimal> _emaBuffer;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema;
	private AverageTrueRange _atr;
	private SuperTrend _supertrend;
	private SimpleMovingAverage _atrSma;
	private SimpleMovingAverage _volSma;

	private int _barIndex;
	private decimal _entryPrice;
	private int _entryBar;
	private bool _tookPartial;
	private int _lastExitBar;
	private decimal _maxPriceSincePartial;

	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal AtrMultTarget { get => _atrMultTarget.Value; set => _atrMultTarget.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }
	public decimal SupertrendFactor { get => _supertrendFactor.Value; set => _supertrendFactor.Value = value; }
	public int SupertrendAtrPeriod { get => _supertrendAtrPeriod.Value; set => _supertrendAtrPeriod.Value = value; }
	public int MinBarsHeld { get => _minBarsHeld.Value; set => _minBarsHeld.Value = value; }
	public int VolFilterLen { get => _volFilterLen.Value; set => _volFilterLen.Value = value; }
	public decimal EmaBuffer { get => _emaBuffer.Value; set => _emaBuffer.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SoxlTrendSurgeProfitOnlyRunnerStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(100, 300, 50);

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 2);

		_atrMultTarget = Param(nameof(AtrMultTarget), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Target", "ATR multiple for partial exit", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 4m, 0.5m);

		_cooldownBars = Param(nameof(CooldownBars), 15)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown Bars", "Bars to wait after exit", "General");

		_supertrendFactor = Param(nameof(SupertrendFactor), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Supertrend Factor", "Multiplier for Supertrend", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(2m, 4m, 0.5m);

		_supertrendAtrPeriod = Param(nameof(SupertrendAtrPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Supertrend ATR Period", "ATR period for Supertrend", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_minBarsHeld = Param(nameof(MinBarsHeld), 2)
			.SetGreaterThanZero()
			.SetDisplay("Min Bars Held", "Minimum bars before exit", "Risk");

		_volFilterLen = Param(nameof(VolFilterLen), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume MA Length", "Length for volume filter", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_emaBuffer = Param(nameof(EmaBuffer), 0.005m)
			.SetGreaterThanZero()
			.SetDisplay("EMA Buffer %", "No-trade buffer around EMA", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_barIndex = 0;
		_entryPrice = 0m;
		_entryBar = -1;
		_tookPartial = false;
		_maxPriceSincePartial = 0m;
		_atrSma = null;
		_volSma = null;
		_ema = null;
		_atr = null;
		_supertrend = null;
		_lastExitBar = -CooldownBars - 1;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema = new ExponentialMovingAverage { Length = EmaLength };
		_atr = new AverageTrueRange { Length = AtrLength };
		_supertrend = new SuperTrend { Length = SupertrendAtrPeriod, Multiplier = SupertrendFactor };
		_atrSma = new SimpleMovingAverage { Length = 20 };
		_volSma = new SimpleMovingAverage { Length = VolFilterLen };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_ema, _atr, _supertrend, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawIndicator(area, _supertrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue emaVal, IIndicatorValue atrVal, IIndicatorValue stVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		var emaValue = emaVal.GetValue<decimal>();
		var atrValue = atrVal.GetValue<decimal>();
		var atrSmaValue = _atrSma.Process(atrValue, candle.ServerTime, true).ToDecimal();
		var volSmaValue = _volSma.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();
		var st = (SuperTrendIndicatorValue)stVal;
		var isUpTrend = st.IsUpTrend;

		if (!_ema.IsFormed || !_atr.IsFormed || !_atrSma.IsFormed || !_volSma.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var volOk = candle.TotalVolume > volSmaValue;
		var atrRising = atrValue > atrSmaValue;
		var hour = candle.OpenTime.Hour;
		var hourOk = hour >= 14 && hour <= 19;
		var outsideBuffer = Math.Abs(candle.ClosePrice - emaValue) / emaValue > EmaBuffer;
		var canTrade = _barIndex - _lastExitBar > CooldownBars;

		var longCondition = candle.ClosePrice > emaValue && isUpTrend && hourOk && volOk && outsideBuffer && atrRising && canTrade;

		if (longCondition)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
			_entryBar = _barIndex;
			_tookPartial = false;
			_maxPriceSincePartial = candle.ClosePrice;
		}

		var barsHeld = _barIndex - _entryBar;
		var partialTarget = _entryPrice + atrValue * AtrMultTarget;
		var takePartial = !_tookPartial && Position > 0 && candle.ClosePrice >= partialTarget && barsHeld >= MinBarsHeld;

		if (takePartial)
		{
			SellMarket(Position / 2m);
			_tookPartial = true;
			_maxPriceSincePartial = candle.ClosePrice;
		}

		if (_tookPartial && Position > 0)
		{
			if (candle.HighPrice > _maxPriceSincePartial)
				_maxPriceSincePartial = candle.HighPrice;

			var trailOffset = atrValue * 1.5m;
			var trailStop = _maxPriceSincePartial - trailOffset;

			if (candle.ClosePrice <= trailStop)
			{
				SellMarket(Position);
				_lastExitBar = _barIndex;
				_entryBar = -1;
				_entryPrice = 0m;
				_tookPartial = false;
				_maxPriceSincePartial = 0m;
			}
		}
	}
}
