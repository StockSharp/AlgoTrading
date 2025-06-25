using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// CCI Breakout Strategy that enters positions when CCI breaks out of its normal range.
	/// </summary>
	public class CciBreakoutStrategy : Strategy
	{
		private readonly StrategyParam<int> _cciPeriod;
		private readonly StrategyParam<int> _smaPeriod;
		private readonly StrategyParam<decimal> _deviationMultiplier;
		private readonly StrategyParam<decimal> _stopLossPercent;
		private readonly StrategyParam<DataType> _candleType;

		private CommodityChannelIndex _cci;
		private SimpleMovingAverage _cciSma;
		private StandardDeviation _cciStdDev;
		
		private decimal _prevCciValue;
		private decimal _prevCciSmaValue;

		/// <summary>
		/// Period for CCI calculation.
		/// </summary>
		public int CciPeriod
		{
			get => _cciPeriod.Value;
			set => _cciPeriod.Value = value;
		}

		/// <summary>
		/// Period for CCI moving average.
		/// </summary>
		public int SmaPeriod
		{
			get => _smaPeriod.Value;
			set => _smaPeriod.Value = value;
		}

		/// <summary>
		/// Standard deviation multiplier for breakout threshold.
		/// </summary>
		public decimal DeviationMultiplier
		{
			get => _deviationMultiplier.Value;
			set => _deviationMultiplier.Value = value;
		}

		/// <summary>
		/// Stop-loss percentage.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercent.Value;
			set => _stopLossPercent.Value = value;
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
		/// Constructor.
		/// </summary>
		public CciBreakoutStrategy()
		{
			_cciPeriod = Param(nameof(CciPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("CCI Period", "Period for CCI calculation", "Indicator Settings")
				.SetCanOptimize(true)
				.SetOptimize(10, 40, 5);

			_smaPeriod = Param(nameof(SmaPeriod), 20)
				.SetGreaterThanZero()
				.SetDisplay("SMA Period", "Period for CCI moving average", "Indicator Settings")
				.SetCanOptimize(true)
				.SetOptimize(10, 40, 10);

			_deviationMultiplier = Param(nameof(DeviationMultiplier), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Deviation Multiplier", "Standard deviation multiplier for breakout threshold", "Breakout Settings")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
				.SetGreaterThanZero()
				.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 4.0m, 0.5m);

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
			// Initialize indicators
			_cci = new CommodityChannelIndex { Length = CciPeriod };
			_cciSma = new SimpleMovingAverage { Length = SmaPeriod };
			_cciStdDev = new StandardDeviation { Length = SmaPeriod };

			// Create subscription and bind indicator
			var subscription = SubscribeCandles(CandleType);
			
			subscription
				.Bind(_cci, ProcessCandle)
				.Start();

			// Enable position protection
			StartProtection(
				new Unit(StopLossPercent, UnitTypes.Percent),
				new Unit(StopLossPercent * 1.5m, UnitTypes.Percent));

			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, _cci);
				DrawOwnTrades(area);
			}

			base.OnStarted(time);
		}

		private void ProcessCandle(ICandleMessage candle, decimal cciValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready for trading
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Process indicators
			var cciInput = new DecimalIndicatorValue(cciValue);
			var cciSmaValue = _cciSma.Process(cciInput).GetValue<decimal>();
			var cciStdDevValue = _cciStdDev.Process(cciInput).GetValue<decimal>();
			
			// Store previous values on first call
			if (_prevCciValue == 0 && _prevCciSmaValue == 0)
			{
				_prevCciValue = cciValue;
				_prevCciSmaValue = cciSmaValue;
				return;
			}

			// Calculate breakout thresholds
			var upperThreshold = cciSmaValue + DeviationMultiplier * cciStdDevValue;
			var lowerThreshold = cciSmaValue - DeviationMultiplier * cciStdDevValue;

			// Trading logic
			if (cciValue > upperThreshold && Position <= 0)
			{
				// CCI broke above upper threshold - buy signal (long)
				BuyMarket(Volume);
				LogInfo($"Buy signal: CCI({cciValue}) > Upper Threshold({upperThreshold})");
			}
			else if (cciValue < lowerThreshold && Position >= 0)
			{
				// CCI broke below lower threshold - sell signal (short)
				SellMarket(Volume + Math.Abs(Position));
				LogInfo($"Sell signal: CCI({cciValue}) < Lower Threshold({lowerThreshold})");
			}
			// Exit conditions
			else if (Position > 0 && cciValue < cciSmaValue)
			{
				// Exit long position when CCI returns below its mean
				SellMarket(Math.Abs(Position));
				LogInfo($"Exit long: CCI({cciValue}) < CCI SMA({cciSmaValue})");
			}
			else if (Position < 0 && cciValue > cciSmaValue)
			{
				// Exit short position when CCI returns above its mean
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exit short: CCI({cciValue}) > CCI SMA({cciSmaValue})");
			}

			// Update previous values
			_prevCciValue = cciValue;
			_prevCciSmaValue = cciSmaValue;
		}
	}
}
