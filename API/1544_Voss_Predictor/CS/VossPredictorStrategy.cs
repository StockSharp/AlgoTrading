using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on John Ehlers' Voss predictor.
/// Buys when the predictive filter crosses above the band-pass filter and sells on the opposite cross.
/// </summary>
public class VossPredictorStrategy : Strategy
{
	private readonly StrategyParam<int> _periodBandpass;
	private readonly StrategyParam<decimal> _bandWidth;
	private readonly StrategyParam<decimal> _barsPrediction;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _pricePrev1;
	private decimal? _pricePrev2;
	private decimal _bandPassPrev1;
	private decimal _bandPassPrev2;
	private readonly decimal[] _vossBuffer = new decimal[9];
	private decimal _prevVpf;
	private decimal _prevBpf;

	/// <summary>
	/// Band-pass period.
	/// </summary>
	public int PeriodBandpass { get => _periodBandpass.Value; set => _periodBandpass.Value = value; }

	/// <summary>
	/// Bandwidth coefficient.
	/// </summary>
	public decimal BandWidth { get => _bandWidth.Value; set => _bandWidth.Value = value; }

	/// <summary>
	/// Bars of prediction.
	/// </summary>
	public decimal BarsPrediction { get => _barsPrediction.Value; set => _barsPrediction.Value = value; }

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public VossPredictorStrategy()
	{
		_periodBandpass = Param(nameof(PeriodBandpass), 20)
		.SetGreaterThanZero()
		.SetDisplay("Bandpass Period", "Period for band-pass filter", "Settings")
		.SetCanOptimize(true)
		.SetOptimize(10, 40, 5);

		_bandWidth = Param(nameof(BandWidth), 0.25m)
		.SetGreaterThanZero()
		.SetDisplay("Bandwidth", "Bandwidth coefficient", "Settings")
		.SetCanOptimize(true)
		.SetOptimize(0.05m, 1.0m, 0.05m);

		_barsPrediction = Param(nameof(BarsPrediction), 3.0m)
		.SetGreaterThanZero()
		.SetDisplay("Bars of Prediction", "Look ahead bars", "Settings")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 3.0m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var alpha = 2.0 * Math.PI / PeriodBandpass;
		var cosAlpha = (decimal)Math.Cos(alpha);
		var gamma = Math.Cos(alpha * (double)BandWidth);
		var delta = 1.0 / gamma - Math.Sqrt(1.0 / (gamma * gamma) - 1.0);
		var deltaDec = (decimal)delta;
		var order = (int)(3m * Math.Min(3m, BarsPrediction));

		var subscription = SubscribeCandles(CandleType);

		subscription
		.Bind(candle =>
		{
			if (candle.State != CandleStates.Finished)
			return;

			if (!IsFormedAndOnlineAndAllowTrading())
			return;

			var price = candle.ClosePrice;

			var prev2 = _pricePrev2 ?? _pricePrev1 ?? price;
			var whiten = 0.5m * (price - prev2);
			_pricePrev2 = _pricePrev1;
			_pricePrev1 = price;

			var bandPass = (1m - deltaDec) * whiten
			+ cosAlpha * (1m + deltaDec) * _bandPassPrev1
			- deltaDec * _bandPassPrev2;

			_bandPassPrev2 = _bandPassPrev1;
			_bandPassPrev1 = bandPass;

			decimal e = 0m;
			for (var i = 0; i < order; i++)
			{
			e += _vossBuffer[order - i - 1] * (1m + i) / order;
			}

			var vpf = 0.5m * (3m + order) * bandPass - e;

			for (var i = order - 1; i > 0; i--)
			_vossBuffer[i] = _vossBuffer[i - 1];
			_vossBuffer[0] = vpf;

			var crossUp = _prevVpf <= _prevBpf && vpf > bandPass;
			var crossDown = _prevVpf >= _prevBpf && vpf < bandPass;

			if (crossUp && Position <= 0)
			{
			BuyMarket(Volume + Math.Abs(Position));
			}
			else if (crossDown && Position >= 0)
			{
			SellMarket(Volume + Math.Abs(Position));
			}

			_prevVpf = vpf;
			_prevBpf = bandPass;
		})
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
		DrawCandles(area, subscription);
		DrawOwnTrades(area);
		}
	}
}
