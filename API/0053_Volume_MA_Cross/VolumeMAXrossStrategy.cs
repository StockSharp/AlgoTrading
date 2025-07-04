using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using System;
using System.Collections.Generic;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Volume MA Cross strategy
	/// Long entry: Fast volume MA crosses above slow volume MA
	/// Short entry: Fast volume MA crosses below slow volume MA
	/// Exit: Reverse crossover
	/// </summary>
	public class VolumeMAXrossStrategy : Strategy
	{
		private readonly StrategyParam<int> _fastVolumeMALength;
		private readonly StrategyParam<int> _slowVolumeMALength;
		private readonly StrategyParam<DataType> _candleType;
		
		private decimal _previousFastVolumeMA;
		private decimal _previousSlowVolumeMA;
		private bool _isFirstValue;
		private SimpleMovingAverage _fastVolumeMA;
		private SimpleMovingAverage _slowVolumeMA;

		/// <summary>
		/// Fast Volume MA Length
		/// </summary>
		public int FastVolumeMALength
		{
			get => _fastVolumeMALength.Value;
			set => _fastVolumeMALength.Value = value;
		}

		/// <summary>
		/// Slow Volume MA Length
		/// </summary>
		public int SlowVolumeMALength
		{
			get => _slowVolumeMALength.Value;
			set => _slowVolumeMALength.Value = value;
		}

		/// <summary>
		/// Candle type for strategy calculation
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initialize <see cref="VolumeMAXrossStrategy"/>.
		/// </summary>
		public VolumeMAXrossStrategy()
		{
			_fastVolumeMALength = Param(nameof(FastVolumeMALength), 10)
				.SetGreaterThanZero()
				.SetDisplay("Fast Volume MA Length", "Period for Fast Volume Moving Average", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(5, 20, 5);

			_slowVolumeMALength = Param(nameof(SlowVolumeMALength), 50)
				.SetGreaterThanZero()
				.SetDisplay("Slow Volume MA Length", "Period for Slow Volume Moving Average", "Strategy Parameters")
				.SetCanOptimize(true)
				.SetOptimize(30, 100, 10);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles for strategy calculation", "Strategy Parameters");
				
			_previousFastVolumeMA = 0;
			_previousSlowVolumeMA = 0;
			_isFirstValue = true;
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return [(Security, CandleType)];
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			// Create indicators
			_fastVolumeMA = new SimpleMovingAverage { Length = FastVolumeMALength };
			_slowVolumeMA = new SimpleMovingAverage { Length = SlowVolumeMALength };
			var priceMA = new SimpleMovingAverage { Length = FastVolumeMALength }; // Use same period as fast Volume MA

			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Regular price MA binding for chart visualization
			subscription
				.Bind(priceMA, ProcessCandle)
				.Start();

			// Configure protection
			StartProtection(
				takeProfit: new Unit(3, UnitTypes.Percent),
				stopLoss: new Unit(2, UnitTypes.Percent)
			);

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, priceMA);
				DrawOwnTrades(area);
			}
		}
		
		private void ProcessCandle(ICandleMessage candle, decimal priceMAValue)
		{
			if (candle.State != CandleStates.Finished)
				return;

			// Process volume through MAs
			var fastMAValue = _fastVolumeMA.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();
			var slowMAValue = _slowVolumeMA.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();

			// Process the volume MAs

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// Skip the first values to initialize previous values
			if (_isFirstValue)
			{
				_previousFastVolumeMA = fastMAValue;
				_previousSlowVolumeMA = slowMAValue;
				_isFirstValue = false;
				return;
			}
			
			// Check for crossovers
			var crossAbove = _previousFastVolumeMA <= _previousSlowVolumeMA && fastMAValue > slowMAValue;
			var crossBelow = _previousFastVolumeMA >= _previousSlowVolumeMA && fastMAValue < slowMAValue;
			
			// Log current values
			LogInfo($"Candle Close: {candle.ClosePrice}, Price MA: {priceMAValue}");
			LogInfo($"Fast Volume MA: {fastMAValue}, Slow Volume MA: {slowMAValue}");
			LogInfo($"Cross Above: {crossAbove}, Cross Below: {crossBelow}");

			// Trading logic:
			// Long: Fast volume MA crosses above slow volume MA
			if (crossAbove && Position <= 0)
			{
				LogInfo($"Buy Signal: Fast Volume MA crossing above Slow Volume MA");
				BuyMarket(Volume + Math.Abs(Position));
			}
			// Short: Fast volume MA crosses below slow volume MA
			else if (crossBelow && Position >= 0)
			{
				LogInfo($"Sell Signal: Fast Volume MA crossing below Slow Volume MA");
				SellMarket(Volume + Math.Abs(Position));
			}
			
			// Exit logic: Reverse crossover
			if (Position > 0 && crossBelow)
			{
				LogInfo($"Exit Long: Fast Volume MA crossing below Slow Volume MA");
				SellMarket(Math.Abs(Position));
			}
			else if (Position < 0 && crossAbove)
			{
				LogInfo($"Exit Short: Fast Volume MA crossing above Slow Volume MA");
				BuyMarket(Math.Abs(Position));
			}

			// Store current values for next comparison
			_previousFastVolumeMA = fastMAValue;
			_previousSlowVolumeMA = slowMAValue;
		}
	}
}