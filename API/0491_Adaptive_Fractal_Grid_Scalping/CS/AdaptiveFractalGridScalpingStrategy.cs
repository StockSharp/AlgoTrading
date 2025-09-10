using System;
using Ecng.ComponentModel;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that places grid orders based on fractal pivots and ATR.
/// </summary>
public class AdaptiveFractalGridScalpingStrategy : Strategy
{
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<decimal> _gridMultiplierHigh;
	private readonly StrategyParam<decimal> _gridMultiplierLow;
	private readonly StrategyParam<decimal> _trailStopMultiplier;
	private readonly StrategyParam<decimal> _volatilityThreshold;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _h1, _h2, _h3, _h4, _h5;
	private decimal _l1, _l2, _l3, _l4, _l5;
	private decimal? _fractalHigh;
	private decimal? _fractalLow;
	private decimal _stopLevel;
	private decimal _takeProfitLevel;

	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public int SmaLength { get => _smaLength.Value; set => _smaLength.Value = value; }
	public decimal GridMultiplierHigh { get => _gridMultiplierHigh.Value; set => _gridMultiplierHigh.Value = value; }
	public decimal GridMultiplierLow { get => _gridMultiplierLow.Value; set => _gridMultiplierLow.Value = value; }
	public decimal TrailStopMultiplier { get => _trailStopMultiplier.Value; set => _trailStopMultiplier.Value = value; }
	public decimal VolatilityThreshold { get => _volatilityThreshold.Value; set => _volatilityThreshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AdaptiveFractalGridScalpingStrategy()
	{
		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 7);

		_smaLength = Param(nameof(SmaLength), 50)
			.SetDisplay("SMA Length", "SMA period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 10);

		_gridMultiplierHigh = Param(nameof(GridMultiplierHigh), 2m)
			.SetDisplay("Grid Multiplier High", "ATR multiplier for high grid", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_gridMultiplierLow = Param(nameof(GridMultiplierLow), 0.5m)
			.SetDisplay("Grid Multiplier Low", "ATR multiplier for low grid", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.25m, 1m, 0.25m);

		_trailStopMultiplier = Param(nameof(TrailStopMultiplier), 0.5m)
			.SetDisplay("Trail Stop Multiplier", "ATR multiplier for stop", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.25m, 1m, 0.25m);

		_volatilityThreshold = Param(nameof(VolatilityThreshold), 1m)
			.SetDisplay("Volatility Threshold", "ATR threshold", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 2m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Data");

		Volume = 1;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_h1 = _h2 = _h3 = _h4 = _h5 = 0;
		_l1 = _l2 = _l3 = _l4 = _l5 = 0;
		_fractalHigh = null;
		_fractalLow = null;
		_stopLevel = 0;
		_takeProfitLevel = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var atr = new AverageTrueRange { Length = AtrLength };
		var sma = new SimpleMovingAverage { Length = SmaLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(atr, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_h1 = _h2; _h2 = _h3; _h3 = _h4; _h4 = _h5; _h5 = candle.HighPrice;
		_l1 = _l2; _l2 = _l3; _l3 = _l4; _l4 = _l5; _l5 = candle.LowPrice;

		if (_h3 > _h1 && _h3 > _h2 && _h3 > _h4 && _h3 > _h5)
			_fractalHigh = _h3;

		if (_l3 < _l1 && _l3 < _l2 && _l3 < _l4 && _l3 < _l5)
			_fractalLow = _l3;

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopLevel || candle.HighPrice >= _takeProfitLevel)
				RegisterOrder(CreateOrder(Sides.Sell, candle.ClosePrice, Math.Abs(Position)));
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopLevel || candle.LowPrice <= _takeProfitLevel)
				RegisterOrder(CreateOrder(Sides.Buy, candle.ClosePrice, Math.Abs(Position)));
		}
		else
		{
			CancelActiveOrders();

			if (atrValue > VolatilityThreshold)
			{
				var isBullish = candle.ClosePrice > smaValue;
				var isBearish = candle.ClosePrice < smaValue;

				if (isBullish && _fractalLow is decimal fl)
				{
					var gridLow = fl - atrValue * GridMultiplierLow;
					var gridHigh = (_fractalHigh ?? fl) + atrValue * GridMultiplierHigh;

					RegisterOrder(CreateOrder(Sides.Buy, gridLow, Volume));
					_stopLevel = fl - atrValue * TrailStopMultiplier;
					_takeProfitLevel = gridHigh;
				}
				else if (isBearish && _fractalHigh is decimal fh)
				{
					var gridHigh = fh + atrValue * GridMultiplierHigh;
					var gridLow = (_fractalLow ?? fh) - atrValue * GridMultiplierLow;

					RegisterOrder(CreateOrder(Sides.Sell, gridHigh, Volume));
					_stopLevel = fh + atrValue * TrailStopMultiplier;
					_takeProfitLevel = gridLow;
				}
			}
		}
	}
}
