using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Sniper Trade Pro strategy.
/// Combines EMA trend filter, VWAP, ADX and money flow divergence with candlestick engulfing patterns.
/// </summary>
public class SniperTradeProStrategy : Strategy
{
	private readonly StrategyParam<int> _emaFastLength;
	private readonly StrategyParam<int> _emaSlowLength;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<decimal> _stopAtrMultiplier;
	private readonly StrategyParam<decimal> _takeAtrMultiplier;
	private readonly StrategyParam<decimal> _riskPerTrade;
	private readonly StrategyParam<decimal> _tickValue;
	private readonly StrategyParam<decimal> _maxDailyLoss;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _emaFast;
	private ExponentialMovingAverage _emaSlow;
	private VolumeWeightedMovingAverage _vwap;
	private AverageDirectionalIndex _adx;
	private AverageTrueRange _atr;
	private Momentum _momentum;
	private ExponentialMovingAverage _mfdEma;
	private SimpleMovingAverage _mfdSignal;

	private decimal _prevOpen;
	private decimal _prevClose;
	private decimal _prevHigh;
	private decimal _prevLow;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private bool _breakevenSet;

	private decimal _dailyProfit;
	private decimal _prevPnL;
	private DateTime _currentDate;

	public int EmaFastLength { get => _emaFastLength.Value; set => _emaFastLength.Value = value; }
	public int EmaSlowLength { get => _emaSlowLength.Value; set => _emaSlowLength.Value = value; }
	public int AdxLength { get => _adxLength.Value; set => _adxLength.Value = value; }
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }
	public decimal StopAtrMultiplier { get => _stopAtrMultiplier.Value; set => _stopAtrMultiplier.Value = value; }
	public decimal TakeAtrMultiplier { get => _takeAtrMultiplier.Value; set => _takeAtrMultiplier.Value = value; }
	public decimal RiskPerTrade { get => _riskPerTrade.Value; set => _riskPerTrade.Value = value; }
	public decimal TickValue { get => _tickValue.Value; set => _tickValue.Value = value; }
	public decimal MaxDailyLoss { get => _maxDailyLoss.Value; set => _maxDailyLoss.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SniperTradeProStrategy()
	{
		_emaFastLength = Param(nameof(EmaFastLength), 9);
		_emaSlowLength = Param(nameof(EmaSlowLength), 21);
		_adxLength = Param(nameof(AdxLength), 14);
		_adxThreshold = Param(nameof(AdxThreshold), 20m);
		_stopAtrMultiplier = Param(nameof(StopAtrMultiplier), 0.8m);
		_takeAtrMultiplier = Param(nameof(TakeAtrMultiplier), 2m);
		_riskPerTrade = Param(nameof(RiskPerTrade), 400m);
		_tickValue = Param(nameof(TickValue), 12.5m);
		_maxDailyLoss = Param(nameof(MaxDailyLoss), -1000m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_prevOpen = 0m;
		_prevClose = 0m;
		_prevHigh = 0m;
		_prevLow = 0m;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_breakevenSet = false;
		_dailyProfit = 0m;
		_prevPnL = 0m;
		_currentDate = default;
		_mfdEma?.Reset();
		_mfdSignal?.Reset();
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_emaFast = new ExponentialMovingAverage { Length = EmaFastLength };
		_emaSlow = new ExponentialMovingAverage { Length = EmaSlowLength };
		_vwap = new VolumeWeightedMovingAverage();
		_adx = new AverageDirectionalIndex { Length = AdxLength };
		_atr = new AverageTrueRange { Length = 14 };
		_momentum = new Momentum { Length = 14 };
		_mfdEma = new ExponentialMovingAverage { Length = 10 };
		_mfdSignal = new SimpleMovingAverage { Length = 10 };

		var sub = SubscribeCandles(CandleType);
		sub
			.BindEx(_emaFast, _emaSlow, _vwap, _atr, _momentum, _adx, Process)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawIndicator(area, _emaFast);
			DrawIndicator(area, _emaSlow);
			DrawIndicator(area, _vwap);
			DrawOwnTrades(area);
		}
	}

	private void Process(ICandleMessage candle, IIndicatorValue emaFastVal, IIndicatorValue emaSlowVal, IIndicatorValue vwapVal, IIndicatorValue atrVal, IIndicatorValue momentumVal, IIndicatorValue adxVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var realizedPnL = PnL;
		if (realizedPnL != _prevPnL)
		{
			_dailyProfit += realizedPnL - _prevPnL;
			_prevPnL = realizedPnL;
		}

		var date = candle.OpenTime.Date;
		if (date != _currentDate)
		{
			_dailyProfit = 0m;
			_currentDate = date;
		}

		if (!emaFastVal.IsFinal || !emaSlowVal.IsFinal || !vwapVal.IsFinal || !atrVal.IsFinal || !momentumVal.IsFinal || !adxVal.IsFinal)
		{
			_prevOpen = candle.OpenPrice;
			_prevClose = candle.ClosePrice;
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			return;
		}

		var emaFast = emaFastVal.ToDecimal();
		var emaSlow = emaSlowVal.ToDecimal();
		var vwap = vwapVal.ToDecimal();
		var atr = atrVal.ToDecimal();
		var mom = momentumVal.ToDecimal();

		var adxTyped = (AverageDirectionalIndexValue)adxVal;
		if (adxTyped.MovingAverage is not decimal adx)
		{
			_prevOpen = candle.OpenPrice;
			_prevClose = candle.ClosePrice;
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			return;
		}

		var mfd = _mfdEma.Process(mom).ToDecimal();
		var mfdAvg = _mfdSignal.Process(mfd).ToDecimal();
		var mfdSignalVal = mfd > mfdAvg ? 1 : -1;

		var time = candle.OpenTime;
		var start = time.Date + TimeSpan.FromHours(9.5);
		var end = time.Date + TimeSpan.FromHours(12);
		var inSession = time >= start && time <= end;

		var bullEngulfing = _prevOpen != 0m && candle.ClosePrice > _prevOpen && candle.OpenPrice < _prevClose && (candle.HighPrice - candle.LowPrice) > (_prevHigh - _prevLow);
		var bearEngulfing = _prevOpen != 0m && candle.ClosePrice < _prevOpen && candle.OpenPrice > _prevClose && (candle.HighPrice - candle.LowPrice) > (_prevHigh - _prevLow);

		var withinRiskLimits = _dailyProfit > MaxDailyLoss;

		var buySignal = inSession && emaFast > emaSlow && candle.ClosePrice > vwap && mfdSignalVal == 1 && bullEngulfing && adx > AdxThreshold;
		var sellSignal = inSession && emaFast < emaSlow && candle.ClosePrice < vwap && mfdSignalVal == -1 && bearEngulfing && adx > AdxThreshold;

		if (buySignal && withinRiskLimits && Position <= 0)
		{
			var volume = Math.Max(1m, Math.Floor(RiskPerTrade / (atr * TickValue)));
			BuyMarket(volume);
			_entryPrice = candle.ClosePrice;
			_stopPrice = candle.ClosePrice - atr * StopAtrMultiplier;
			_takePrice = candle.ClosePrice + atr * TakeAtrMultiplier;
			_breakevenSet = false;
		}
		else if (sellSignal && withinRiskLimits && Position >= 0)
		{
			var volume = Math.Max(1m, Math.Floor(RiskPerTrade / (atr * TickValue)));
			SellMarket(volume);
			_entryPrice = candle.ClosePrice;
			_stopPrice = candle.ClosePrice + atr * StopAtrMultiplier;
			_takePrice = candle.ClosePrice - atr * TakeAtrMultiplier;
			_breakevenSet = false;
		}
		else if (Position > 0)
		{
			if (!_breakevenSet && candle.ClosePrice >= _entryPrice + atr)
			{
				_stopPrice = _entryPrice;
				_breakevenSet = true;
			}

			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			{
				SellMarket(Position);
			}
		}
		else if (Position < 0)
		{
			if (!_breakevenSet && candle.ClosePrice <= _entryPrice - atr)
			{
				_stopPrice = _entryPrice;
				_breakevenSet = true;
			}

			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
			{
				BuyMarket(-Position);
			}
		}

		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
	}
}
