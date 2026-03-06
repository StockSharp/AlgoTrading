using System;
using System.Collections.Generic;

using Ecng.Common;

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
	private readonly StrategyParam<int> _maxEntries;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private decimal _prevRsi;
	private int _holdingDays;
	private int _entriesExecuted;
	private int _barsSinceSignal;

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
	/// Minimum holding bars.
	/// </summary>
	public int MinHoldingDays
	{
		get => _minHolding.Value;
		set => _minHolding.Value = value;
	}

	/// <summary>
	/// Maximum holding bars.
	/// </summary>
	public int MaxHoldingDays
	{
		get => _maxHolding.Value;
		set => _maxHolding.Value = value;
	}

	/// <summary>
	/// Maximum entries per run.
	/// </summary>
	public int MaxEntries
	{
		get => _maxEntries.Value;
		set => _maxEntries.Value = value;
	}

	/// <summary>
	/// Minimum bars between entries.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type for both instruments.
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
			.SetDisplay("RSI Period", "RSI length", "Parameters");

		_overbought = Param(nameof(OverboughtLevel), 61)
			.SetDisplay("Overbought Level", "RSI overbought level", "Parameters");

		_oversold = Param(nameof(OversoldLevel), 42)
			.SetDisplay("Oversold Level", "RSI oversold level", "Parameters");

		_minHolding = Param(nameof(MinHoldingDays), 7)
			.SetDisplay("Min Holding Days", "Minimum holding period", "Risk Management");

		_maxHolding = Param(nameof(MaxHoldingDays), 12)
			.SetDisplay("Max Holding Days", "Maximum holding period", "Risk Management");

		_maxEntries = Param(nameof(MaxEntries), 45)
			.SetDisplay("Max Entries", "Maximum entries per run", "Risk Management");

		_cooldownBars = Param(nameof(CooldownBars), 240)
			.SetDisplay("Cooldown Bars", "Minimum bars between entries", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
	protected override void OnReseted()
	{
		base.OnReseted();
		_rsi = null;
		_prevRsi = 0m;
		_holdingDays = 0;
		_entriesExecuted = 0;
		_barsSinceSignal = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_prevRsi = 0m;
		_holdingDays = 0;
		_entriesExecuted = 0;
		_barsSinceSignal = CooldownBars;

		SubscribeCandles(CandleType, true, Security).Start();
		SubscribeCandles(CandleType, true, Vix)
			.Bind(_rsi, ProcessVix)
			.Start();
	}

	private void ProcessVix(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barsSinceSignal++;

		if (!_rsi.IsFormed)
		{
			_prevRsi = rsiValue;
			return;
		}

		if (_holdingDays > 0)
		{
			_holdingDays++;

			if (_holdingDays >= MinHoldingDays && _holdingDays >= MaxHoldingDays)
			{
				if (Position > 0)
					SellMarket(Math.Abs(Position));
				else if (Position < 0)
					BuyMarket(Math.Abs(Position));

				_holdingDays = 0;
				_barsSinceSignal = 0;
			}
		}

		var crossOver = _prevRsi < OverboughtLevel && rsiValue >= OverboughtLevel;
		var crossUnder = _prevRsi > OversoldLevel && rsiValue <= OversoldLevel;

		if (_holdingDays == 0 && _entriesExecuted < MaxEntries && _barsSinceSignal >= CooldownBars)
		{
			if (crossOver && Position <= 0)
			{
				BuyMarket();
				_holdingDays = 1;
				_entriesExecuted++;
				_barsSinceSignal = 0;
			}
			else if (crossUnder && Position >= 0)
			{
				SellMarket();
				_holdingDays = 1;
				_entriesExecuted++;
				_barsSinceSignal = 0;
			}
		}

		_prevRsi = rsiValue;
	}
}
