using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that builds ATR based trendlines from pivot points and trades their breakouts.
/// </summary>
public class AtrBasedTrendlinesStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _lookbackLength;
	private readonly StrategyParam<decimal> _atrPercent;
	private readonly StrategyParam<bool> _useWicks;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevHigh;
	private decimal _prevPrevHigh;
	private decimal _prevLow;
	private decimal _prevPrevLow;
	private decimal _lastPivotHigh;
	private decimal _lastPivotLow;
	private decimal _slopeHigh;
	private decimal _slopeLow;
	private int _barsSinceHigh;
	private int _barsSinceLow;
	private int _barIndex;
	private int _lastTradeBar;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Lookback length for pivot detection.
	/// </summary>
	public int LookbackLength
	{
		get => _lookbackLength.Value;
		set => _lookbackLength.Value = value;
	}

	/// <summary>
	/// ATR percentage used for line slope.
	/// </summary>
	public decimal AtrPercent
	{
		get => _atrPercent.Value;
		set => _atrPercent.Value = value;
	}

	/// <summary>
	/// Use candle wicks for pivots.
	/// </summary>
	public bool UseWicks
	{
		get => _useWicks.Value;
		set => _useWicks.Value = value;
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
	/// Constructor.
	/// </summary>
	public AtrBasedTrendlinesStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_lookbackLength = Param(nameof(LookbackLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Lookback length for pivots", "General");

		_atrPercent = Param(nameof(AtrPercent), 1m)
			.SetDisplay("ATR Percent", "ATR target percentage", "General");

		_useWicks = Param(nameof(UseWicks), true)
			.SetDisplay("Use Wicks", "Use candle wicks for pivots", "General");

		_cooldownBars = Param(nameof(CooldownBars), 350)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Trading");
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

		_prevHigh = _prevPrevHigh = _prevLow = _prevPrevLow = 0m;
		_lastPivotHigh = _lastPivotLow = 0m;
		_slopeHigh = _slopeLow = 0m;
		_barsSinceHigh = _barsSinceLow = 0;
		_barIndex = 0;
		_lastTradeBar = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var atr = new AverageTrueRange { Length = Math.Max(1, LookbackLength / 2) };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		var cooldownOk = _barIndex - _lastTradeBar > CooldownBars;

		var highSource = UseWicks ? candle.HighPrice : Math.Max(candle.ClosePrice, candle.OpenPrice);
		var lowSource = UseWicks ? candle.LowPrice : Math.Min(candle.ClosePrice, candle.OpenPrice);

		if (_prevPrevHigh != 0 && _prevHigh > _prevPrevHigh && _prevHigh > highSource)
		{
			_lastPivotHigh = _prevHigh;
			_slopeHigh = AtrPercent * LookbackLength / 200m * atrValue;
			_barsSinceHigh = 1;
		}
		else if (_barsSinceHigh > 0)
		{
			_barsSinceHigh++;
			var lineValue = _lastPivotHigh - _slopeHigh * _barsSinceHigh;
			if (candle.ClosePrice > lineValue && Position <= 0 && cooldownOk)
			{
				BuyMarket();
				_lastTradeBar = _barIndex;
			}
		}

		if (_prevPrevLow != 0 && _prevLow < _prevPrevLow && _prevLow < lowSource)
		{
			_lastPivotLow = _prevLow;
			_slopeLow = AtrPercent * LookbackLength / 200m * atrValue;
			_barsSinceLow = 1;
		}
		else if (_barsSinceLow > 0)
		{
			_barsSinceLow++;
			var lineValue = _lastPivotLow + _slopeLow * _barsSinceLow;
			if (candle.ClosePrice < lineValue && Position >= 0 && cooldownOk)
			{
				SellMarket();
				_lastTradeBar = _barIndex;
			}
		}

		_prevPrevHigh = _prevHigh;
		_prevHigh = highSource;
		_prevPrevLow = _prevLow;
		_prevLow = lowSource;
	}
}
