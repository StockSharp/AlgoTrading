using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Larry Conners VIX Reversal II strategy based on VIX RSI.
/// </summary>
public class LarryConnersVixReversalIIStrategy : Strategy
{
	private readonly StrategyParam<Security> _vix;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _overbought;
	private readonly StrategyParam<int> _oversold;
	private readonly StrategyParam<int> _minHolding;
	private readonly StrategyParam<int> _maxHolding;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;

	private decimal _prevRsi;
	private int _holdingDays;

	/// <summary>
	/// VIX security used for RSI calculation.
	/// </summary>
	public Security Vix
	{
		get => _vix.Value;
		set => _vix.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public int OverboughtLevel
	{
		get => _overbought.Value;
		set => _overbought.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public int OversoldLevel
	{
		get => _oversold.Value;
		set => _oversold.Value = value;
	}

	/// <summary>
	/// Minimum holding days.
	/// </summary>
	public int MinHoldingDays
	{
		get => _minHolding.Value;
		set => _minHolding.Value = value;
	}

	/// <summary>
	/// Maximum holding days.
	/// </summary>
	public int MaxHoldingDays
	{
		get => _maxHolding.Value;
		set => _maxHolding.Value = value;
	}

	/// <summary>
	/// Candle type for VIX data.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public LarryConnersVixReversalIIStrategy()
	{
		_vix = Param<Security>(nameof(Vix), null)
			.SetDisplay("VIX Security", "VIX symbol", "Universe");

		_rsiPeriod = Param(nameof(RsiPeriod), 25)
			.SetDisplay("RSI Period", "RSI length", "Parameters")
			.SetCanOptimize(true);

		_overbought = Param(nameof(OverboughtLevel), 61)
			.SetDisplay("Overbought Level", "RSI overbought level", "Parameters")
			.SetCanOptimize(true);

		_oversold = Param(nameof(OversoldLevel), 42)
			.SetDisplay("Oversold Level", "RSI oversold level", "Parameters")
			.SetCanOptimize(true);

		_minHolding = Param(nameof(MinHoldingDays), 7)
			.SetDisplay("Min Holding Days", "Minimum holding period", "Risk Management")
			.SetCanOptimize(true);

		_maxHolding = Param(nameof(MaxHoldingDays), 12)
			.SetDisplay("Max Holding Days", "Maximum holding period", "Risk Management")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Data");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security == null || Vix == null)
			throw new InvalidOperationException("Securities must be set.");

		yield return (Security, CandleType);
		yield return (Vix, CandleType);
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		SubscribeCandles(CandleType, true, Security).Start();
		SubscribeCandles(CandleType, true, Vix)
			.Bind(_rsi, ProcessVix)
			.Start();
	}

	private void ProcessVix(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_rsi.IsFormed)
		{
			_prevRsi = rsiValue;
			return;
		}

		if (_holdingDays > 0)
		{
			_holdingDays++;

			if (_holdingDays >= MinHoldingDays && _holdingDays <= MaxHoldingDays)
			{
				ClosePosition();
				_holdingDays = 0;
			}
		}

		var crossOver = _prevRsi < OverboughtLevel && rsiValue >= OverboughtLevel;
		var crossUnder = _prevRsi > OversoldLevel && rsiValue <= OversoldLevel;

		if (_holdingDays == 0)
		{
			if (crossOver && Position <= 0)
			{
				BuyMarket();
				_holdingDays = 1;
			}
			else if (crossUnder && Position >= 0)
			{
				SellMarket();
				_holdingDays = 1;
			}
		}

		_prevRsi = rsiValue;
	}
}
