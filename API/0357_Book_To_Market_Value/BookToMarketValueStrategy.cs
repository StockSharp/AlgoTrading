// BookToMarketValueStrategy.cs
// -----------------------------------------------------------------------------
// Book‑to‑Market factor (monthly)
// Rebalance frequency and data feeds stubbed; candle-trigger only.
// -----------------------------------------------------------------------------
// Date: 2 Aug 2025
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Book-to-market value strategy.
	/// Placeholder implementation demonstrating parameter setup and candle subscription.
	/// </summary>
	public class BookToMarketValueStrategy : Strategy
	{
		// Parameters
		private readonly StrategyParam<IEnumerable<Security>> _univ;
		private readonly StrategyParam<decimal> _min;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// Securities universe to analyze.
		/// </summary>
		public IEnumerable<Security> Universe
		{
			get => _univ.Value;
			set => _univ.Value = value;
		}

		/// <summary>
		/// Minimum trade value in USD.
		/// </summary>
		public decimal MinTradeUsd
		{
			get => _min.Value;
			set => _min.Value = value;
		}

		/// <summary>
		/// The type of candles to use for strategy calculation.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BookToMarketValueStrategy"/> class.
		/// </summary>
		public BookToMarketValueStrategy()
		{
			_univ = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>())
				.SetDisplay("Universe", "Securities to process", "General");

			_min = Param(nameof(MinTradeUsd), 200m)
				.SetGreaterThanZero()
				.SetDisplay("Min Trade USD", "Minimum trade value in USD", "General");

			_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return Universe.Select(sec => (sec, CandleType));
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			if (Universe == null || !Universe.Any())
				throw new InvalidOperationException("Universe is empty.");

			var trigger = Universe.First();

			SubscribeCandles(CandleType, true, trigger)
				.Bind(candle => OnDay(candle.OpenTime.Date))
				.Start();
		}

		private void OnDay(DateTime date)
		{
			// TODO: implement factor logic. Placeholder keeps portfolio flat.
		}
	}
}