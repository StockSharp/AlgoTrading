using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Dispersion trading strategy.
	/// Trades an equity index against its constituent securities when the average correlation falls below a threshold.
	/// </summary>
	public class DispersionTradingStrategy : Strategy
	{
		private readonly StrategyParam<IEnumerable<Security>> _constituents;
		private readonly StrategyParam<int> _lookbackDays;
		private readonly StrategyParam<decimal> _corrThreshold;
		private readonly StrategyParam<decimal> _minTradeUsd;
		private readonly StrategyParam<DataType> _candleType;

		private readonly Dictionary<Security, RollingWindow<decimal>> _windows = new();
		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private DateTime _lastDay = DateTime.MinValue;
		private bool _open;

		/// <summary>
		/// Securities representing index constituents.
		/// </summary>
		public IEnumerable<Security> Constituents
		{
			get => _constituents.Value;
			set => _constituents.Value = value;
		}

		/// <summary>
		/// Number of days used for correlation calculation.
		/// </summary>
		public int LookbackDays
		{
			get => _lookbackDays.Value;
			set => _lookbackDays.Value = value;
		}

		/// <summary>
		/// Correlation threshold for opening dispersion.
		/// </summary>
		public decimal CorrThreshold
		{
			get => _corrThreshold.Value;
			set => _corrThreshold.Value = value;
		}

		/// <summary>
		/// Minimum trade value in USD.
		/// </summary>
		public decimal MinTradeUsd
		{
			get => _minTradeUsd.Value;
			set => _minTradeUsd.Value = value;
		}

		/// <summary>
		/// Candle type used for analysis.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of <see cref="DispersionTradingStrategy"/>.
		/// </summary>
		public DispersionTradingStrategy()
		{
			_constituents = Param<IEnumerable<Security>>(nameof(Constituents), Array.Empty<Security>())
				.SetDisplay("Constituents", "Index constituent securities", "General");

			_lookbackDays = Param(nameof(LookbackDays), 60)
				.SetDisplay("Lookback Days", "Days for rolling correlation", "Parameters");

			_corrThreshold = Param(nameof(CorrThreshold), 0.4m)
				.SetDisplay("Correlation Threshold", "Average correlation threshold", "Parameters");

			_minTradeUsd = Param(nameof(MinTradeUsd), 100m)
				.SetDisplay("Minimum Trade USD", "Minimal order value", "Risk");

			_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
				.SetDisplay("Candle Type", "Time frame for analysis", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return Constituents.Append(Security).Select(s => (s, CandleType));
		}

		/// <inheritdoc />
		
		protected override void OnReseted()
		{
			base.OnReseted();

			_windows.Clear();
			_latestPrices.Clear();
			_lastDay = default;
			_open = default;
		}

		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			if (Security == null)
				throw new InvalidOperationException("IndexSec is not set.");

			if (Constituents == null || !Constituents.Any())
				throw new InvalidOperationException("Constituents collection is empty.");

			foreach (var (sec, dt) in GetWorkingSecurities())
			{
				_windows[sec] = new RollingWindow<decimal>(LookbackDays + 1);

				SubscribeCandles(dt, true, sec)
					.Bind(c => ProcessCandle(c, sec))
					.Start();
			}
		}

		private void ProcessCandle(ICandleMessage candle, Security security)
		{
			// Skip unfinished candles.
			if (candle.State != CandleStates.Finished)
				return;

			// Store the latest closing price for this security.
			_latestPrices[security] = candle.ClosePrice;

			_windows[security].Add(candle.ClosePrice);

			var day = candle.OpenTime.Date;
			if (day == _lastDay)
				return;

			_lastDay = day;

			if (_windows.Values.Any(w => !w.IsFull()))
				return;

			// Daily check after windows are full.
			EvaluateSignal();
		}

		private void EvaluateSignal()
		{
			var indexRet = Returns(_windows[Security]);

			var corrs = new List<decimal>();
			foreach (var s in Constituents)
				corrs.Add(Corr(Returns(_windows[s]), indexRet));

			var avg = corrs.Average();

			if (avg < CorrThreshold && !_open)
				OpenDispersion();
			else if (avg >= CorrThreshold && _open)
				CloseAll();
		}

		private void OpenDispersion()
		{
			var count = Constituents.Count();
			var portfolioValue = Portfolio.CurrentValue ?? 0m;
			var capLeg = portfolioValue * 0.5m;
			var eachLong = capLeg / count;

			foreach (var s in Constituents)
			{
				var price = GetLatestPrice(s);
				if (price > 0)
					TradeToTarget(s, eachLong / price);
			}

			var indexPrice = GetLatestPrice(Security);
			if (indexPrice > 0)
				TradeToTarget(Security, -capLeg / indexPrice); // short index

			_open = true;
			LogInfo("Opened dispersion spread");
		}

		private void CloseAll()
		{
			foreach (var position in Positions)
				TradeToTarget(position.Security, 0m);

			_open = false;
			LogInfo("Closed dispersion spread");
		}

		private decimal GetLatestPrice(Security security)
		{
			return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
		}

		#region Helper math / trading

		private decimal[] Returns(RollingWindow<decimal> win)
		{
			var arr = win.ToArray();
			var r = new decimal[arr.Length - 1];

			for (var i = 1; i < arr.Length; i++)
				r[i - 1] = (arr[i] - arr[i - 1]) / arr[i - 1];

			return r;
		}

		private decimal Corr(decimal[] x, decimal[] y)
		{
			var n = Math.Min(x.Length, y.Length);
			var meanX = x.Take(n).Average();
			var meanY = y.Take(n).Average();

			decimal num = 0, dx = 0, dy = 0;

			for (var i = 0; i < n; i++)
			{
				var a = x[i] - meanX;
				var b = y[i] - meanY;
				num += a * b;
				dx += a * a;
				dy += b * b;
			}

			return dx > 0 && dy > 0 ? num / (decimal)Math.Sqrt((double)(dx * dy)) : 0m;
		}

		private void TradeToTarget(Security s, decimal tgtQty)
		{
			var diff = tgtQty - PositionBy(s);
			var price = GetLatestPrice(s);

			if (price <= 0 || Math.Abs(diff) * price < MinTradeUsd)
				return;

			RegisterOrder(new Order
			{
				Security = s,
				Portfolio = Portfolio,
				Side = diff > 0 ? Sides.Buy : Sides.Sell,
				Volume = Math.Abs(diff),
				Type = OrderTypes.Market,
				Comment = "Dispersion"
			});
		}

		private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;

		#endregion

		#region RollingWindow

		private class RollingWindow<T>
		{
			private readonly Queue<T> _queue = new();
			private readonly int _size;

			public RollingWindow(int size)
			{
				_size = size;
			}

			public void Add(T value)
			{
				if (_queue.Count == _size)
					_queue.Dequeue();

				_queue.Enqueue(value);
			}

			public bool IsFull() => _queue.Count == _size;

			public T Last() => _queue.Last();

			public T[] ToArray() => _queue.ToArray();
		}

		#endregion
	}
}

