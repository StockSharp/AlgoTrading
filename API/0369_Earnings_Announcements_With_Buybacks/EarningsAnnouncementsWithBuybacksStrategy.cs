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
	/// Strategy that buys stocks with active buyback programs before their earnings announcements
	/// and exits a few days after the report.
	/// </summary>
	public class EarningsAnnouncementsWithBuybacksStrategy : Strategy
	{
		private readonly StrategyParam<IEnumerable<Security>> _universe;
		private readonly StrategyParam<int> _daysBefore;
		private readonly StrategyParam<int> _daysAfter;
		private readonly StrategyParam<decimal> _capitalUsd;
		private readonly StrategyParam<decimal> _minUsd;
		private readonly StrategyParam<DataType> _candleType;
		
		private readonly Dictionary<Security, DateTimeOffset> _exit = new();
		private readonly Dictionary<Security, decimal> _latestPrices = new();
		private DateTime _lastProcessed = DateTime.MinValue;
		
		/// <summary>
		/// Securities universe to monitor.
		/// </summary>
		public IEnumerable<Security> Universe
		{
			get => _universe.Value;
			set => _universe.Value = value;
		}
		
		/// <summary>
		/// Number of days before earnings to enter.
		/// </summary>
		public int DaysBefore
		{
			get => _daysBefore.Value;
			set => _daysBefore.Value = value;
		}
		
		/// <summary>
		/// Number of days after earnings to exit.
		/// </summary>
		public int DaysAfter
		{
			get => _daysAfter.Value;
			set => _daysAfter.Value = value;
		}
		
		/// <summary>
		/// Capital allocated per trade in USD.
		/// </summary>
		public decimal CapitalPerTradeUsd
		{
			get => _capitalUsd.Value;
			set => _capitalUsd.Value = value;
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
		/// Candle type used for price data.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}
		
		/// <summary>
		/// Initializes a new instance of <see cref="EarningsAnnouncementsWithBuybacksStrategy"/>.
		/// </summary>
		public EarningsAnnouncementsWithBuybacksStrategy()
		{
			_universe = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>())
			.SetDisplay("Universe", "Securities to monitor", "General");
			
			_daysBefore = Param(nameof(DaysBefore), 5)
			.SetGreaterThanZero()
			.SetDisplay("Days Before", "Days before earnings to enter", "Trading");
			
			_daysAfter = Param(nameof(DaysAfter), 1)
			.SetGreaterThanZero()
			.SetDisplay("Days After", "Days after earnings to exit", "Trading");
			
			_capitalUsd = Param(nameof(CapitalPerTradeUsd), 5000m)
			.SetGreaterThanZero()
			.SetDisplay("Capital Per Trade USD", "Capital allocated per trade", "Risk Management");
			
			_minUsd = Param(nameof(MinTradeUsd), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Minimum Trade USD", "Minimum trade value", "Risk Management");
			
			_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
		}
		
		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return Universe.Select(s => (s, CandleType));
		}
		
		/// <inheritdoc />
		
		protected override void OnReseted()
		{
			base.OnReseted();

			_exit.Clear();
			_latestPrices.Clear();
			_lastProcessed = default;
		}

		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);
			
			if (Universe == null || !Universe.Any())
			throw new InvalidOperationException("Universe is empty.");
			
			foreach (var (sec, dt) in GetWorkingSecurities())
			{
				SubscribeCandles(dt, true, sec)
				.Bind(c => ProcessCandle(c, sec))
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
			
			var d = candle.OpenTime.Date;
			if (d == _lastProcessed)
			return;
			
			_lastProcessed = d;
			DailyScan(d);
		}
		
		private void DailyScan(DateTime today)
		{
			foreach (var stock in Universe)
			{
				if (!TryGetNextEarningsDate(stock, out var earnDate))
				continue;
				
				var diff = (earnDate.Date - today).TotalDays;
				if (diff == DaysBefore && !_exit.ContainsKey(stock) && TryHasActiveBuyback(stock))
				{
					var price = GetLatestPrice(stock);
					if (price <= 0)
					continue;
					
					var qty = CapitalPerTradeUsd / price;
					if (qty * price >= MinTradeUsd)
					{
						Place(stock, qty, Sides.Buy, "Enter");
						_exit[stock] = earnDate.Date.AddDays(DaysAfter);
					}
				}
			}
			
			foreach (var kv in _exit.ToList())
			{
				if (today >= kv.Value)
				{
					var pos = PositionBy(kv.Key);
					if (pos > 0)
					Place(kv.Key, pos, Sides.Sell, "Exit");
					
					_exit.Remove(kv.Key);
				}
			}
		}
		
		private decimal PositionBy(Security s) => GetPositionValue(s, Portfolio) ?? 0;
		
		private decimal GetLatestPrice(Security security)
		{
			return _latestPrices.TryGetValue(security, out var price) ? price : 0m;
		}
		
		private void Place(Security s, decimal qty, Sides side, string tag)
		{
			RegisterOrder(new Order
			{
				Security = s,
				Portfolio = Portfolio,
				Side = side,
				Volume = qty,
				Type = OrderTypes.Market,
				Comment = $"EarnBuyback-{tag}"
			});
		}
		
		private bool TryGetNextEarningsDate(Security s, out DateTimeOffset dt)
		{
			dt = DateTimeOffset.MinValue;
			return false;
		}
		
		private bool TryHasActiveBuyback(Security s) => false;
	}
}
