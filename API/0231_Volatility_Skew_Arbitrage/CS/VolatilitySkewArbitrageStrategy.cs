using System;
using System.Collections.Generic;
using StockSharp.Algo;
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
			_optionWithLowVolParam = Param<Security>(nameof(OptionWithLowVol))
				.SetDisplay("Option with Low Vol", "The option instrument with lower implied volatility", "Instruments");

			_optionWithHighVolParam = Param<Security>(nameof(OptionWithHighVol))
				.SetDisplay("Option with High Vol", "The option instrument with higher implied volatility", "Instruments");

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
		protected override void OnReseted()
		{
			base.OnReseted();
			_volSkewStdDev.Reset();
			_barCount = 0;
			_avgVolSkew = 0;
			_currentVolSkew = 0;
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);


			// Subscribe to implied volatility for both options
			if (OptionWithLowVol != null && OptionWithHighVol != null)
			{
				SubscribeLevel1(OptionWithLowVol)
					.Bind(ProcessLowOptionImpliedVolatility)
					.Start();

				SubscribeLevel1(OptionWithHighVol)
					.Bind(ProcessHighOptionImpliedVolatility)
					.Start();
			}
			else
			{
				LogWarning("Option instruments not specified. Strategy won't work properly.");
			}

			// Start position protection with stop-loss
			StartProtection(
				takeProfit: null,
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent)
			);
		}

		private void ProcessLowOptionImpliedVolatility(Level1ChangeMessage data)
		{
			if (data.TryGetDecimal(Level1Fields.ImpliedVolatility) is not decimal lowIV)
				return;

			var highIV = _currentVolSkew + lowIV;
			
			UpdateVolatilitySkew(highIV - lowIV, data.ServerTime, true);
		}

		private void ProcessHighOptionImpliedVolatility(Level1ChangeMessage data)
		{
			if (data.TryGetDecimal(Level1Fields.ImpliedVolatility) is not decimal highIV)
				return;

			_currentVolSkew = highIV;
		}

		private void UpdateVolatilitySkew(decimal volSkew, DateTimeOffset time, bool isFinal)
		{
			if (volSkew == 0)
				return;

			// Process volatility skew through the indicator
			var stdDevValue = _volSkewStdDev.Process(volSkew, time, isFinal);

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

			var stdDev = stdDevValue.ToDecimal();
			
			// Trading logic for volatility skew arbitrage
			if (volSkew > _avgVolSkew + Threshold * stdDev && Position <= 0)
			{
				// Long low vol option, short high vol option
				LogInfo($"Volatility skew above threshold: {volSkew} > {_avgVolSkew + Threshold * stdDev}");
				BuyMarket(Volume, OptionWithLowVol);
				SellMarket(Volume, OptionWithHighVol);
			}
			else if (volSkew < _avgVolSkew - Threshold * stdDev && Position >= 0)
			{
				// Short low vol option, long high vol option
				LogInfo($"Volatility skew below threshold: {volSkew} < {_avgVolSkew - Threshold * stdDev}");
				SellMarket(Volume, OptionWithLowVol);
				BuyMarket(Volume, OptionWithHighVol);
			}
			else if (Math.Abs(volSkew - _avgVolSkew) < 0.2m * stdDev)
			{
				// Close position when vol skew returns to average
				LogInfo($"Volatility skew returned to average: {volSkew} ≈ {_avgVolSkew}");
				
				if (GetPositionValue(OptionWithLowVol) > 0)
					SellMarket(Math.Abs(GetPositionValue(OptionWithLowVol)), OptionWithLowVol);
					
				if (GetPositionValue(OptionWithLowVol) < 0)
					BuyMarket(Math.Abs(GetPositionValue(OptionWithLowVol)), OptionWithLowVol);
					
				if (GetPositionValue(OptionWithHighVol) > 0)
					SellMarket(Math.Abs(GetPositionValue(OptionWithHighVol)), OptionWithHighVol);
					
				if (GetPositionValue(OptionWithHighVol) < 0)
					BuyMarket(Math.Abs(GetPositionValue(OptionWithHighVol)), OptionWithHighVol);
			}
		}

		private decimal GetPositionValue(Security security)
		{
			return GetPositionValue(security, Portfolio) ?? 0;
		}
	}
}
