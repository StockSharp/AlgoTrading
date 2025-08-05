// JanuaryBarometerStrategy.cs
// -----------------------------------------------------------------------------
// If January monthly return is positive, stay long equity index ETF for rest
// of year; otherwise move to cash proxy.
// Uses daily candles to detect January close.
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
	/// January barometer strategy rotating between equity and cash ETFs
	/// based on January performance.
	/// </summary>
	public class JanuaryBarometerStrategy : Strategy
	{
		#region Params
		private readonly StrategyParam<Security> _equity;
		private readonly StrategyParam<Security> _cash;
		private readonly StrategyParam<decimal> _minUsd;
		private readonly StrategyParam<DataType> _candleType;
		
		/// <summary>
		/// Equity ETF to hold when January is bullish.
		/// </summary>
		public Security EquityETF { get => _equity.Value; set => _equity.Value = value; }
		
		/// <summary>
		/// Cash proxy ETF.
		/// </summary>
		public Security CashETF { get => _cash.Value; set => _cash.Value = value; }
		
		/// <summary>
		/// Minimum trade value in USD.
		/// </summary>
		public decimal MinTradeUsd { get => _minUsd.Value; set => _minUsd.Value = value; }

		/// <summary>
		/// The type of candles to use for strategy calculation.
		/// </summary>
		public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
		#endregion

		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private decimal _janOpenPrice = 0m;

		public JanuaryBarometerStrategy()
		{
			_equity = Param<Security>(nameof(EquityETF), null)
				.SetDisplay("Equity ETF", "Risk asset", "General");
			
			_cash = Param<Security>(nameof(CashETF), null)
				.SetDisplay("Cash ETF", "Safe asset", "General");
			
			_minUsd = Param(nameof(MinTradeUsd), 200m)
				.SetGreaterThanZero()
				.SetDisplay("Min trade USD", "Minimum order value", "Risk");

			_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
			}

		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			if (EquityETF == null || CashETF == null)
				throw new InvalidOperationException("Both equity and cash ETFs must be set.");
			return new[] { (EquityETF, CandleType) };
		}
		
		protected override void OnReseted()
		{
			base.OnReseted();

			_latestPrices.Clear();
			_janOpenPrice = default;
		}

		protected override void OnStarted(DateTimeOffset t)
		{
			if (EquityETF == null || CashETF == null)
				throw new InvalidOperationException("Both equity and cash ETFs must be set.");

			base.OnStarted(t);

			SubscribeCandles(CandleType, true, EquityETF)
				.Bind(c => ProcessCandle(c, EquityETF))
					.Start();
		}

		private void ProcessCandle(ICandleMessage candle, Security security)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Store the latest closing price for this security
			_latestPrices[security] = candle.ClosePrice;

			OnDaily(candle);
		}

		private void OnDaily(ICandleMessage c)
		{
			var d = c.OpenTime.Date;
			// capture open price on first trading day of January
			if (d.Month == 1 && d.Day == 1)
				_janOpenPrice = c.OpenPrice;

			// detect January close (31 Jan, or last trading day of Jan)
			if (d.Month == 1 && (d.Day == 31 || c.State == CandleStates.Finished && c.CloseTime.Date.Month == 2))
			{
				if (_janOpenPrice == 0m)
					return;
				var janRet = (c.ClosePrice - _janOpenPrice) / _janOpenPrice;
				Rebalance(janRet > 0);
			}
		}

		private void Rebalance(bool bullish)
		{
			if (bullish)
			{
				Move(EquityETF, 1m);
				Move(CashETF, 0m);
			}
			else
			{
				Move(EquityETF, 0m);
				Move(CashETF, 1m);
			}
		}

		private decimal GetLatestPrice(Security security)
		{
			return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
		}

		private void Move(Security s, decimal weight)
		{
			var portfolioValue = Portfolio.CurrentValue ?? 0m;
			var price = GetLatestPrice(s);
			if (price <= 0)
				return;
				
			var tgt = weight * portfolioValue / price;
			var diff = tgt - PositionBy(s);
			if (Math.Abs(diff) * price < MinTradeUsd)
				return;

			RegisterOrder(new Order
			{
				Security = s,
				Portfolio = Portfolio,
				Side = diff > 0 ? Sides.Buy : Sides.Sell,
				Volume = Math.Abs(diff),
				Type = OrderTypes.Market,
				Comment = "JanBar"
			});
		}

		private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;
	}
}
