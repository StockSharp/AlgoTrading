using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Collections;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Strategy based on Hull Moving Average with volatility contraction filter.
	/// </summary>
	public class HullMaVolatilityContractionStrategy : Strategy
	{
		private readonly StrategyParam<int> _hmaPeriod;
		private readonly StrategyParam<int> _atrPeriod;
		private readonly StrategyParam<decimal> _volatilityContractionFactor;
		private readonly StrategyParam<DataType> _candleType;
		
		private HullMovingAverage _hma;
		private AverageTrueRange _atr;
		
		// Store values for analysis
		private readonly SynchronizedList<decimal> _atrValues = [];
		private decimal _prevHmaValue;
		private decimal _currentHmaValue;
		private bool _isLongPosition;
		private bool _isShortPosition;

		/// <summary>
		/// Hull Moving Average period.
		/// </summary>
		public int HmaPeriod
		{
			get => _hmaPeriod.Value;
			set => _hmaPeriod.Value = value;
		}

		/// <summary>
		/// Average True Range period for volatility calculation.
		/// </summary>
		public int AtrPeriod
		{
			get => _atrPeriod.Value;
			set => _atrPeriod.Value = value;
		}

		/// <summary>
		/// Volatility contraction factor (standard deviation multiplier).
		/// </summary>
		public decimal VolatilityContractionFactor
		{
			get => _volatilityContractionFactor.Value;
			set => _volatilityContractionFactor.Value = value;
		}

		/// <summary>
		/// Candle type to use for the strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleType.Value;
			set => _candleType.Value = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HullMaVolatilityContractionStrategy"/>.
		/// </summary>
		public HullMaVolatilityContractionStrategy()
		{
			_hmaPeriod = Param(nameof(HmaPeriod), 9)
				.SetDisplay("Hull MA Period", "Hull Moving Average period", "Hull MA")
				.SetCanOptimize(true)
				.SetOptimize(5, 20, 1);

			_atrPeriod = Param(nameof(AtrPeriod), 14)
				.SetDisplay("ATR Period", "Period for ATR volatility calculation", "Volatility")
				.SetCanOptimize(true)
				.SetOptimize(10, 30, 2);

			_volatilityContractionFactor = Param(nameof(VolatilityContractionFactor), 2.0m)
				.SetDisplay("Volatility Contraction Factor", "Standard deviation multiplier for volatility contraction", "Volatility")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

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

			// Create indicators
			_hma = new HullMovingAverage
			{
				Length = HmaPeriod
			};
			
			_atr = new AverageTrueRange
			{
				Length = AtrPeriod
			};

			// Create subscription and bind indicators
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.BindEach(
					_hma,
					_atr,
					ProcessCandle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _hma);
				DrawIndicator(area, _atr);
				DrawOwnTrades(area);
			}
			
			// Setup position protection
			StartProtection(
				new Unit(2, UnitTypes.Percent), 
				new Unit(2, UnitTypes.Percent)
			);
		}
		
		private void ProcessCandle(ICandleMessage candle, IIndicatorValue hmaValue, IIndicatorValue atrValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Save previous HMA value
			_prevHmaValue = _currentHmaValue;
			
			// Extract values from indicators
			_currentHmaValue = hmaValue.ToDecimal();
			decimal atr = atrValue.ToDecimal();
			
			// Store ATR values for volatility analysis
			_atrValues.Add(atr);
			
			// Keep only needed history
			while (_atrValues.Count > AtrPeriod * 2)
				_atrValues.RemoveAt(0);
			
			// Check for volatility contraction
			bool isVolatilityContracted = IsVolatilityContracted();
			
			// Determine HMA trend direction
			bool isHmaRising = _currentHmaValue > _prevHmaValue;
			bool isHmaFalling = _currentHmaValue < _prevHmaValue;
			
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
				
			// Log current status
			if (_atrValues.Count >= AtrPeriod)
			{
				decimal avgAtr = _atrValues.Skip(Math.Max(0, _atrValues.Count - AtrPeriod)).Average();
				LogInfo($"HMA: {_currentHmaValue:F2} (Prev: {_prevHmaValue:F2}), ATR: {atr:F2}, Avg ATR: {avgAtr:F2}, Volatility Contracted: {isVolatilityContracted}");
			}
			
			// Trading logic
			// Buy when HMA is rising and volatility is contracted
			if (isHmaRising && isVolatilityContracted && Position <= 0)
			{
				BuyMarket(Volume);
				LogInfo($"Buy Signal: HMA Rising ({_prevHmaValue:F2} -> {_currentHmaValue:F2}) with Contracted Volatility");
				_isLongPosition = true;
				_isShortPosition = false;
			}
			// Sell when HMA is falling and volatility is contracted
			else if (isHmaFalling && isVolatilityContracted && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				LogInfo($"Sell Signal: HMA Falling ({_prevHmaValue:F2} -> {_currentHmaValue:F2}) with Contracted Volatility");
				_isLongPosition = false;
				_isShortPosition = true;
			}
			// Exit long position when HMA starts falling
			else if (_isLongPosition && isHmaFalling)
			{
				SellMarket(Position);
				LogInfo($"Exit Long: HMA started falling ({_prevHmaValue:F2} -> {_currentHmaValue:F2})");
				_isLongPosition = false;
			}
			// Exit short position when HMA starts rising
			else if (_isShortPosition && isHmaRising)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exit Short: HMA started rising ({_prevHmaValue:F2} -> {_currentHmaValue:F2})");
				_isShortPosition = false;
			}
		}
		
		private bool IsVolatilityContracted()
		{
			// Need enough ATR values for calculation
			if (_atrValues.Count < AtrPeriod)
				return false;
				
			// Get recent ATR values for analysis
			var recentAtrValues = _atrValues.Skip(Math.Max(0, _atrValues.Count - AtrPeriod)).ToList();
			
			// Calculate mean and standard deviation
			decimal mean = recentAtrValues.Average();
			decimal sumSquaredDifferences = recentAtrValues.Sum(x => (x - mean) * (x - mean));
			decimal standardDeviation = (decimal)Math.Sqrt((double)(sumSquaredDifferences / recentAtrValues.Count));
			
			// Get current ATR (latest)
			decimal currentAtr = _atrValues.Last();
			
			// Check if current ATR is less than mean minus standard deviation * factor
			bool isContracted = currentAtr < (mean - standardDeviation * VolatilityContractionFactor);
			
			// Log details if contraction is detected
			if (isContracted)
			{
				LogInfo($"Volatility Contraction Detected: Current ATR {currentAtr:F2} < Mean {mean:F2} - (StdDev {standardDeviation:F2} * Factor {VolatilityContractionFactor})");
			}
			
			return isContracted;
		}
	}
}