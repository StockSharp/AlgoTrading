using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

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
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;
	private decimal _prevWilliamsR;
	private int _cooldown;

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
	/// Bars to wait between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
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
			;

		_keltnerMultiplier = Param(nameof(KeltnerMultiplier), 2m)
			.SetRange(1m, 4m)
			.SetDisplay("K Multiplier", "Multiplier for Keltner Channel", "Indicators")
			;

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetRange(7, 28)
			.SetDisplay("ATR Period", "ATR period for Keltner Channel", "Indicators")
			;

		_williamsRPeriod = Param(nameof(WilliamsRPeriod), 14)
			.SetRange(5, 30)
			.SetDisplay("Williams %R Period", "Period for Williams %R indicator", "Indicators")
			;

		_cooldownBars = Param(nameof(CooldownBars), 40)
			.SetRange(1, 200)
			.SetDisplay("Cooldown Bars", "Bars between entries", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		_prevWilliamsR = 0m;
		_cooldown = 0;
	}

	/// <inheritdoc />
		protected override void OnStarted2(DateTime time)
		{
				base.OnStarted2(time);

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
		var crossedIntoOversold = _prevWilliamsR > -80m && williamsR <= -80m;
		var crossedIntoOverbought = _prevWilliamsR < -20m && williamsR >= -20m;
		_prevWilliamsR = williamsR;

		var price = candle.ClosePrice;
		if (_cooldown > 0)
			_cooldown--;

		// Trading logic:
		// Long: Price < lower Keltner band && Williams %R < -80 (oversold at lower band)
		// Short: Price > upper Keltner band && Williams %R > -20 (overbought at upper band)
		
		if (_cooldown == 0 && price <= lower * 1.001m && crossedIntoOversold && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_cooldown = CooldownBars;
		}
		else if (_cooldown == 0 && price >= upper * 0.999m && crossedIntoOverbought && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_cooldown = CooldownBars;
		}
	}
}
