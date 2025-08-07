// WTIBrentSpreadStrategy.cs
// -----------------------------------------------------------------------------
// WTI‑Brent spread vs 20‑day SMA. Daily WTI candle triggers evaluation.
// -----------------------------------------------------------------------------
// Date: 2 Aug 2025
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Trades the spread between WTI and Brent based on a moving average.
	/// </summary>
	public class WTIBrentSpreadStrategy : Strategy
	{
		private readonly StrategyParam<Security> _brent;
		private readonly StrategyParam<int> _ma;
		private readonly StrategyParam<decimal> _minUsd;
		private readonly StrategyParam<DataType> _candleType;
		private readonly Queue<decimal> _spr = new();
		private readonly Dictionary<Security, decimal> _latestPrices = new();

		/// <summary>
		/// WTI security.
		/// </summary>
		public Security WTI
		{
			get => Security;
			set => Security = value;
		}

		/// <summary>
		/// Brent security.
		/// </summary>
		public Security Brent
		{
			get => _brent.Value;
			set => _brent.Value = value;
		}

		/// <summary>
		/// Moving average period for spread.
		/// </summary>
		public int MaPeriod
		{
			get => _ma.Value;
			set => _ma.Value = value;
		}

		/// <summary>
		/// Minimum trade value in USD.
		/// </summary>
		public decimal MinTradeUsd
		{
			get => _minUsd.Value;
			set => _minUsd.Value = value;
		}

		/// <summary>
		/// The type of candles to use for strategy calculation.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		public WTIBrentSpreadStrategy()
		{
			_brent = Param<Security>(nameof(Brent), null)
				.SetDisplay("Brent", "Brent security", "Universe");

			_ma = Param(nameof(MaPeriod), 20)
				.SetDisplay("MA Period", "Moving average period", "Parameters");

			_minUsd = Param(nameof(MinTradeUsd), 200m)
				.SetDisplay("Min Trade USD", "Minimum notional value for orders", "Risk Management");

			_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "Data");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
		{
			if (WTI == null || Brent == null)
				throw new InvalidOperationException("WTI and Brent must be set.");

			yield return (WTI, CandleType);
			yield return (Brent, CandleType);
		}

		/// <inheritdoc />
		protected override void OnReseted()
		{
			base.OnReseted();

			_latestPrices.Clear();
			_spr.Clear();
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset t)
		{
			if (WTI == null || Brent == null)
				throw new InvalidOperationException("WTI and Brent must be set.");

			base.OnStarted(t);
			SubscribeCandles(CandleType, true, WTI).Bind(c => ProcessCandle(c, WTI)).Start();
			SubscribeCandles(CandleType, true, Brent).Bind(c => ProcessCandle(c, Brent)).Start();
		}

		private void ProcessCandle(ICandleMessage candle, Security security)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Store the latest closing price for this security
			_latestPrices[security] = candle.ClosePrice;

			// Only trigger evaluation when WTI candle comes in
			if (security == WTI)
				OnDaily();
		}

		private void OnDaily()
		{
			var p1 = GetLatestPrice(WTI);
			var p2 = GetLatestPrice(Brent);
			if (p1 == 0 || p2 == 0)
				return;

			var spr = p1 - p2;
			if (_spr.Count == MaPeriod)
				_spr.Dequeue();
			_spr.Enqueue(spr);
			if (_spr.Count < MaPeriod)
				return;

			var sma = _spr.Average();
			var portfolioValue = Portfolio.CurrentValue ?? 0m;
			var notional = portfolioValue / 2;

			if (Math.Abs(spr - sma) < 0.01m)
			{ 
				Move(WTI, 0); 
				Move(Brent, 0); 
				return; 
			}

			if (spr > sma)
			{ 
				Move(WTI, -notional / p1); 
				Move(Brent, notional / p2); 
			}
			else
			{ 
				Move(WTI, notional / p1); 
				Move(Brent, -notional / p2); 
			}
		}

		private decimal GetLatestPrice(Security security)
		{
			return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
		}

		private void Move(Security s, decimal tgt)
		{
			var diff = tgt - Pos(s);
			var price = GetLatestPrice(s);
			if (price <= 0 || Math.Abs(diff) * price < MinTradeUsd)
				return;
			RegisterOrder(new Order { Security = s, Portfolio = Portfolio, Side = diff > 0 ? Sides.Buy : Sides.Sell, Volume = Math.Abs(diff), Type = OrderTypes.Market, Comment = "Spread" });
		}

		private decimal Pos(Security s) => GetPositionValue(s, Portfolio) ?? 0;
	}
}