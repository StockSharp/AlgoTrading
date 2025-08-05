using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Country value factor strategy based on CAPE ratio.
	/// </summary>
	public class CountryValueFactorStrategy : Strategy
	{
		private readonly StrategyParam<IEnumerable<Security>> _universe;
		private readonly StrategyParam<decimal> _minTradeUsd;

		private readonly DataType _timeFrame = TimeSpan.FromDays(1).TimeFrame();

		/// <summary>
		/// Securities to trade.
		/// </summary>
		public IEnumerable<Security> Universe
		{
			get => _universe.Value;
			set => _universe.Value = value;
		}

		/// <summary>
		/// Minimum trade size in USD.
		/// </summary>
		public decimal MinTradeUsd
		{
			get => _minTradeUsd.Value;
			set => _minTradeUsd.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of <see cref="CountryValueFactorStrategy"/>.
		/// </summary>
		public CountryValueFactorStrategy()
		{
			_universe = Param<IEnumerable<Security>>(nameof(Universe), Array.Empty<Security>())
				.SetDisplay("Universe", "Trading securities collection", "General");

			_minTradeUsd = Param(nameof(MinTradeUsd), 200m)
				.SetDisplay("Min Trade USD", "Minimal trade size in USD", "General");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return Universe?.Select(s => (s, _timeFrame)) ?? Array.Empty<(Security, DataType)>();
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			if (Universe == null || !Universe.Any())
				throw new InvalidOperationException("Universe is empty.");

			var trigger = Universe.First();

			SubscribeCandles(_timeFrame, true, trigger)
				.Bind(c => OnDay(c.OpenTime.Date))
				.Start();
		}

		private void OnDay(DateTime date)
		{
			// TODO: implement factor logic. Placeholder keeps portfolio flat.
		}
	}
}

