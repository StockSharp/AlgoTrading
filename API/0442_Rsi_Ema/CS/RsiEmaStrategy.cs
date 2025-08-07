using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// RSI + EMA Strategy - uses RSI oversold/overbought levels with dual EMA trend filter
	/// </summary>
	public class RsiEmaStrategy : Strategy
	{
		private readonly StrategyParam<DataType> _candleType;
		private readonly StrategyParam<int> _rsiLength;
		private readonly StrategyParam<int> _rsiOverbought;
		private readonly StrategyParam<int> _rsiOversold;
		private readonly StrategyParam<string> _ma1Type;
		private readonly StrategyParam<int> _ma1Length;
		private readonly StrategyParam<string> _ma2Type;
		private readonly StrategyParam<int> _ma2Length;

		private RelativeStrengthIndex _rsi;
		private IIndicator _ma1;
		private IIndicator _ma2;

		/// <summary>
		/// Candle type for strategy calculation.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// RSI calculation length.
		/// </summary>
		public int RsiLength
		{
			get => _rsiLength.Value;
			set => _rsiLength.Value = value;
		}

		/// <summary>
		/// RSI overbought level.
		/// </summary>
		public int RsiOverbought
		{
			get => _rsiOverbought.Value;
			set => _rsiOverbought.Value = value;
		}

		/// <summary>
		/// RSI oversold level.
		/// </summary>
		public int RsiOversold
		{
			get => _rsiOversold.Value;
			set => _rsiOversold.Value = value;
		}

		/// <summary>
		/// First moving average type.
		/// </summary>
		public string Ma1Type
		{
			get => _ma1Type.Value;
			set => _ma1Type.Value = value;
		}

		/// <summary>
		/// First moving average length.
		/// </summary>
		public int Ma1Length
		{
			get => _ma1Length.Value;
			set => _ma1Length.Value = value;
		}

		/// <summary>
		/// Second moving average type.
		/// </summary>
		public string Ma2Type
		{
			get => _ma2Type.Value;
			set => _ma2Type.Value = value;
		}

		/// <summary>
		/// Second moving average length.
		/// </summary>
		public int Ma2Length
		{
			get => _ma2Length.Value;
			set => _ma2Length.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public RsiEmaStrategy()
		{
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");

			_rsiLength = Param(nameof(RsiLength), 14)
				.SetGreaterThanZero()
				.SetDisplay("RSI Length", "RSI calculation length", "RSI")
				.SetCanOptimize(true)
				.SetOptimize(5, 30, 2);

			_rsiOverbought = Param(nameof(RsiOverbought), 70)
				.SetRange(50, 95)
				.SetDisplay("RSI Overbought", "RSI overbought level", "RSI")
				.SetCanOptimize(true)
				.SetOptimize(65, 85, 5);

			_rsiOversold = Param(nameof(RsiOversold), 30)
				.SetRange(5, 50)
				.SetDisplay("RSI Oversold", "RSI oversold level", "RSI")
				.SetCanOptimize(true)
				.SetOptimize(15, 35, 5);

			_ma1Type = Param(nameof(Ma1Type), "EMA")
				.SetDisplay("MA1 Type", "First moving average type", "Moving Averages");

			_ma1Length = Param(nameof(Ma1Length), 150)
				.SetGreaterThanZero()
				.SetDisplay("MA1 Length", "First moving average length", "Moving Averages")
				.SetCanOptimize(true)
				.SetOptimize(100, 200, 25);

			_ma2Type = Param(nameof(Ma2Type), "EMA")
				.SetDisplay("MA2 Type", "Second moving average type", "Moving Averages");

			_ma2Length = Param(nameof(Ma2Length), 600)
				.SetGreaterThanZero()
				.SetDisplay("MA2 Length", "Second moving average length", "Moving Averages")
				.SetCanOptimize(true)
				.SetOptimize(400, 800, 100);
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return new[] { (Security, CandleType) };
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Initialize indicators
			_rsi = new RelativeStrengthIndex { Length = RsiLength };
			
			_ma1 = Ma1Type == "SMA" 
				? (IIndicator)new SimpleMovingAverage { Length = Ma1Length }
				: new ExponentialMovingAverage { Length = Ma1Length };
				
			_ma2 = Ma2Type == "SMA" 
				? (IIndicator)new SimpleMovingAverage { Length = Ma2Length }
				: new ExponentialMovingAverage { Length = Ma2Length };

			// Create subscription for candles
			var subscription = SubscribeCandles(CandleType);
			subscription
				.Bind(_rsi, _ma1, _ma2, ProcessCandle)
				.Start();

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _ma1);
				DrawIndicator(area, _ma2);
				DrawOwnTrades(area);
			}
		}

		private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal ma1Value, decimal ma2Value)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Wait for indicators to form
			if (!_rsi.IsFormed || !_ma1.IsFormed || !_ma2.IsFormed)
				return;

			var currentPrice = candle.ClosePrice;

			// Long entry: RSI < oversold and MA1 > MA2 (trend filter)
			if (rsiValue < RsiOversold && ma1Value > ma2Value && Position == 0)
			{
				RegisterOrder(CreateOrder(Sides.Buy, currentPrice, Volume));
			}

			// Short entry: RSI > overbought and MA1 > MA2 (trend filter)
			if (rsiValue > RsiOverbought && ma1Value > ma2Value && Position == 0)
			{
				RegisterOrder(CreateOrder(Sides.Sell, currentPrice, Volume));
			}

			// Exit long: RSI > overbought
			if (Position > 0 && rsiValue > RsiOverbought)
			{
				RegisterOrder(CreateOrder(Sides.Sell, currentPrice, Math.Abs(Position)));
			}

			// Exit short: RSI < oversold
			if (Position < 0 && rsiValue < RsiOversold)
			{
				RegisterOrder(CreateOrder(Sides.Buy, currentPrice, Math.Abs(Position)));
			}
		}
	}
}