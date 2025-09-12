using System;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy estimating the Nth percentile using the P-Square algorithm.
/// Enters long when value exceeds the upper percentile and short when it falls below the lower percentile.
/// </summary>
public class PSquareNthPercentileStrategy : Strategy
{
	private readonly StrategyParam<decimal> _percentile;
	private readonly StrategyParam<bool> _useReturns;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private bool _isFirst = true;
	private int _count;
	private decimal _mean;
	private decimal _m2;
	private PSquareEstimator _upper;
	private PSquareEstimator _lower;

	/// <summary>
	/// Percentile to estimate.
	/// </summary>
	public decimal Percentile { get => _percentile.Value; set => _percentile.Value = value; }

	/// <summary>
	/// Use returns instead of raw price.
	/// </summary>
	public bool UseReturns { get => _useReturns.Value; set => _useReturns.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public PSquareNthPercentileStrategy()
	{
		_percentile = Param(nameof(Percentile), 84.1m).SetDisplay("Percentile");
		_useReturns = Param(nameof(UseReturns), true).SetDisplay("Use returns");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_upper = new PSquareEstimator(Percentile);
		_lower = new PSquareEstimator(100m - Percentile);

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		decimal value;
		if (UseReturns)
		{
			if (_isFirst)
			{
				_prevClose = close;
				_isFirst = false;
				return;
			}

			value = 100m * (close / _prevClose - 1m);
			_prevClose = close;
		}
		else
		{
			value = close;
		}

		_count++;
		var delta = value - _mean;
		_mean += delta / _count;
		var delta2 = value - _mean;
		_m2 += delta * delta2;
		var stdev = _count > 1 ? (decimal)Math.Sqrt((double)(_m2 / (_count - 1))) : 0m;

		var upper = _upper.Process(value);
		var lower = _lower.Process(value);

		if (upper is null || lower is null)
			return;

		if (value > upper && Position <= 0)
			BuyMarket();
		else if (value < lower && Position >= 0)
			SellMarket();
	}

	private sealed class PSquareEstimator
	{
		private readonly decimal _p;
		private int _count;
		private readonly decimal[] _q = new decimal[5];
		private readonly int[] _n = new int[5];
		private readonly decimal[] _np = new decimal[5];
		private readonly decimal[] _dn = new decimal[5];

		public PSquareEstimator(decimal percentile)
		{
			_p = percentile / 100m;

			_dn[0] = 0m;
			_dn[1] = _p * 0.5m;
			_dn[2] = _p;
			_dn[3] = (1m + _p) * 0.5m;
			_dn[4] = 1m;

			for (var i = 0; i < 5; i++)
			{
				_n[i] = i + 1;
				_np[i] = _dn[i] * 4m + 1m;
			}
		}

		public decimal? Process(decimal value)
		{
			if (_p <= 0m)
				return null;

			_count++;

			if (_count <= 5)
			{
				_q[_count - 1] = value;

				if (_count == 5)
					Array.Sort(_q);

				return _q[2];
			}

			var k = 0;
			while (k < 5 && value >= _q[k])
				k++;

			if (k == 0)
			{
				_q[0] = value;
				k = 1;
			}
			else if (k == 5)
			{
				k = 4;
				_q[4] = value;
			}

			for (var i = k; i < 5; i++)
				_n[i]++;

			for (var i = 0; i < 5; i++)
				_np[i] += _dn[i];

			for (var i = 1; i <= 3; i++)
			{
				var d = _np[i] - _n[i];
				if ((d >= 1m && _n[i + 1] - _n[i] > 1) || (d <= -1m && _n[i - 1] - _n[i] < -1))
				{
					var ds = Math.Sign(d);
					var qi = _q[i];
					var qip1 = _q[i + 1];
					var qim1 = _q[i - 1];

					var ni = (decimal)_n[i];
					var nip1 = (decimal)_n[i + 1];
					var nim1 = (decimal)_n[i - 1];

					var df = (decimal)ds;
					var qp = qi + df / (nip1 - nim1) * ((ni - nim1 + df) * (qip1 - qi) / (nip1 - ni) + (nip1 - ni - df) * (qi - qim1) / (ni - nim1));

					if (qim1 < qp && qp < qip1)
						_q[i] = qp;
					else
						_q[i] = qi + df * (_q[i + ds] - qi) / (_n[i + ds] - _n[i]);

					_n[i] += ds;
				}
			}

			return _q[2];
		}
	}
}
