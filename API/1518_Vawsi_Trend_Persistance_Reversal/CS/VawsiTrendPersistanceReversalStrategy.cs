using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// VAWSI and Trend Persistance Reversal Strategy.
/// Combines RSI, trend persistence and ATR to build a dynamic threshold.
/// Long when Heikin-Ashi close crosses above threshold, short when below.
/// </summary>
public class VawsiTrendPersistanceReversalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _slTp;
	private readonly StrategyParam<decimal> _rsiWeight;
	private readonly StrategyParam<decimal> _trendWeight;
	private readonly StrategyParam<decimal> _atrWeight;
	private readonly StrategyParam<decimal> _combinationMult;
	private readonly StrategyParam<int> _smoothing;
	private readonly StrategyParam<int> _cycleLength;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private AverageTrueRange _atr;
	private SimpleMovingAverage _trendSma;

	private decimal _thresh;
	private decimal _prevHaClose;
	private int _direction = 1;
	private int _barsSinceCross;
	private decimal _highestSinceCross;
	private decimal _lowestSinceCross;

	/// <summary>
	/// Minimum stop loss / take profit percent.
	/// </summary>
	public decimal SlTp { get => _slTp.Value; set => _slTp.Value = value; }

	/// <summary>
	/// Weight for RSI component.
	/// </summary>
	public decimal RsiWeight { get => _rsiWeight.Value; set => _rsiWeight.Value = value; }

	/// <summary>
	/// Weight for trend persistence component.
	/// </summary>
	public decimal TrendWeight { get => _trendWeight.Value; set => _trendWeight.Value = value; }

	/// <summary>
	/// Weight for ATR component.
	/// </summary>
	public decimal AtrWeight { get => _atrWeight.Value; set => _atrWeight.Value = value; }

	/// <summary>
	/// Combination multiplier.
	/// </summary>
	public decimal CombinationMult { get => _combinationMult.Value; set => _combinationMult.Value = value; }

	/// <summary>
	/// Smoothing length for trend persistence.
	/// </summary>
	public int Smoothing { get => _smoothing.Value; set => _smoothing.Value = value; }

	/// <summary>
	/// Cycle length for calculations.
	/// </summary>
	public int CycleLength { get => _cycleLength.Value; set => _cycleLength.Value = value; }

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="VawsiTrendPersistanceReversalStrategy"/> class.
	/// </summary>
	public VawsiTrendPersistanceReversalStrategy()
	{
		_slTp = Param(nameof(SlTp), 5m)
			.SetDisplay("SL/TP", "Stop loss and take profit percent", "Risk Management")
			.SetRange(1m, 10m)
			.SetCanOptimize(true);

		_rsiWeight = Param(nameof(RsiWeight), 100m)
			.SetDisplay("RSI Weight", "Weight of RSI component", "Weights")
			.SetRange(0m, 200m);

		_trendWeight = Param(nameof(TrendWeight), 79m)
			.SetDisplay("Trend Weight", "Weight of trend persistence", "Weights")
			.SetRange(0m, 200m);

		_atrWeight = Param(nameof(AtrWeight), 20m)
			.SetDisplay("ATR Weight", "Weight of ATR component", "Weights")
			.SetRange(0m, 200m);

		_combinationMult = Param(nameof(CombinationMult), 1m)
			.SetDisplay("Combination Mult", "Multiplier for final value", "Weights")
			.SetRange(0m, 5m);

		_smoothing = Param(nameof(Smoothing), 3)
			.SetDisplay("Smoothing", "Trend smoothing length", "General")
			.SetRange(1, 20);

		_cycleLength = Param(nameof(CycleLength), 20)
			.SetDisplay("Cycle Length", "Lookback length", "General")
			.SetRange(1, 100);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_rsi = new RelativeStrengthIndex { Length = CycleLength };
		_atr = new AverageTrueRange { Length = CycleLength };
		_trendSma = new SimpleMovingAverage { Length = Smoothing };
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

		_rsi = new RelativeStrengthIndex { Length = CycleLength };
		_atr = new AverageTrueRange { Length = CycleLength };
		_trendSma = new SimpleMovingAverage { Length = Smoothing };
		_thresh = 0m;
		_prevHaClose = 0m;
		_direction = 1;
		_barsSinceCross = 0;
		_highestSinceCross = 0m;
		_lowestSinceCross = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection(
			takeProfit: new Unit(SlTp, UnitTypes.Percent),
			stopLoss: new Unit(SlTp, UnitTypes.Percent),
			isStopTrailing: false,
			useMarketOrders: true
		);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;

		if (_barsSinceCross == 0 && _thresh == 0m)
		{
			_thresh = haClose;
			_prevHaClose = haClose;
			_highestSinceCross = candle.HighPrice;
			_lowestSinceCross = candle.LowPrice;
			return;
		}

		_barsSinceCross++;
		_highestSinceCross = Math.Max(_highestSinceCross, candle.HighPrice);
		_lowestSinceCross = Math.Min(_lowestSinceCross, candle.LowPrice);

		var trendUp = Math.Abs(haClose - _highestSinceCross);
		var trendDown = Math.Abs(haClose - _lowestSinceCross);
		var trend = Math.Max(trendUp, trendDown);
		var trendSmoothed = _trendSma.Process(trend, candle.ServerTime, true).ToDecimal();
		var ce = trendSmoothed <= 0m ? 0m : 1m / trendSmoothed;

		var rsiWeight = RsiWeight / 100m;
		var trendWeight = TrendWeight / 100m;
		var atrWeight = AtrWeight / 100m;

		var com = 1m / Math.Max(Math.Abs(rsi - 50m) * 2m, 20m);
		var atrNorm = atr / Math.Max(candle.ClosePrice, 1m);
		var comFin = ((com * rsiWeight) + (ce * trendWeight) - atrNorm * atrWeight) * CombinationMult;

		var sl = candle.ClosePrice * (1m - SlTp / 100m);
		var tp = candle.ClosePrice * (1m + SlTp / 100m);

		var lower = Math.Min(Math.Max(_highestSinceCross * (1m - comFin), sl), candle.OpenPrice);
		var upper = Math.Max(Math.Min(_lowestSinceCross * (1m + comFin), tp), candle.OpenPrice);

		_thresh = _direction == 1 ? lower : upper;

		var crossUp = _prevHaClose <= _thresh && haClose > _thresh;
		var crossDown = _prevHaClose >= _thresh && haClose < _thresh;

		if (crossUp)
		{
			_direction = 1;
			_barsSinceCross = 0;
			_highestSinceCross = candle.HighPrice;
			_lowestSinceCross = candle.LowPrice;

			if (Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}
		else if (crossDown)
		{
			_direction = -1;
			_barsSinceCross = 0;
			_highestSinceCross = candle.HighPrice;
			_lowestSinceCross = candle.LowPrice;

			if (Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_prevHaClose = haClose;
	}
}
