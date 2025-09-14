using System;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on MESA Adaptive Moving Average crossing.
/// Buys when MAMA crosses below FAMA and sells on opposite.
/// </summary>
public class ExpMamaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _fastLimit;
	private readonly StrategyParam<decimal> _slowLimit;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;

	private decimal? _prevMama;
	private decimal? _prevFama;
	private readonly MamaCalculator _calc = new();

	/// <summary>
	/// Fast limit of adaptive factor.
	/// </summary>
	public decimal FastLimit { get => _fastLimit.Value; set => _fastLimit.Value = value; }

	/// <summary>
	/// Slow limit of adaptive factor.
	/// </summary>
	public decimal SlowLimit { get => _slowLimit.Value; set => _slowLimit.Value = value; }

	/// <summary>
	/// Candle type used by the strategy.
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
	/// Allow closing long positions.
	/// </summary>
	public bool BuyClose { get => _buyClose.Value; set => _buyClose.Value = value; }

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool SellClose { get => _sellClose.Value; set => _sellClose.Value = value; }

	public ExpMamaStrategy()
	{
		_fastLimit = Param(nameof(FastLimit), 0.5m)
		.SetDisplay("Fast Limit", "Fast alpha limit", "Indicators")
		.SetCanOptimize(true);
		_slowLimit = Param(nameof(SlowLimit), 0.05m)
		.SetDisplay("Slow Limit", "Slow alpha limit", "Indicators")
		.SetCanOptimize(true);
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe", "General");
		_buyOpen = Param(nameof(BuyOpen), true)
		.SetDisplay("Buy Open", "Allow opening long positions", "Trading");
		_sellOpen = Param(nameof(SellOpen), true)
		.SetDisplay("Sell Open", "Allow opening short positions", "Trading");
		_buyClose = Param(nameof(BuyClose), true)
		.SetDisplay("Buy Close", "Allow closing long positions", "Trading");
		_sellClose = Param(nameof(SellClose), true)
		.SetDisplay("Sell Close", "Allow closing short positions", "Trading");
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

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var price = candle.ClosePrice;
		var result = _calc.Process(price, FastLimit, SlowLimit);
		if (result is null)
		return;

		var (mama, fama) = result.Value;

		if (_prevMama.HasValue && _prevFama.HasValue)
		{
			var wasAbove = _prevMama > _prevFama;
			var isAbove = mama > fama;

			if (wasAbove && !isAbove)
			{
				if (SellClose && Position < 0)
				BuyMarket(Math.Abs(Position));
				if (BuyOpen && Position == 0)
				BuyMarket(Volume);
			}
			else if (!wasAbove && isAbove)
			{
				if (BuyClose && Position > 0)
				SellMarket(Position);
				if (SellOpen && Position == 0)
				SellMarket(Volume);
			}
		}

		_prevMama = mama;
		_prevFama = fama;
	}

	private class MamaCalculator
	{
		private const decimal c0 = 0.0962m;
		private const decimal c1 = 0.5769m;
		private const decimal c2 = -0.5769m;
		private const decimal c3 = -0.0962m;

		private decimal _p1, _p2, _p3;
		private decimal _s1, _s2, _s3;
		private decimal _d1, _d2, _d3;
		private decimal _q1, _q2, _q3;
		private decimal _i1, _i2, _i3;
		private decimal _i21, _q21;
		private decimal _re1, _im1;
		private decimal _phase1;
		private decimal _period;
		private decimal? _mama;
		private decimal? _fama;
		private int _count;

		public (decimal mama, decimal fama)? Process(decimal price, decimal fast, decimal slow)
		{
			_count++;

			var smooth = (4m * price + 3m * _p1 + 2m * _p2 + _p3) / 10m;

			var detrender = c0 * smooth + c1 * _s1 + c2 * _s2 + c3 * _s3;

			var q1 = c0 * detrender + c1 * _d1 + c2 * _d2 + c3 * _d3;
			var i1 = _d1;

			var jI = c0 * i1 + c1 * _i1 + c2 * _i2 + c3 * _i3;
			var jQ = c0 * q1 + c1 * _q1 + c2 * _q2 + c3 * _q3;

			var i2 = i1 - jQ;
			var q2 = q1 + jI;

			i2 = 0.2m * i2 + 0.8m * _i21;
			q2 = 0.2m * q2 + 0.8m * _q21;

			var re = i2 * _i21 + q2 * _q21;
			var im = i2 * _q21 - q2 * _i21;

			re = 0.2m * re + 0.8m * _re1;
			im = 0.2m * im + 0.8m * _im1;

			var period = _period;
			if (re != 0m && im != 0m)
			{
				var ang = (decimal)Math.Atan((double)(im / re));
				if (ang != 0m)
				period = 2m * (decimal)Math.PI / ang;
			}
			period = Math.Min(Math.Max(period, 6m), 50m);

			var phase = 0m;
			if (i1 != 0m)
			phase = (decimal)Math.Atan((double)(q1 / i1));
			var delta = phase - _phase1;
			_phase1 = phase;
			if (delta < 1m)
			delta = 1m;
			if (delta > 1.5m)
			delta = 1.5m;

			var alpha = fast / delta;
			if (alpha < slow)
			alpha = slow;

			var mama = _mama is null ? price : alpha * price + (1m - alpha) * _mama.Value;
			var fama = _fama is null ? price : 0.5m * alpha * mama + (1m - 0.5m * alpha) * _fama.Value;

			_p3 = _p2;
			_p2 = _p1;
			_p1 = price;
			_s3 = _s2;
			_s2 = _s1;
			_s1 = smooth;
			_d3 = _d2;
			_d2 = _d1;
			_d1 = detrender;
			_q3 = _q2;
			_q2 = _q1;
			_q1 = q1;
			_i3 = _i2;
			_i2 = _i1;
			_i1 = i1;
			_i21 = i2;
			_q21 = q2;
			_re1 = re;
			_im1 = im;
			_period = period;
			_mama = mama;
			_fama = fama;

			if (_count < 7)
			return null;

			return (mama, fama);
		}
	}
}
