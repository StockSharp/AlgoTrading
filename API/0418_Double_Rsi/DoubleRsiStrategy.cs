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
	/// Double RSI Strategy with multi-timeframe analysis
	/// </summary>
	public class DoubleRsiStrategy : Strategy
	{
		private decimal _mtfRsiValue;

		public DoubleRsiStrategy()
		{
			_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

			_rsiLength = Param(nameof(RSILength), 14)
				.SetGreaterThanZero()
				.SetDisplay("RSI Length", "RSI period", "RSI");

			_mtfTimeframe = Param(nameof(MTFTimeframe), TimeSpan.FromMinutes(15).TimeFrame())
				.SetDisplay("MTF Timeframe", "Multi-timeframe for second RSI", "Multi Timeframe RSI");

			_useTP = Param(nameof(UseTP), false)
				.SetDisplay("Use Take Profit", "Enable take profit", "Take Profit");

			_tpPercent = Param(nameof(TPPercent), 1.2m)
				.SetDisplay("Take Profit %", "Take profit percentage", "Take Profit");
		}

		private readonly StrategyParam<DataType> _candleTypeParam;
		public DataType CandleType
		{
			get => _candleTypeParam.Value;
			set => _candleTypeParam.Value = value;
		}

		private readonly StrategyParam<int> _rsiLength;
		public int RSILength
		{
			get => _rsiLength.Value;
			set => _rsiLength.Value = value;
		}

		private readonly StrategyParam<DataType> _mtfTimeframe;
		public DataType MTFTimeframe
		{
			get => _mtfTimeframe.Value;
			set => _mtfTimeframe.Value = value;
		}

		private readonly StrategyParam<bool> _useTP;
		public bool UseTP
		{
			get => _useTP.Value;
			set => _useTP.Value = value;
		}

		private readonly StrategyParam<decimal> _tpPercent;
		public decimal TPPercent
		{
			get => _tpPercent.Value;
			set => _tpPercent.Value = value;
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
			=> new[] { (Security, CandleType), (Security, MTFTimeframe) };

		/// <inheritdoc />
		protected override void OnReseted()
		{
			base.OnReseted();
			_mtfRsiValue = 0;
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create RSI indicators
			var rsi = new RelativeStrengthIndex { Length = RSILength };
			var mtfRsi = new RelativeStrengthIndex { Length = RSILength };

			// Subscribe to regular timeframe
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(rsi, OnProcessMainTimeframe)
				.Start();

			// Subscribe to multi-timeframe
			var mtfSubscription = SubscribeCandles(MTFTimeframe);
			mtfSubscription
				.Bind(mtfRsi, OnProcessMTF)
				.Start();

			// Configure chart
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawOwnTrades(area);
			}

			// Start protection if enabled
			if (UseTP)
			{
				var takeValue = new Unit(TPPercent, UnitTypes.Percent);
				StartProtection(takeValue, new());
			}
		}

		private void OnProcessMTF(ICandleMessage candle, decimal mtfRsiValue)
		{
			if (candle.State == CandleStates.Finished)
				_mtfRsiValue = mtfRsiValue;
		}

		private void OnProcessMainTimeframe(ICandleMessage candle, decimal rsiValue)
		{
			// Only process finished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Buy signal: RSI crosses above 30 and MTF RSI < 35
			var buy = rsiValue > 30 && rsiValue < 35 && _mtfRsiValue < 35;
			
			// Sell signal: RSI crosses below 70 and MTF RSI > 65
			var sell = rsiValue < 70 && rsiValue > 65 && _mtfRsiValue > 65;

			// Execute trades
			if (buy && Position <= 0)
			{
				if (Position < 0)
					RegisterOrder(this.BuyMarket(Position.Abs()));
				RegisterOrder(this.BuyMarket(Volume));
			}
			else if (sell && Position >= 0)
			{
				if (Position > 0)
					RegisterOrder(this.SellMarket(Position));
				RegisterOrder(this.SellMarket(Volume));
			}
		}
	}
}