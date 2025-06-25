using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Volatility Skew Arbitrage strategy that trades options based on volatility skew anomalies.
	/// </summary>
	public class VolatilitySkewArbitrageStrategy : Strategy
	{
		private readonly StrategyParam<Security> _optionWithLowVolParam;
		private readonly StrategyParam<Security> _optionWithHighVolParam;
		private readonly StrategyParam<Portfolio> _portfolioParam;
		private readonly StrategyParam<decimal> _volumeParam;
		private readonly StrategyParam<int> _lookbackPeriodParam;
		private readonly StrategyParam<decimal> _thresholdParam;
		private readonly StrategyParam<decimal> _stopLossPercentParam;

		private readonly StandardDeviation _volSkewStdDev;
		private decimal _avgVolSkew;
		private int _barCount;
		private decimal _currentVolSkew;

		/// <summary>
		/// Option with lower implied volatility.
		/// </summary>
		public Security OptionWithLowVol
		{
			get => _optionWithLowVolParam.Value;
			set => _optionWithLowVolParam.Value = value;
		}

		/// <summary>
		/// Option with higher implied volatility.
		/// </summary>
		public Security OptionWithHighVol
		{
			get => _optionWithHighVolParam.Value;
			set => _optionWithHighVolParam.Value = value;
		}

		/// <summary>
		/// Portfolio for trading.
		/// </summary>
		public Portfolio Portfolio
		{
			get => _portfolioParam.Value;
			set => _portfolioParam.Value = value;
		}

		/// <summary>
		/// Trading volume.
		/// </summary>
		public decimal Volume
		{
			get => _volumeParam.Value;
			set => _volumeParam.Value = value;
		}

		/// <summary>
		/// Period for calculating average volatility skew.
		/// </summary>
		public int LookbackPeriod
		{
			get => _lookbackPeriodParam.Value;
			set => _lookbackPeriodParam.Value = value;
		}

		/// <summary>
		/// Threshold multiplier for standard deviation.
		/// </summary>
		public decimal Threshold
		{
			get => _thresholdParam.Value;
			set => _thresholdParam.Value = value;
		}

		/// <summary>
		/// Stop loss percentage.
		/// </summary>
		public decimal StopLossPercent
		{
			get => _stopLossPercentParam.Value;
			set => _stopLossPercentParam.Value = value;
		}

		/// <summary>
		/// Strategy constructor.
		/// </summary>
		public VolatilitySkewArbitrageStrategy()
		{
			_optionWithLowVolParam = Param(nameof(OptionWithLowVol))
				.SetDisplay("Option with Low Vol", "The option instrument with lower implied volatility", "Instruments");

			_optionWithHighVolParam = Param(nameof(OptionWithHighVol))
				.SetDisplay("Option with High Vol", "The option instrument with higher implied volatility", "Instruments");

			_portfolioParam = Param(nameof(Portfolio))
				.SetDisplay("Portfolio", "Portfolio for trading", "General");

			_volumeParam = Param(nameof(Volume), 1m)
				.SetDisplay("Volume", "Trading volume", "General")
				.SetNotNegative();

			_lookbackPeriodParam = Param(nameof(LookbackPeriod), 20)
				.SetDisplay("Lookback Period", "Period for calculating average volatility skew", "Strategy")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 5)
				.SetGreaterThanZero();

			_thresholdParam = Param(nameof(Threshold), 2m)
				.SetDisplay("Threshold", "Threshold multiplier for standard deviation", "Strategy")
				.SetCanOptimize(true)
				.SetOptimize(1m, 3m, 0.5m)
				.SetNotNegative();

			_stopLossPercentParam = Param(nameof(StopLossPercent), 2m)
				.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
				.SetNotNegative();

			_volSkewStdDev = new StandardDeviation { Length = LookbackPeriod };
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			if (OptionWithLowVol != null)
				yield return (OptionWithLowVol, DataType.ImpliedVolatility);

			if (OptionWithHighVol != null)
				yield return (OptionWithHighVol, DataType.ImpliedVolatility);
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			_barCount = 0;
			_avgVolSkew = 0;
			_currentVolSkew = 0;

			// Subscribe to implied volatility for both options
			if (OptionWithLowVol != null && OptionWithHighVol != null)
			{
				var lowVolSubscription = new Subscription(DataType.ImpliedVolatility, OptionWithLowVol);
				var highVolSubscription = new Subscription(DataType.ImpliedVolatility, OptionWithHighVol);

				// Create a rule to process implied volatility data
				this.SuspendRules(() =>
				{
					lowVolSubscription
						.WhenLevel1ReceivingImpliedVolatility(this)
						.Do(ProcessLowOptionImpliedVolatility)
						.Apply(this);

					highVolSubscription
						.WhenLevel1ReceivingImpliedVolatility(this)
						.Do(ProcessHighOptionImpliedVolatility)
						.Apply(this);
				});

				Subscribe(lowVolSubscription);
				Subscribe(highVolSubscription);
			}
			else
			{
				this.AddWarningLog("Option instruments not specified. Strategy won't work properly.");
			}

			// Start position protection with stop-loss
			StartProtection(
				takeProfit: null,
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
			);
		}

		private void ProcessLowOptionImpliedVolatility(Level1ChangeMessage data)
		{
			var lowIV = data.TryGetImpliedVolatility() ?? 0;
			var highIV = _currentVolSkew + lowIV;
			
			UpdateVolatilitySkew(highIV - lowIV);
		}

		private void ProcessHighOptionImpliedVolatility(Level1ChangeMessage data)
		{
			var highIV = data.TryGetImpliedVolatility() ?? 0;
			_currentVolSkew = highIV;
		}

		private void UpdateVolatilitySkew(decimal volSkew)
		{
			if (volSkew == 0)
				return;

			// Process volatility skew through the indicator
			var stdDevValue = _volSkewStdDev.Process(new DecimalIndicatorValue(volSkew));

			// Update running average for the first LookbackPeriod bars
			if (_barCount < LookbackPeriod)
			{
				_avgVolSkew = (_avgVolSkew * _barCount + volSkew) / (_barCount + 1);
				_barCount++;
				return;
			}

			// Moving average calculation after initial period
			_avgVolSkew = (_avgVolSkew * (LookbackPeriod - 1) + volSkew) / LookbackPeriod;

			// Check if we have enough data
			if (!_volSkewStdDev.IsFormed)
				return;

			// Check trading conditions
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			var stdDev = stdDevValue.GetValue<decimal>();
			
			// Trading logic for volatility skew arbitrage
			if (volSkew > _avgVolSkew + Threshold * stdDev && Position <= 0)
			{
				// Long low vol option, short high vol option
				LogInfo($"Volatility skew above threshold: {volSkew} > {_avgVolSkew + Threshold * stdDev}");
				BuyMarket(OptionWithLowVol, Volume);
				SellMarket(OptionWithHighVol, Volume);
			}
			else if (volSkew < _avgVolSkew - Threshold * stdDev && Position >= 0)
			{
				// Short low vol option, long high vol option
				LogInfo($"Volatility skew below threshold: {volSkew} < {_avgVolSkew - Threshold * stdDev}");
				SellMarket(OptionWithLowVol, Volume);
				BuyMarket(OptionWithHighVol, Volume);
			}
			else if (Math.Abs(volSkew - _avgVolSkew) < 0.2m * stdDev)
			{
				// Close position when vol skew returns to average
				LogInfo($"Volatility skew returned to average: {volSkew} ≈ {_avgVolSkew}");
				
				if (GetPositionValue(OptionWithLowVol) > 0)
					SellMarket(OptionWithLowVol, Math.Abs(GetPositionValue(OptionWithLowVol)));
					
				if (GetPositionValue(OptionWithLowVol) < 0)
					BuyMarket(OptionWithLowVol, Math.Abs(GetPositionValue(OptionWithLowVol)));
					
				if (GetPositionValue(OptionWithHighVol) > 0)
					SellMarket(OptionWithHighVol, Math.Abs(GetPositionValue(OptionWithHighVol)));
					
				if (GetPositionValue(OptionWithHighVol) < 0)
					BuyMarket(OptionWithHighVol, Math.Abs(GetPositionValue(OptionWithHighVol)));
			}
		}

		private decimal GetPositionValue(Security security)
		{
			return security is null ? 0 : PositionManager.Positions.TryGetValue(security)?.Value ?? 0;
		}
	}
}
