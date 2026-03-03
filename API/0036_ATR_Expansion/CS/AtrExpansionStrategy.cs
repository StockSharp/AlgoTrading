using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades on volatility expansion as measured by ATR.
/// Enters when ATR expands above threshold and price is above/below MA,
/// exits when volatility contracts.
/// </summary>
public class AtrExpansionStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _atrExpansionRatio;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<int> _lookback;

	private decimal _prevAtr;
	private bool _hasPrev;
	private int _cooldown;

	/// <summary>
	/// Period for ATR calculation.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Period for Moving Average calculation.
	/// </summary>
	public int MAPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Ratio of current ATR to previous ATR needed for expansion signal.
	/// </summary>
	public decimal AtrExpansionRatio
	{
		get => _atrExpansionRatio.Value;
		set => _atrExpansionRatio.Value = value;
	}

	/// <summary>
	/// Type of candles used for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Lookback period for ATR comparison.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}

	/// <summary>
	/// Initialize the ATR Expansion strategy.
	/// </summary>
	public AtrExpansionStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
			.SetOptimize(7, 21, 7);

		_maPeriod = Param(nameof(MAPeriod), 20)
			.SetDisplay("MA Period", "Period for MA calculation", "Indicators")
			.SetOptimize(10, 50, 5);

		_atrExpansionRatio = Param(nameof(AtrExpansionRatio), 1.05m)
			.SetDisplay("Expansion Ratio", "ATR expansion ratio for entry signal", "Entry")
			.SetOptimize(1.1m, 2.0m, 0.1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cooldownBars = Param(nameof(CooldownBars), 500)
			.SetRange(1, 100)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "General");

		_lookback = Param(nameof(Lookback), 5)
			.SetRange(1, 50)
			.SetDisplay("Lookback", "Bars to look back for ATR comparison", "General");
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
		_prevAtr = default;
		_hasPrev = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevAtr = 0;
		_hasPrev = false;
		_cooldown = 0;

		var atr = new AverageTrueRange { Length = AtrPeriod };
		var sma = new SimpleMovingAverage { Length = MAPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_hasPrev)
		{
			_prevAtr = atrValue;
			_hasPrev = true;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevAtr = atrValue;
			return;
		}

		var isExpanding = _prevAtr > 0 && atrValue / _prevAtr >= AtrExpansionRatio;
		var isContracting = _prevAtr > 0 && atrValue / _prevAtr < 1m / AtrExpansionRatio;

		if (Position == 0 && isExpanding)
		{
			// ATR expanding - enter in direction of price vs MA
			if (candle.ClosePrice > smaValue)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (candle.ClosePrice < smaValue)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0 && isContracting)
		{
			// Volatility contracting - exit long
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && isContracting)
		{
			// Volatility contracting - exit short
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevAtr = atrValue;
	}
}
