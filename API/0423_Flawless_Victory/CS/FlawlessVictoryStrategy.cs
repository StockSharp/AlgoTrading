namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Flawless Victory Strategy
/// </summary>
public class FlawlessVictoryStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _version;
	private readonly StrategyParam<decimal> _v2StopLossPercent;
	private readonly StrategyParam<decimal> _v2TakeProfitPercent;
	private readonly StrategyParam<decimal> _v3StopLossPercent;
	private readonly StrategyParam<decimal> _v3TakeProfitPercent;

	private RelativeStrengthIndex _rsi;
	private MoneyFlowIndex _mfi;
	private BollingerBands _bollinger1;
	private BollingerBands _bollinger2;

	public FlawlessVictoryStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_version = Param(nameof(Version), 1)
			.SetDisplay("Version", "Strategy version (1, 2, or 3)", "Strategy");

		// Version 2 parameters
		_v2StopLossPercent = Param(nameof(V2StopLossPercent), 6.604m)
			.SetDisplay("V2 Stop Loss %", "Stop loss for version 2", "Version 2");
		_v2TakeProfitPercent = Param(nameof(V2TakeProfitPercent), 2.328m)
			.SetDisplay("V2 Take Profit %", "Take profit for version 2", "Version 2");

		// Version 3 parameters
		_v3StopLossPercent = Param(nameof(V3StopLossPercent), 8.882m)
			.SetDisplay("V3 Stop Loss %", "Stop loss for version 3", "Version 3");
		_v3TakeProfitPercent = Param(nameof(V3TakeProfitPercent), 2.317m)
			.SetDisplay("V3 Take Profit %", "Take profit for version 3", "Version 3");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int Version
	{
		get => _version.Value;
		set => _version.Value = value;
	}

	public decimal V2StopLossPercent
	{
		get => _v2StopLossPercent.Value;
		set => _v2StopLossPercent.Value = value;
	}

	public decimal V2TakeProfitPercent
	{
		get => _v2TakeProfitPercent.Value;
		set => _v2TakeProfitPercent.Value = value;
	}

	public decimal V3StopLossPercent
	{
		get => _v3StopLossPercent.Value;
		set => _v3StopLossPercent.Value = value;
	}

	public decimal V3TakeProfitPercent
	{
		get => _v3TakeProfitPercent.Value;
		set => _v3TakeProfitPercent.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize indicators
		_rsi = new RelativeStrengthIndex { Length = 14 };
		_mfi = new MoneyFlowIndex { Length = 14 };
		
		// Version 1 and 3 use 20-period Bollinger
		_bollinger1 = new BollingerBands
		{
			Length = 20,
			Width = 1.0m
		};

		// Version 2 uses 17-period Bollinger
		_bollinger2 = new BollingerBands
		{
			Length = 17,
			Width = 1.0m
		};

		// Subscribe to candles using high-level API
		var subscription = SubscribeCandles(CandleType);

		// Bind indicators based on version
		if (Version == 3)
		{
			subscription
				.BindEx(_bollinger1, _rsi, _mfi, OnProcessWithMfi)
				.Start();
		}
		else if (Version == 2)
		{
			subscription
				.BindEx(_bollinger2, _rsi, OnProcessWithoutMfi)
				.Start();
		}
		else // Version 1
		{
			subscription
				.BindEx(_bollinger1, _rsi, OnProcessWithoutMfi)
				.Start();
		}

		// Setup chart
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			if (Version == 2)
				DrawIndicator(area, _bollinger2);
			else
				DrawIndicator(area, _bollinger1);
			DrawOwnTrades(area);
		}

		// Setup protection based on version
		if (Version == 2)
		{
			StartProtection(
				new Unit(V2TakeProfitPercent, UnitTypes.Percent),
				new Unit(V2StopLossPercent, UnitTypes.Percent)
			);
		}
		else if (Version == 3)
		{
			StartProtection(
				new Unit(V3TakeProfitPercent, UnitTypes.Percent),
				new Unit(V3StopLossPercent, UnitTypes.Percent)
			);
		}
	}

	private void OnProcessWithMfi(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue rsiValue, IIndicatorValue mfiValue)
	{
		ProcessCandle(candle, bollingerValue, rsiValue.ToDecimal(), mfiValue.ToDecimal());
	}

	private void OnProcessWithoutMfi(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue rsiValue)
	{
		ProcessCandle(candle, bollingerValue, rsiValue.ToDecimal(), 0m);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, decimal rsiValue, decimal mfiValue)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Wait for indicators to form
		if (!_rsi.IsFormed)
			return;

		if (Version == 3 && !_mfi.IsFormed)
			return;

		if ((Version == 2 && !_bollinger2.IsFormed) || (Version != 2 && !_bollinger1.IsFormed))
			return;

		// Get Bollinger Bands values
		var bollingerTyped = (BollingerBandsValue)bollingerValue;
		var upper = bollingerTyped.UpBand;
		var lower = bollingerTyped.LowBand;

		// Define strategy parameters based on version
		bool buySignal = false;
		bool sellSignal = false;

		if (Version == 1)
		{
			// Version 1 parameters
			var rsiLowerLevel = 42m;
			var rsiUpperLevel = 70m;

			var bbBuyTrigger = candle.ClosePrice < lower;
			var bbSellTrigger = candle.ClosePrice > upper;
			var rsiBuyGuard = rsiValue < rsiLowerLevel;
			var rsiSellGuard = rsiValue > rsiUpperLevel;

			buySignal = bbBuyTrigger && rsiBuyGuard;
			sellSignal = bbSellTrigger && rsiSellGuard;
		}
		else if (Version == 2)
		{
			// Version 2 parameters
			var rsiLowerLevel = 42m;
			var rsiUpperLevel = 76m;

			var bbBuyTrigger = candle.ClosePrice < lower;
			var bbSellTrigger = candle.ClosePrice > upper;
			var rsiBuyGuard = rsiValue < rsiLowerLevel;
			var rsiSellGuard = rsiValue > rsiUpperLevel;

			buySignal = bbBuyTrigger && rsiBuyGuard;
			sellSignal = bbSellTrigger && rsiSellGuard;
		}
		else if (Version == 3)
		{
			// Version 3 parameters
			var mfiLowerLevel = 60m;
			var rsiUpperLevel = 65m;
			var mfiUpperLevel = 64m;

			var bbBuyTrigger = candle.ClosePrice < lower;
			var bbSellTrigger = candle.ClosePrice > upper;
			var mfiBuyGuard = mfiValue < mfiLowerLevel;
			var rsiSellGuard = rsiValue > rsiUpperLevel;
			var mfiSellGuard = mfiValue > mfiUpperLevel;

			buySignal = bbBuyTrigger && mfiBuyGuard;
			sellSignal = bbSellTrigger && rsiSellGuard && mfiSellGuard;
		}

		// Execute trades
		if (buySignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (sellSignal && Position > 0)
		{
			ClosePosition();
		}
	}
}