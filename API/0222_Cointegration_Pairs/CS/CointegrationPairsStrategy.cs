using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// Cointegration pairs trading strategy.
	/// Trades based on cointegration relationship between two assets.
	/// </summary>
	public class CointegrationPairsStrategy : Strategy
	{
		private readonly StrategyParam<int> _periodParam;
		private readonly StrategyParam<decimal> _entryThresholdParam;
		private readonly StrategyParam<decimal> _betaParam;
		private readonly StrategyParam<Security> _asset2Param;
		private readonly StrategyParam<decimal> _stopLossPercentParam;
		private readonly StrategyParam<DataType> _candleTypeParam;

		private decimal _residualMean;
		private decimal _residualStdDev;
		private decimal _residualSum;
		private decimal _squaredResidualSum;
		private readonly Queue<decimal> _residuals = [];
		
		private decimal _asset1Price;
		private decimal _asset2Price;

		private Portfolio _asset2Portfolio;

		/// <summary>
		/// Period for calculation of residual mean and standard deviation.
		/// </summary>
		public int Period
		{
			get => _periodParam.Value;
			set => _periodParam.Value = value;
		}

		/// <summary>
		/// Entry threshold as a multiple of standard deviation.
		/// </summary>
		public decimal EntryThreshold
		{
			get => _entryThresholdParam.Value;
			set => _entryThresholdParam.Value = value;
		}

		/// <summary>
		/// Beta coefficient for calculation of residual.
		/// </summary>
		public decimal Beta
		{
			get => _betaParam.Value;
			set => _betaParam.Value = value;
		}

		/// <summary>
		/// Second asset for pair trading.
		/// </summary>
		public Security Asset2
		{
			get => _asset2Param.Value;
			set => _asset2Param.Value = value;
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
		/// Candle type for strategy.
		/// </summary>
		public DataType CandleType
		{
			get => _candleTypeParam.Value;
			set => _candleTypeParam.Value = value;
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public CointegrationPairsStrategy()
		{
			_periodParam = Param(nameof(Period), 20)
				.SetGreaterThanZero()
				.SetDisplay("Period", "Period for residual calculations", "Parameters")
				.SetCanOptimize(true)
				.SetOptimize(10, 50, 10);

			_entryThresholdParam = Param(nameof(EntryThreshold), 2.0m)
				.SetRange(0.1m, decimal.MaxValue)
				.SetDisplay("Entry Threshold", "Entry threshold as multiple of standard deviation", "Parameters")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 3.0m, 0.5m);

			_betaParam = Param(nameof(Beta), 1.0m)
				.SetRange(0.01m, decimal.MaxValue)
				.SetDisplay("Beta", "Coefficient of cointegration", "Parameters")
				.SetCanOptimize(true)
				.SetOptimize(0.5m, 2.0m, 0.1m);

			_asset2Param = Param<Security>(nameof(Asset2))
				.SetDisplay("Asset 2", "Second asset for pair trading", "Parameters");

			_stopLossPercentParam = Param(nameof(StopLossPercent), 2.0m)
				.SetRange(0.1m, decimal.MaxValue)
				.SetDisplay("Stop Loss %", "Stop loss percentage", "Parameters")
				.SetCanOptimize(true)
				.SetOptimize(1.0m, 5.0m, 1.0m);

			_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Candle type for strategy", "Common");
		}

		/// <inheritdoc />
		public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		{
			return
			[
				(Security, CandleType),
				(Asset2, CandleType)
			];
		}

		/// <inheritdoc />
		protected override void OnReseted()
		{
			base.OnReseted();

			_residualMean = 0;
			_residualStdDev = 0;
			_residualSum = 0;
			_squaredResidualSum = 0;
			_residuals.Clear();
			_asset1Price = 0;
			_asset2Price = 0;

			// Use the same portfolio for second asset or find another portfolio
			_asset2Portfolio = Portfolio;
		}

		/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
			base.OnStarted(time);

			if (Asset2 == null)
				throw new InvalidOperationException("Second asset is not specified.");
			
			// Subscribe to Asset1 candles
			var asset1Subscription = SubscribeCandles(CandleType)
				.Bind(ProcessAsset1Candle)
				.Start();

			// Subscribe to Asset2 candles
			var asset2Subscription = SubscribeCandles(CandleType, security: Asset2)
				.Bind(ProcessAsset2Candle)
				.Start();

			// Setup chart visualization if available
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, asset1Subscription);
				DrawOwnTrades(area);
			}
			
			// Enable position protection with stop loss
			StartProtection(
				takeProfit: new Unit(0, UnitTypes.Absolute), // No take profit
				stopLoss: new Unit(StopLossPercent, UnitTypes.Percent) // Stop loss percentage
			);
		}
		
		private void ProcessAsset1Candle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;
				
			_asset1Price = candle.ClosePrice;
			ProcessPair();
		}
		
		private void ProcessAsset2Candle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;
				
			_asset2Price = candle.ClosePrice;
			ProcessPair();
		}

		private void ProcessPair()
		{
			if (_asset1Price == 0 || _asset2Price == 0)
				return;
				
			if (!IsFormedAndOnlineAndAllowTrading())
				return;

			// Calculate residual = Asset1Price - Beta * Asset2Price
			var residual = _asset1Price - Beta * _asset2Price;
			
			// Track residual statistics over period
			_residuals.Enqueue(residual);
			_residualSum += residual;
			_squaredResidualSum += residual * residual;
			
			if (_residuals.Count > Period)
			{
				var oldResidual = _residuals.Dequeue();
				_residualSum -= oldResidual;
				_squaredResidualSum -= oldResidual * oldResidual;
			}
			
			if (_residuals.Count == Period)
			{
				// Calculate mean and standard deviation
				_residualMean = _residualSum / Period;
				
				var variance = (_squaredResidualSum / Period) - (_residualMean * _residualMean);
				_residualStdDev = variance <= 0 ? 0.0001m : (decimal)Math.Sqrt((double)variance);
				
				// Calculate z-score of current residual
				var zScore = (_residualStdDev == 0) ? 0 : (residual - _residualMean) / _residualStdDev;
				
				// Check for trading signals
				if (zScore < -EntryThreshold && Position <= 0)
				{
					// Long Asset1, Short Asset2
					// First, close any existing short position on Asset1
					BuyMarket(Volume + Math.Abs(Position));
					
					// Then, short Asset2 using the second portfolio
					if (_asset2Portfolio != null)
					{
						var asset2Order = new Order
						{
							Side = Sides.Sell,
							Security = Asset2,
							Portfolio = _asset2Portfolio,
							Volume = Volume * Beta
						};
						
						RegisterOrder(asset2Order);
					}
				}
				else if (zScore > EntryThreshold && Position >= 0)
				{
					// Short Asset1, Long Asset2
					// First, close any existing long position on Asset1
					SellMarket(Volume + Math.Abs(Position));
					
					// Then, buy Asset2 using the second portfolio
					if (_asset2Portfolio != null)
					{
						var asset2Order = new Order
						{
							Side = Sides.Buy,
							Security = Asset2,
							Portfolio = _asset2Portfolio,
							Volume = Volume * Beta
						};
						
						RegisterOrder(asset2Order);
					}
				}
				else if (Math.Abs(zScore) < 0.5m)
				{
					// Close positions when spread reverts to mean
					if (Position != 0)
					{
						if (Position > 0)
							SellMarket(Position);
						else
							BuyMarket(Math.Abs(Position));
						
						// Close position on Asset2
						if (_asset2Portfolio != null)
						{
							var asset2Order = new Order
							{
								Side = Position > 0 ? Sides.Buy : Sides.Sell,
								Security = Asset2,
								Portfolio = _asset2Portfolio,
								Volume = Volume * Beta
							};
							
							RegisterOrder(asset2Order);
						}
					}
				}
			}
			
			// Reset prices for next update
			_asset1Price = 0;
			_asset2Price = 0;
		}
	}
}