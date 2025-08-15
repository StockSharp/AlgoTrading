using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Keltner Channels and Williams %R indicators
/// </summary>
public class KeltnerWilliamsRStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _keltnerMultiplier;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _williamsRPeriod;
	private readonly StrategyParam<DataType> _candleType;

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
	/// ATR period for Keltner Channel
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Williams %R period
	/// </summary>
	public int WilliamsRPeriod
	{
		get => _williamsRPeriod.Value;
		set => _williamsRPeriod.Value = value;
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
	public KeltnerWilliamsRStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetRange(10, 50)
			.SetDisplay("EMA Period", "EMA period for Keltner Channel", "Indicators")
			.SetCanOptimize(true);

		_keltnerMultiplier = Param(nameof(KeltnerMultiplier), 2m)
			.SetRange(1m, 4m)
			.SetDisplay("K Multiplier", "Multiplier for Keltner Channel", "Indicators")
			.SetCanOptimize(true);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetRange(7, 28)
			.SetDisplay("ATR Period", "ATR period for Keltner Channel", "Indicators")
			.SetCanOptimize(true);

		_williamsRPeriod = Param(nameof(WilliamsRPeriod), 14)
			.SetRange(5, 30)
			.SetDisplay("Williams %R Period", "Period for Williams %R indicator", "Indicators")
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
	protected override void OnReseted()
	{
		base.OnReseted();
	}

	/// <inheritdoc />
		protected override void OnStarted(DateTimeOffset time)
		{
				base.OnStarted(time);

		// Initialize indicators
		var keltner = new KeltnerChannels
		{
			Length = EmaPeriod,
			Multiplier = KeltnerMultiplier
		};

		var williamsR = new WilliamsR { Length = WilliamsRPeriod };

		// Create subscription and bind indicators
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(keltner, williamsR, ProcessIndicators)
			.Start();
		
		// Enable stop-loss protection based on ATR
		StartProtection(default, new Unit(2, UnitTypes.Absolute));

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, keltner);
			DrawIndicator(area, williamsR);
			DrawOwnTrades(area);
		}
	}

	private void ProcessIndicators(ICandleMessage candle, IIndicatorValue keltnerValue, IIndicatorValue williamsRValue)
	{
		// Skip unfinished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Check if strategy is ready to trade
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var keltnerTyped = (KeltnerChannelsValue)keltnerValue;
		var upper = keltnerTyped.Upper;
		var lower = keltnerTyped.Lower;
		var middle = keltnerTyped.Middle;

		var williamsR = williamsRValue.ToDecimal();

		var price = candle.ClosePrice;

		// Trading logic:
		// Long: Price < lower Keltner band && Williams %R < -80 (oversold at lower band)
		// Short: Price > upper Keltner band && Williams %R > -20 (overbought at upper band)
		
		if (price < lower && williamsR < -80 && Position <= 0)
		{
			// Buy signal
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (price > upper && williamsR > -20 && Position >= 0)
		{
			// Sell signal
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
		// Exit conditions
		else if (Position > 0 && price > middle)
		{
			// Exit long position when price returns to middle band
			SellMarket(Position);
		}
		else if (Position < 0 && price < middle)
		{
			// Exit short position when price returns to middle band
			BuyMarket(Math.Abs(Position));
		}
	}
}
