using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gold pullback strategy using EMA trend filter, MACD and TDI confirmation.
/// </summary>
public class GoldPullbackStrategy : Strategy
{
	private readonly StrategyParam<int> _emaFastLength;
	private readonly StrategyParam<int> _emaSlowLength;
	private readonly StrategyParam<int> _emaPullbackLength;
	private readonly StrategyParam<decimal> _slOffset;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _emaFast;
	private ExponentialMovingAverage _emaSlow;
	private ExponentialMovingAverage _emaPullback;
	private MovingAverageConvergenceDivergence _macd;
	private RelativeStrengthIndex _rsi;
	private SimpleMovingAverage _tdiMa;
	private SimpleMovingAverage _tdiSignal;

	private decimal _longStop;
	private decimal _longTake;
	private decimal _shortStop;
	private decimal _shortTake;

	public GoldPullbackStrategy()
	{
		_emaFastLength = Param(nameof(EmaFastLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("EMA Fast", "Fast EMA length", "Indicators")
			.SetCanOptimize(true);

		_emaSlowLength = Param(nameof(EmaSlowLength), 60)
			.SetGreaterThanZero()
			.SetDisplay("EMA Slow", "Slow EMA length", "Indicators")
			.SetCanOptimize(true);

		_emaPullbackLength = Param(nameof(EmaPullbackLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("EMA Pullback", "Pullback EMA length", "Indicators")
			.SetCanOptimize(true);

		_slOffset = Param(nameof(SlOffset), 0.1m)
			.SetGreaterThan(0m)
			.SetDisplay("SL Offset", "Offset for stop calculation", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy", "General");
	}

	public int EmaFastLength { get => _emaFastLength.Value; set => _emaFastLength.Value = value; }
	public int EmaSlowLength { get => _emaSlowLength.Value; set => _emaSlowLength.Value = value; }
	public int EmaPullbackLength { get => _emaPullbackLength.Value; set => _emaPullbackLength.Value = value; }
	public decimal SlOffset { get => _slOffset.Value; set => _slOffset.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_longStop = _longTake = 0m;
		_shortStop = _shortTake = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_emaFast = new ExponentialMovingAverage { Length = EmaFastLength };
		_emaSlow = new ExponentialMovingAverage { Length = EmaSlowLength };
		_emaPullback = new ExponentialMovingAverage { Length = EmaPullbackLength };
		_macd = new MovingAverageConvergenceDivergence
		{
			ShortPeriod = 5,
			LongPeriod = 34,
			SignalPeriod = 5
		};
		_rsi = new RelativeStrengthIndex { Length = 13 };
		_tdiMa = new SimpleMovingAverage { Length = 2 };
		_tdiSignal = new SimpleMovingAverage { Length = 7 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_emaFast, _emaSlow, _emaPullback, _macd, _rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _emaFast);
			DrawIndicator(area, _emaSlow);
			DrawIndicator(area, _emaPullback);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaFast, decimal emaSlow, decimal emaPullback, decimal macd, decimal macdSignal, decimal _, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var tdiMaValue = _tdiMa.Process(new DecimalIndicatorValue(_tdiMa, rsiValue)).ToDecimal();
		var tdiSignalValue = _tdiSignal.Process(new DecimalIndicatorValue(_tdiSignal, rsiValue)).ToDecimal();

		if (!_macd.IsFormed || !_tdiSignal.IsFormed || !_emaFast.IsFormed || !_emaSlow.IsFormed || !_emaPullback.IsFormed)
			return;

		var trendUp = emaFast > emaSlow;
		var trendDown = emaFast < emaSlow;
		var touchEma = candle.LowPrice <= emaPullback && candle.HighPrice >= emaPullback;
		var macdBuy = macd > macdSignal;
		var macdSell = macd < macdSignal;
		var tdiBuy = tdiMaValue > tdiSignalValue && rsiValue > 50m;
		var tdiSell = tdiMaValue < tdiSignalValue && rsiValue < 50m;

		var buySignal = trendUp && touchEma && macdBuy && tdiBuy;
		var sellSignal = trendDown && touchEma && macdSell && tdiSell;

		if (Position > 0)
		{
			if (candle.LowPrice <= _longStop || candle.HighPrice >= _longTake)
			{
				SellMarket(Position);
				_longStop = _longTake = 0m;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _shortStop || candle.LowPrice <= _shortTake)
			{
				BuyMarket(Math.Abs(Position));
				_shortStop = _shortTake = 0m;
			}
		}

		if (buySignal && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			var sl = candle.LowPrice - SlOffset;
			var tp = candle.ClosePrice + (candle.ClosePrice - sl);
			BuyMarket(volume);
			_longStop = sl;
			_longTake = tp;
		}
		else if (sellSignal && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			var sl = candle.HighPrice + SlOffset;
			var tp = candle.ClosePrice - (sl - candle.ClosePrice);
			SellMarket(volume);
			_shortStop = sl;
			_shortTake = tp;
		}
	}
}
