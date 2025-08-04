// CrudeOilPredictsEquityStrategy.cs (daily candles version)
// If last-month oil return > 0, invest in equity ETF, else stay in cash ETF.
// Date: 2 August 2025

using System;
using System.Collections.Generic;
using System.Linq;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	public class CrudeOilPredictsEquityStrategy : Strategy
	{
		private readonly StrategyParam<Security> _equity;
		private readonly StrategyParam<Security> _oil;
		private readonly StrategyParam<Security> _cash;
		private readonly StrategyParam<DataType> _tf;
		private readonly StrategyParam<int> _lookback;

		public Security Equity { get => _equity.Value; set => _equity.Value = value; }
		public Security Oil { get => _oil.Value; set => _oil.Value = value; }
		public Security CashEtf { get => _cash.Value; set => _cash.Value = value; }
		public DataType CandleType => _tf.Value;
		public int Lookback => _lookback.Value;

		private readonly Dictionary<Security, RollingWindow<decimal>> _wins = new();
		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private DateTime _lastDay = DateTime.MinValue;

		public CrudeOilPredictsEquityStrategy()
		{
			_equity = Param<Security>(nameof(Equity), null);
			_oil = Param<Security>(nameof(Oil), null);
			_cash = Param<Security>(nameof(CashEtf), null);
			_tf = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame());
			_lookback = Param(nameof(Lookback), 22); // 1 month
		}

		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			if (Equity == null || Oil == null || CashEtf == null)
				throw new InvalidOperationException("Set securities");
			return new[] { (Equity, CandleType), (Oil, CandleType), (CashEtf, CandleType) };
		}

		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);
			foreach (var (s, dt) in GetWorkingSecurities())
			{
				_wins[s] = new RollingWindow<decimal>(Lookback + 1);
				SubscribeCandles(dt, true, s)
					.Bind(c => ProcessCandle(c, s))
					.Start();
			}
		}

		private void ProcessCandle(ICandleMessage candle, Security security)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Store the latest closing price for this security
			_latestPrices[security] = candle.ClosePrice;

			_wins[security].Add(candle.ClosePrice);
			var d = candle.OpenTime.Date;
			if (d == _lastDay)
				return;
			_lastDay = d;
			if (d.Day == 1)
				Rebalance();
		}

		private void Rebalance()
		{
			if (!_wins[Oil].IsFull())
				return;
			var oilRet = (_wins[Oil].Last() - _wins[Oil][0]) / _wins[Oil][0];
			if (oilRet > 0)
				MoveTo(Equity);
			else
				MoveTo(CashEtf);
		}

		private void MoveTo(Security target)
		{
			foreach (var position in Positions)
				if (position.Security != target)
					Move(position.Security, 0);

			var portfolioValue = Portfolio.CurrentValue ?? 0m;
			var price = GetLatestPrice(target);
			if (price > 0)
				Move(target, portfolioValue / price);
		}

		private decimal GetLatestPrice(Security security)
		{
			return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
		}

		private void Move(Security s, decimal tgt)
		{
			var diff = tgt - PositionBy(s);
			var price = GetLatestPrice(s);
			if (price <= 0 || Math.Abs(diff) * price < 100)
				return;
			RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "OilEq" });
		}
		private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;

		#region RollingWindow
		private class RollingWindow<T>
		{
			private readonly Queue<T> _q = new();
			private readonly int _n;
			public RollingWindow(int n) { _n = n; }
			public void Add(T v) { if (_q.Count == _n) _q.Dequeue(); _q.Enqueue(v); }
			public bool IsFull() => _q.Count == _n;
			public T Last() => _q.Last();
			public T this[int i] => _q.ElementAt(i);
		}
		#endregion

	}
}