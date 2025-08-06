using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Drawing;

using StockSharp.Messages;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Indicators;
using StockSharp.BusinessEntities;
using StockSharp.Localization;
using StockSharp.Charting;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Double Supertrend Strategy
	/// </summary>
	public class DoubleSupertrendStrategy : Strategy
	{
		private bool _prevDirection1;
		private bool _prevDirection2;

		public DoubleSupertrendStrategy()
		{
			_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
				.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

			_atrPeriod1 = Param(nameof(ATRPeriod1), 10)
				.SetGreaterThanZero()
				.SetDisplay("ST1 ATR Period", "First Supertrend ATR period", "Supertrend");

			_factor1 = Param(nameof(Factor1), 3.0m)
				.SetDisplay("ST1 Factor", "First Supertrend factor", "Supertrend");

			_atrPeriod2 = Param(nameof(ATRPeriod2), 10)
				.SetGreaterThanZero()
				.SetDisplay("ST2 ATR Period", "Second Supertrend ATR period", "Supertrend");

			_factor2 = Param(nameof(Factor2), 5.0m)
				.SetDisplay("ST2 Factor", "Second Supertrend factor", "Supertrend");

			_direction = Param(nameof(Direction), "Long")
				.SetDisplay("Direction", "Trading direction (Long/Short)", "Strategy");

			_tpType = Param(nameof(TPType), "Supertrend")
				.SetDisplay("TP Type", "Take profit type (Supertrend/%)", "Take Profit");

			_tpPercent = Param(nameof(TPPercent), 1.5m)
				.SetDisplay("TP Percent", "Take profit percentage", "Take Profit");

			_slPercent = Param(nameof(SLPercent), 10m)
				.SetDisplay("Stop Loss %", "Stop loss percentage", "Stop Loss");
		}

		private readonly StrategyParam<DataType> _candleTypeParam;
		public DataType CandleType
		{
			get => _candleTypeParam.Value;
			set => _candleTypeParam.Value = value;
		}

		private readonly StrategyParam<int> _atrPeriod1;
		public int ATRPeriod1
		{
			get => _atrPeriod1.Value;
			set => _atrPeriod1.Value = value;
		}

		private readonly StrategyParam<decimal> _factor1;
		public decimal Factor1
		{
			get => _factor1.Value;
			set => _factor1.Value = value;
		}

		private readonly StrategyParam<int> _atrPeriod2;
		public int ATRPeriod2
		{
			get => _atrPeriod2.Value;
			set => _atrPeriod2.Value = value;
		}

		private readonly StrategyParam<decimal> _factor2;
		public decimal Factor2
		{
			get => _factor2.Value;
			set => _factor2.Value = value;
		}

		private readonly StrategyParam<string> _direction;
		public string Direction
		{
			get => _direction.Value;
			set => _direction.Value = value;
		}

		private readonly StrategyParam<string> _tpType;
		public string TPType
		{
			get => _tpType.Value;
			set => _tpType.Value = value;
		}

		private readonly StrategyParam<decimal> _tpPercent;
		public decimal TPPercent
		{
			get => _tpPercent.Value;
			set => _tpPercent.Value = value;
		}

		private readonly StrategyParam<decimal> _slPercent;
		public decimal SLPercent
		{
			get => _slPercent.Value;
			set => _slPercent.Value = value;
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
			=> new[] { (Security, CandleType) };

		/// <inheritdoc />
		protected override void OnReseted()
		{
			base.OnReseted();
			_prevDirection1 = false;
			_prevDirection2 = false;
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create Supertrend indicators
			var supertrend1 = new SuperTrend
			{
				Period = ATRPeriod1,
				Multiplier = Factor1
			};

			var supertrend2 = new SuperTrend
			{
				Period = ATRPeriod2,
				Multiplier = Factor2
			};

			// Subscribe to candles
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(supertrend1, supertrend2, OnProcess)
				.Start();

			// Configure chart
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, supertrend1, System.Drawing.Color.Green);
				DrawIndicator(area, supertrend2, System.Drawing.Color.Red);
				DrawOwnTrades(area);
			}

			// Start protection
			Unit? takeProfit = null;
			if (TPType == "%")
				takeProfit = new Unit(TPPercent, UnitTypes.Percent);
			
			var stopLoss = new Unit(SLPercent, UnitTypes.Percent);
			StartProtection(takeProfit, stopLoss);
		}

		private void OnProcess(ICandleMessage candle, 
			SuperTrendValue st1Value, SuperTrendValue st2Value)
		{
			// Only process finished candles
			if (candle.State != CandleStates.Finished)
				return;

			var inLong1 = st1Value.IsUp;
			var inLong2 = st2Value.IsUp;

			// Check for direction changes
			var exitLong = _prevDirection2 && !inLong2;
			var exitShort = !_prevDirection2 && inLong2;

			var isLongMode = Direction == "Long";
			var isShortMode = Direction == "Short";

			// Entry conditions
			var entryLong = inLong1;
			var entryShort = !inLong1;

			// Execute trades
			if (isLongMode)
			{
				if (entryLong && Position == 0)
				{
					RegisterOrder(this.BuyMarket(Volume));
				}
				else if (exitLong && Position > 0)
				{
					RegisterOrder(this.SellMarket(Position));
				}
			}
			else if (isShortMode)
			{
				if (entryShort && Position == 0)
				{
					RegisterOrder(this.SellMarket(Volume));
				}
				else if ((exitShort || inLong1) && Position < 0)
				{
					RegisterOrder(this.BuyMarket(Position.Abs()));
				}
			}

			// Update previous directions
			_prevDirection1 = inLong1;
			_prevDirection2 = inLong2;
		}
	}
}