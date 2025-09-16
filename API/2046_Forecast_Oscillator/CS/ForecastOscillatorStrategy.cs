using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the Forecast Oscillator indicator.
/// </summary>
public class ForecastOscillatorStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _t3;
	private readonly StrategyParam<decimal> _b;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	
	private LinearRegression _linReg = null!;
	
	private decimal _b2;
	private decimal _b3;
	private decimal _c1;
	private decimal _c2;
	private decimal _c3;
	private decimal _c4;
	private decimal _w1;
	private decimal _w2;
	
	private decimal _e1;
	private decimal _e2;
	private decimal _e3;
	private decimal _e4;
	private decimal _e5;
	private decimal _e6;
	
	private decimal? _forecastPrev1;
	private decimal? _forecastPrev2;
	private decimal? _sigPrev1;
	private decimal? _sigPrev2;
	private decimal? _sigPrev3;
	
	/// <summary>
	/// Regression length for the baseline.
	/// </summary>
	public int Length { get => _length.Value; set => _length.Value = value; }
	
	/// <summary>
	/// T3 smoothing period.
	/// </summary>
	public int T3 { get => _t3.Value; set => _t3.Value = value; }
	
	/// <summary>
	/// T3 smoothing factor.
	/// </summary>
	public decimal B { get => _b.Value; set => _b.Value = value; }
	
	/// <summary>
	/// Candles type used for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyOpen { get => _buyOpen.Value; set => _buyOpen.Value = value; }
	
	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellOpen { get => _sellOpen.Value; set => _sellOpen.Value = value; }
	
	/// <summary>
	/// Allow closing short positions on buy signal.
	/// </summary>
	public bool BuyClose { get => _buyClose.Value; set => _buyClose.Value = value; }
	
	/// <summary>
	/// Allow closing long positions on sell signal.
	/// </summary>
	public bool SellClose { get => _sellClose.Value; set => _sellClose.Value = value; }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="ForecastOscillatorStrategy"/> class.
	/// </summary>
	public ForecastOscillatorStrategy()
	{
		_length = Param(nameof(Length), 15).SetGreaterThanZero().SetDisplay("Length", "Regression length", "Indicators");
		_t3 = Param(nameof(T3), 3).SetGreaterThanZero().SetDisplay("T3 Period", "T3 smoothing period", "Indicators");
		_b = Param(nameof(B), 0.7m).SetDisplay("T3 Factor", "T3 smoothing factor", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(12).TimeFrame()).SetDisplay("Candle Type", "Type of candles", "General");
		_buyOpen = Param(nameof(BuyOpen), true).SetDisplay("Buy Open", "Allow opening long positions", "Trading");
		_sellOpen = Param(nameof(SellOpen), true).SetDisplay("Sell Open", "Allow opening short positions", "Trading");
		_buyClose = Param(nameof(BuyClose), true).SetDisplay("Buy Close", "Allow closing short positions", "Trading");
		_sellClose = Param(nameof(SellClose), true).SetDisplay("Sell Close", "Allow closing long positions", "Trading");
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
		
		_linReg = new LinearRegression { Length = Length };
		
		// Pre-calculate constants for T3 smoothing
		_b2 = B * B;
		_b3 = _b2 * B;
		_c1 = -_b3;
		_c2 = 3m * (_b2 + _b3);
		_c3 = -3m * (2m * _b2 + B + _b3);
		_c4 = 1m + 3m * B + _b3 + 3m * _b2;
		
		var n = Math.Max((decimal)T3, T3);
		n = 1m + 0.5m * (n - 1m);
		_w1 = 2m / (n + 1m);
		_w2 = 1m - _w1;
		
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
		
		StartProtection();
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var price = candle.ClosePrice;
		var lrValue = (LinearRegressionValue)_linReg.Process(price, candle.OpenTime, true);
		var wt = lrValue.LinearReg;
		
		if (wt is null)
		return;
		
		var forecast = (price - wt.Value) / wt.Value * 100m;
		
		// T3 smoothing
		_e1 = _w1 * forecast + _w2 * _e1;
		_e2 = _w1 * _e1 + _w2 * _e2;
		_e3 = _w1 * _e2 + _w2 * _e3;
		_e4 = _w1 * _e3 + _w2 * _e4;
		_e5 = _w1 * _e4 + _w2 * _e5;
		_e6 = _w1 * _e5 + _w2 * _e6;
		var t3 = _c1 * _e6 + _c2 * _e5 + _c3 * _e4 + _c4 * _e3;
		
		// Cross detection
		var buySignal = _forecastPrev1 > _sigPrev2 && _forecastPrev2 <= _sigPrev3 && _sigPrev1 < 0;
		var sellSignal = _forecastPrev1 < _sigPrev2 && _forecastPrev2 >= _sigPrev3 && _sigPrev1 > 0;
		
		if (buySignal)
		{
			// Close short if allowed
			if (SellClose && Position < 0)
			BuyMarket(Math.Abs(Position));
			// Open long if allowed
			if (BuyOpen && Position <= 0)
			BuyMarket(Volume);
		}
		
		if (sellSignal)
		{
			// Close long if allowed
			if (BuyClose && Position > 0)
			SellMarket(Position);
			// Open short if allowed
			if (SellOpen && Position >= 0)
			SellMarket(Volume);
		}
		
		// Shift previous values
		_forecastPrev2 = _forecastPrev1;
		_forecastPrev1 = forecast;
		_sigPrev3 = _sigPrev2;
		_sigPrev2 = _sigPrev1;
		_sigPrev1 = t3;
	}
}
