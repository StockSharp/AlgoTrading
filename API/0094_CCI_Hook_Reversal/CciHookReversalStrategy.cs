using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies
{
	/// <summary>
	/// CCI Hook Reversal Strategy.
	/// Enters long when CCI forms an upward hook from oversold conditions.
	/// Enters short when CCI forms a downward hook from overbought conditions.
	/// </summary>
	public class CciHookReversalStrategy : Strategy
	{
		private readonly StrategyParam<int> _cciPeriod;
		private readonly StrategyParam<int> _oversoldLevel;
		private readonly StrategyParam<int> _overboughtLevel;
		private readonly StrategyParam<Unit> _stopLoss;
		private readonly StrategyParam<DataType> _candleType;
		
		private decimal _prevCci;

		/// <summary>
		/// Period for CCI calculation.
		/// </summary>
		public int CciPeriod
		{
			get => _cciPeriod.Value;
			set => _cciPeriod.Value = value;
		}

		/// <summary>
		/// Oversold level for CCI.
		/// </summary>
		public int OversoldLevel
		{
			get => _oversoldLevel.Value;
			set => _oversoldLevel.Value = value;
		}

		/// <summary>
		/// Overbought level for CCI.
		/// </summary>
		public int OverboughtLevel
		{
			get => _overboughtLevel.Value;
			set => _overboughtLevel.Value = value;
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
		/// Initializes a new instance of the <see cref="CciHookReversalStrategy"/>.
		/// </summary>
		public CciHookReversalStrategy()
		{
			_cciPeriod = Param(nameof(CciPeriod), 20)
				.SetDisplay("CCI Period", "Period for CCI calculation", "CCI Settings")
				.SetRange(14, 30)
				.SetCanOptimize(true);
				
			_oversoldLevel = Param(nameof(OversoldLevel), -100)
				.SetDisplay("Oversold Level", "Oversold level for CCI", "CCI Settings")
				.SetRange(-150, -50)
				.SetCanOptimize(true);
				
			_overboughtLevel = Param(nameof(OverboughtLevel), 100)
				.SetDisplay("Overbought Level", "Overbought level for CCI", "CCI Settings")
				.SetRange(50, 150)
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

			// Initialize previous CCI value
			_prevCci = 0;
			
			// Create CCI indicator
			var cci = new CommodityChannelIndex { Length = CciPeriod };

			// Create subscription
			var subscription = SubscribeCandles(CandleType);
			
			// Bind indicator and process candles
			subscription
				.Bind(cci, ProcessCandle)
				.Start();
				
			// Setup chart visualization
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, cci);
				DrawOwnTrades(area);
			}
		}

		/// <summary>
		/// Process candle with CCI value.
		/// </summary>
		/// <param name="candle">Candle.</param>
		/// <param name="cciValue">CCI value.</param>
		private void ProcessCandle(ICandleMessage candle, decimal cciValue)
		{
			// Skip unfinished candles
			if (candle.State != CandleStates.Finished)
				return;

			// Check if strategy is ready to trade
			if (!IsFormedAndOnlineAndAllowTrading())
				return;
			
			// If this is the first calculation, just store the value
			if (_prevCci == 0)
			{
				_prevCci = cciValue;
				return;
			}

			// Check for CCI hooks
			bool oversoldHookUp = _prevCci < OversoldLevel && cciValue > _prevCci;
			bool overboughtHookDown = _prevCci > OverboughtLevel && cciValue < _prevCci;
			
			// Long entry: CCI forms an upward hook from oversold
			if (oversoldHookUp && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				LogInfo($"Long entry: CCI upward hook from oversold ({_prevCci} -> {cciValue})");
			}
			// Short entry: CCI forms a downward hook from overbought
			else if (overboughtHookDown && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				LogInfo($"Short entry: CCI downward hook from overbought ({_prevCci} -> {cciValue})");
			}
			
			// Exit conditions based on CCI crossing zero line
			if (cciValue > 0 && Position < 0)
			{
				BuyMarket(Math.Abs(Position));
				LogInfo($"Exit short: CCI crossed above zero ({cciValue})");
			}
			else if (cciValue < 0 && Position > 0)
			{
				SellMarket(Position);
				LogInfo($"Exit long: CCI crossed below zero ({cciValue})");
			}
			
			// Update previous CCI value
			_prevCci = cciValue;
		}
	}
}