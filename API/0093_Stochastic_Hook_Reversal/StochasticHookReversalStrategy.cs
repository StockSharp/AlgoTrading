using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Stochastic Hook Reversal Strategy.
	/// Enters long when %K forms an upward hook from oversold conditions.
	/// Enters short when %K forms a downward hook from overbought conditions.
	/// </summary>
	public class StochasticHookReversalStrategy : Strategy
	{
		private readonly StrategyParam<int> _stochPeriod;
		private readonly StrategyParam<int> _kPeriod;
		private readonly StrategyParam<int> _dPeriod;
		private readonly StrategyParam<int> _oversoldLevel;
		private readonly StrategyParam<int> _overboughtLevel;
		private readonly StrategyParam<int> _exitLevel;
		private readonly StrategyParam<Unit> _stopLoss;
		private readonly StrategyParam<DataType> _candleType;
		
		private decimal _prevK;

		/// <summary>
		/// Period for Stochastic calculation.
		/// </summary>
		public int StochPeriod
		{
			get => _stochPeriod.Value;
			set => _stochPeriod.Value = value;
		}

		/// <summary>
		/// %K Period for Stochastic calculation.
		/// </summary>
		public int KPeriod
		{
			get => _kPeriod.Value;
			set => _kPeriod.Value = value;
		}

		/// <summary>
		/// %D Period for Stochastic calculation.
		/// </summary>
		public int DPeriod
		{
			get => _dPeriod.Value;
			set => _dPeriod.Value = value;
		}

		/// <summary>
		/// Oversold level for Stochastic.
		/// </summary>
		public int OversoldLevel
		{
			get => _oversoldLevel.Value;
			set => _oversoldLevel.Value = value;
		}

		/// <summary>
		/// Overbought level for Stochastic.
		/// </summary>
		public int OverboughtLevel
		{
			get => _overboughtLevel.Value;
			set => _overboughtLevel.Value = value;
		}

		/// <summary>
		/// Exit level for Stochastic (neutral zone).
		/// </summary>
		public int ExitLevel
		{
			get => _exitLevel.Value;
			set => _exitLevel.Value = value;
		}

		/// <summary>
		/// Stop loss percentage from entry price.
		/// </summary>
		public Unit StopLoss
		{
			get => _stopLoss.Value;
			set => _stopLoss.Value = value;
		}

		/// <summary>
		/// Type of candles to use.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="StochasticHookReversalStrategy"/>.
		/// </summary>
		public StochasticHookReversalStrategy()
		{
			_stochPeriod = Param(nameof(StochPeriod), 14)
				.SetDisplay("Stochastic Period", "Period for Stochastic calculation", "Stochastic Settings")
				.SetRange(7, 21)
				.SetCanOptimize(true);
				
			_kPeriod = Param(nameof(KPeriod), 3)
				.SetDisplay("K Period", "%K Period for Stochastic calculation", "Stochastic Settings")
				.SetRange(1, 5)
				.SetCanOptimize(true);
				
			_dPeriod = Param(nameof(DPeriod), 3)
				.SetDisplay("D Period", "%D Period for Stochastic calculation", "Stochastic Settings")
				.SetRange(1, 5)
				.SetCanOptimize(true);
				
			_oversoldLevel = Param(nameof(OversoldLevel), 20)
				.SetDisplay("Oversold Level", "Oversold level for Stochastic", "Stochastic Settings")
				.SetRange(10, 30)
				.SetCanOptimize(true);
				
			_overboughtLevel = Param(nameof(OverboughtLevel), 80)
				.SetDisplay("Overbought Level", "Overbought level for Stochastic", "Stochastic Settings")
				.SetRange(70, 90)
				.SetCanOptimize(true);
				
			_exitLevel = Param(nameof(ExitLevel), 50)
				.SetDisplay("Exit Level", "Exit level for Stochastic (neutral zone)", "Stochastic Settings")
				.SetRange(45, 55)
				.SetCanOptimize(true);
				
			_stopLoss = Param(nameof(StopLoss), new Unit(2, UnitTypes.Percent))
				.SetDisplay("Stop Loss", "Stop loss as percentage from entry price", "Risk Management")
				.SetRange(1m, 3m)
				.SetCanOptimize(true);
				
			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
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

			// Enable position protection using stop-loss
			StartProtection(
				takeProfit: null,
				stopLoss: StopLoss,
				isStopTrailing: false,
				useMarketOrders: true
			);

			// Initialize previous K value
			_prevK = 0;
			
			// Create Stochastic oscillator
			var stochastic = new StochasticOscillator
			{
				K = { Length = KPeriod },
				D = { Length = DPeriod },
			};

			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind indicator and process candles
			subscription
				.BindEx(stochastic, ProcessCandle)
				.Start();
				
			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, stochastic);
				DrawOwnTrades(area);
			}
		}

		/// <summary>
		/// Process candle with Stochastic values.
		/// </summary>
		/// <param name="candle">Candle.</param>
		/// <param name="kValue">Stochastic %K value.</param>
		/// <param name="dValue">Stochastic %D value.</param>
		private void ProcessCandle(ICandleMessage candle, IIndicatorValue kValue, IIndicatorValue dValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// If this is the first calculation, just store the value
			if (_prevK == 0)
			{
				_prevK = kValue;
				return;
			}

			// Check for Stochastic hooks
			bool oversoldHookUp = _prevK < OversoldLevel && kValue > _prevK;
			bool overboughtHookDown = _prevK > OverboughtLevel && kValue < _prevK;
			
			// Long entry: %K forms an upward hook from oversold
			if (oversoldHookUp && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				LogInfo($"Long entry: Stochastic %K upward hook from oversold ({_prevK} -> {kValue})");
			}
			// Short entry: %K forms a downward hook from overbought
			else if (overboughtHookDown && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				LogInfo($"Short entry: Stochastic %K downward hook from overbought ({_prevK} -> {kValue})");
			}
			
			// Exit conditions based on Stochastic reaching neutral zone
			if (kValue > ExitLevel && Position < 0)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exit short: Stochastic %K reached neutral zone ({kValue} > {ExitLevel})");
			}
			else if (kValue < ExitLevel && Position > 0)
			{
				SellMarket(Position);
				LogInfo($"Exit long: Stochastic %K reached neutral zone ({kValue} < {ExitLevel})");
			}
			
			// Update previous K value
			_prevK = kValue;
		}
	}
}