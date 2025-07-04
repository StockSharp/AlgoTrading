using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Stochastic Oscillator and Keltner Channels indicators (#208)
	/// </summary>
	public class StochasticKeltnerStrategy : Strategy
	{
		private readonly StrategyParam<int> _stochPeriod;
		private readonly StrategyParam<int> _stochK;
		private readonly StrategyParam<int> _stochD;
		private readonly StrategyParam<int> _emaPeriod;
		private readonly StrategyParam<decimal> _keltnerMultiplier;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<decimal> _atrMultiplier;
		private readonly StrategyParam<DataType> _candleType;

		/// <summary>
		/// Stochastic period
		/// </summary>
		public int StochPeriod
		{
			get => _stochPeriod.Value;
			set => _stochPeriod.Value = value;
		}

		/// <summary>
		/// Stochastic %K smoothing period
		/// </summary>
		public int StochK
		{
			get => _stochK.Value;
			set => _stochK.Value = value;
		}

		/// <summary>
		/// Stochastic %D smoothing period
		/// </summary>
		public int StochD
		{
			get => _stochD.Value;
			set => _stochD.Value = value;
		}

		/// <summary>
		/// EMA period for Keltner Channel
		/// </summary>
		public int EmaPeriod
		{
			get => _emaPeriod.Value;
			set => _emaPeriod.Value = value;
		}

		/// <summary>
		/// Keltner Channel multiplier (k)
		/// </summary>
		public decimal KeltnerMultiplier
		{
			get => _keltnerMultiplier.Value;
			set => _keltnerMultiplier.Value = value;
		}

		/// <summary>
		/// ATR period for Keltner Channel and stop-loss
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}

		/// <summary>
		/// ATR multiplier for stop-loss
		/// </summary>
		public decimal AtrMultiplier
		{
			get => _atrMultiplier.Value;
			set => _atrMultiplier.Value = value;
		}

		/// <summary>
		/// Candle type for strategy
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public StochasticKeltnerStrategy()
		{
			_stochPeriod = Param(nameof(StochPeriod), 14)
				.SetRange(5, 30)
				.SetDisplay("Stoch Period", "Period for Stochastic Oscillator", "Stochastic")
				.SetCanOptimize(true);

			_stochK = Param(nameof(StochK), 3)
				.SetRange(1, 10)
				.SetDisplay("Stoch %K", "Stochastic %K smoothing period", "Stochastic")
				.SetCanOptimize(true);

			_stochD = Param(nameof(StochD), 3)
				.SetRange(1, 10)
				.SetDisplay("Stoch %D", "Stochastic %D smoothing period", "Stochastic")
				.SetCanOptimize(true);

			_emaPeriod = Param(nameof(EmaPeriod), 20)
				.SetRange(10, 50)
				.SetDisplay("EMA Period", "EMA period for Keltner Channel", "Keltner")
				.SetCanOptimize(true);

			_keltnerMultiplier = Param(nameof(KeltnerMultiplier), 2m)
				.SetRange(1m, 4m)
				.SetDisplay("K Multiplier", "Multiplier for Keltner Channel", "Keltner")
				.SetCanOptimize(true);

			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetRange(7, 28)
				.SetDisplay("ATR Period", "ATR period for Keltner Channel and stop-loss", "Risk Management")
				.SetCanOptimize(true);

			_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
				.SetRange(1m, 4m)
				.SetDisplay("ATR Multiplier", "Multiplier for ATR-based stop-loss", "Risk Management")
				.SetCanOptimize(true);

			_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

			// Initialize indicators
			var stochastic = new StochasticOscillator
			{
				K = { Length = StochPeriod },
				D = { Length = StochD },
			};

			var keltner = new KeltnerChannels
			{
				Length = EmaPeriod,
			};

			var atr = new AverageTrueRange { Length = AtrPeriod };

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			subscription
				.BindEx(keltner, stochastic, atr, ProcessIndicators)
				.Start();
			
			// Enable ATR-based stop protection
			StartProtection(default, new Unit(AtrMultiplier, UnitTypes.Absolute));

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, keltner);
				DrawIndicator(area, stochastic);
				DrawOwnTrades(area);
			}
		}

		private void ProcessIndicators(ICandleMessage candle, IIndicatorValue keltnerValue, IIndicatorValue stochValue, IIndicatorValue atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var price = candle.ClosePrice;

			var keltnerTyped = (KeltnerChannelsValue)keltnerValue;
			var upperBand = keltnerTyped.Upper;
			var lowerBand = keltnerTyped.Lower;
			var middleBand = keltnerTyped.Middle;

			// Trading logic:
			// Long: Stoch %K < 20 && Price < Keltner lower band (oversold at lower band)
			// Short: Stoch %K > 80 && Price > Keltner upper band (overbought at upper band)

			var stochTyped = (StochasticOscillatorValue)stochValue;
			var stochK = stochTyped.K;
			var stochD = stochTyped.D;

			if (stochK < 20 && price < lowerBand && Position <= 0)
			{
				// Buy signal - Stochastic oversold at Keltner lower band
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}
			else if (stochK > 80 && price > upperBand && Position >= 0)
			{
				// Sell signal - Stochastic overbought at Keltner upper band
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
			}
			// Exit conditions
			else if (Position > 0 && price > middleBand)
			{
				// Exit long position when price returns to middle band
				SellMarket(Position);
			}
			else if (Position < 0 && price < middleBand)
			{
				// Exit short position when price returns to middle band
				BuyMarket(Math.Abs(Position));
			}
		}
	}
}
