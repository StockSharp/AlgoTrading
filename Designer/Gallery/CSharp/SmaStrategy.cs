namespace StockSharp.Designer
{
	using System;
	using System.Linq;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.Messages;
	using StockSharp.Algo;
	using StockSharp.Algo.Strategies;
	using StockSharp.Algo.Indicators;
	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Charting;

	/// <summary>
	/// Sample strategy demonstrating the work with SMA indicators.
	/// 
	/// See more examples https://github.com/StockSharp/StockSharp/tree/master/Algo/Strategies/Quoting
	/// </summary>
	public class SmaStrategy : Strategy
	{
		private readonly List<MyTrade> _myTrades = new List<MyTrade>();

		private SimpleMovingAverage _longSma;
		private SimpleMovingAverage _shortSma;

		private IChartCandleElement _chartCandlesElem;
		private IChartTradeElement _chartTradesElem;
		private IChartIndicatorElement _chartLongElem;
		private IChartIndicatorElement _chartShortElem;

		private bool? _isShortLessThenLong;
		private IChart _chart;

		public SmaStrategy()
		{
			_candleTypeParam = this.Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(1)));
			_long = this.Param(nameof(Long), 80);
			_short = this.Param(nameof(Short), 20);
		}

		private readonly StrategyParam<DataType> _candleTypeParam;

		public DataType CandleType
		{
			get => _candleTypeParam.Value;
			set => _candleTypeParam.Value = value;
		}

		private readonly StrategyParam<int> _long;

		public int Long
		{
			get => _long.Value;
			set => _long.Value = value;
		}

		private readonly StrategyParam<int> _short;

		public int Short
		{
			get => _short.Value;
			set => _short.Value = value;
		}

		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			this.AddInfoLog(nameof(OnStarted));

			_longSma = new SimpleMovingAverage { Length = Long };
			_shortSma = new SimpleMovingAverage { Length = Short };

			// !!! DO NOT FORGET add it in case use IsFormed property (see code below)
			Indicators.Add(_longSma);
			Indicators.Add(_shortSma);

			// reset prev state
			_myTrades.Clear();
			_isShortLessThenLong = null;

			var subscription = new Subscription(CandleType, Security)
			{
				MarketData =
				{
					// working with only formed candles
					IsFinishedOnly = true,
				}
			};

			subscription
				.WhenCandleReceived(this)
				.Do(ProcessCandle)
				.Apply(this);

			this
				.WhenNewMyTrade()
				.Do(_myTrades.Add)
				.Apply(this);

			_chart = this.GetChart();

			// chart can be NULL in case hosting strategy in custom app like Runner or Shell
			if (_chart != null)
			{
				var area = _chart.AddArea();

				_chartCandlesElem = area.AddCandles();
				_chartTradesElem = area.AddTrades();
				_chartShortElem = area.AddIndicator(_shortSma);
				_chartLongElem = area.AddIndicator(_longSma);

				// you can apply custom color palette here
			}

			Subscribe(subscription);
		}

		private void ProcessCandle(ICandleMessage candle)
		{
			// strategy are stopping
			if (ProcessState == ProcessStates.Stopping)
			{
				CancelActiveOrders();
				return;
			}

			this.AddInfoLog(LocalizedStrings.SmaNewCandleLog, candle.OpenTime, candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice, candle.TotalVolume, candle.SecurityId);

			// process new candle
			var longValue = _longSma.Process(candle);
			var shortValue = _shortSma.Process(candle);

			// some of indicators added in OnStarted not yet fully formed
			// or user turned off allow trading
			if (this.IsFormedAndOnlineAndAllowTrading())
			{
				// in case we subscribed on non finished only candles
				if (candle.State == CandleStates.Finished)
				{
					// calc new values for short and long
					var isShortLessThenLong = shortValue.GetValue<decimal>() < longValue.GetValue<decimal>();

					if (_isShortLessThenLong == null)
					{
						_isShortLessThenLong = isShortLessThenLong;
					}
					else if (_isShortLessThenLong != isShortLessThenLong)
					{
						// crossing happened

						// if short less than long, the sale, otherwise buy
						var direction = isShortLessThenLong ? Sides.Sell : Sides.Buy;

						// calc size for open position or revert
						var volume = Position == 0 ? Volume : Position.Abs().Min(Volume) * 2;

						var priceStep = this.GetSecurity().PriceStep;

						// calc order price as a close price + offset
						var price = candle.ClosePrice + ((direction == Sides.Buy ? priceStep : -priceStep) ?? 1);

						RegisterOrder(this.CreateOrder(direction, price, volume));

						// store current values for short and long
						_isShortLessThenLong = isShortLessThenLong;
					}
				}
			}

			var trade = _myTrades.FirstOrDefault();
			_myTrades.Clear();

			if (_chart == null)
				return;

			var data = _chart.CreateData();

			data
				.Group(candle.OpenTime)
					.Add(_chartCandlesElem, candle)
					.Add(_chartShortElem, shortValue)
					.Add(_chartLongElem, longValue)
					.Add(_chartTradesElem, trade)
					;

			_chart.Draw(data);
		}
	}
}