using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// BabyShark VWAP strategy.
/// A rolling VWAP of the typical price over Length candles carries bands two volume-weighted standard deviations away, and an
/// RSI of On-Balance Volume confirms extremes. A close below the lower band with OBV RSI below LowerLevel goes long and a close
/// above the upper band with OBV RSI above HigherLevel goes short. A long closes when price returns to the VWAP from below and
/// a short when it returns from above; a StopLossPercent stop protects both. After a position is closed no new entry is
/// taken for Cooldown candles.
/// </summary>
public class BabySharkVwapStrategy : Strategy
{
	private const decimal _bandDeviations = 2m;

	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _higherLevel;
	private readonly StrategyParam<decimal> _lowerLevel;
	private readonly StrategyParam<int> _cooldown;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<(decimal price, decimal volume)> _window = new();
	private RelativeStrengthIndex _obvRsi;
	private decimal _obv;
	private decimal? _prevClose;
	private int _barIndex;
	private int? _lastExitBar;
	private bool _wasInPosition;

	/// <summary>
	/// Rolling VWAP window.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// RSI period applied to OBV.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// OBV RSI level that confirms shorts.
	/// </summary>
	public decimal HigherLevel
	{
		get => _higherLevel.Value;
		set => _higherLevel.Value = value;
	}

	/// <summary>
	/// OBV RSI level that confirms longs.
	/// </summary>
	public decimal LowerLevel
	{
		get => _lowerLevel.Value;
		set => _lowerLevel.Value = value;
	}

	/// <summary>
	/// Candles to wait after a position closes.
	/// </summary>
	public int Cooldown
	{
		get => _cooldown.Value;
		set => _cooldown.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public BabySharkVwapStrategy()
	{
		_length = Param(nameof(Length), 60)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Rolling VWAP window", "VWAP");

		_rsiLength = Param(nameof(RsiLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period applied to OBV", "RSI");

		_higherLevel = Param(nameof(HigherLevel), 70m)
			.SetDisplay("Higher Level", "OBV RSI level that confirms shorts", "RSI");

		_lowerLevel = Param(nameof(LowerLevel), 30m)
			.SetDisplay("Lower Level", "OBV RSI level that confirms longs", "RSI");

		_cooldown = Param(nameof(Cooldown), 10)
			.SetNotNegative()
			.SetDisplay("Cooldown", "Candles to wait after a position closes", "Trading");

		_stopLossPercent = Param(nameof(StopLossPercent), 0.6m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_obvRsi = null;
		ResetState();
	}

	private void ResetState()
	{
		_window.Clear();
		_obv = 0m;
		_prevClose = null;
		_barIndex = 0;
		_lastExitBar = null;
		_wasInPosition = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		_obvRsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(new Unit(), StopLossPercent > 0m ? new Unit(StopLossPercent, UnitTypes.Percent) : new Unit(), useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		var close = candle.ClosePrice;
		var volume = candle.TotalVolume;

		if (_prevClose is decimal pc)
			_obv += close > pc ? volume : close < pc ? -volume : 0m;
		_prevClose = close;

		var obvRsiValue = _obvRsi.Process(_obv, candle.ServerTime, true);

		var typical = (candle.HighPrice + candle.LowPrice + close) / 3m;
		_window.Enqueue((typical, volume));
		if (_window.Count > Length)
			_window.Dequeue();

		// A position that has disappeared since the last candle was closed, by the stop or by an exit.
		if (_wasInPosition && Position == 0)
			_lastExitBar = _barIndex;
		_wasInPosition = Position != 0;

		if (_window.Count < Length || !_obvRsi.IsFormed || obvRsiValue.IsEmpty)
			return;

		var obvRsi = obvRsiValue.ToDecimal();

		var sumPv = 0m;
		var sumV = 0m;
		foreach (var (price, v) in _window)
		{
			sumPv += price * v;
			sumV += v;
		}

		if (sumV <= 0m)
			return;

		var vwap = sumPv / sumV;
		var sumSq = 0m;
		foreach (var (price, v) in _window)
			sumSq += v * (price - vwap) * (price - vwap);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var variance = sumSq / sumV;
		var deviation = (decimal)Math.Sqrt((double)variance);
		var upper = vwap + _bandDeviations * deviation;
		var lower = vwap - _bandDeviations * deviation;

		if (Position > 0)
		{
			if (close >= vwap)
			{
				SellMarket(Position);
				_lastExitBar = _barIndex;
			}

			return;
		}

		if (Position < 0)
		{
			if (close <= vwap)
			{
				BuyMarket(-Position);
				_lastExitBar = _barIndex;
			}

			return;
		}

		if (_lastExitBar is int last && _barIndex - last < Cooldown)
			return;

		if (close < lower && obvRsi < LowerLevel)
		{
			BuyMarket(Volume);
			_wasInPosition = true;
		}
		else if (close > upper && obvRsi > HigherLevel)
		{
			SellMarket(Volume);
			_wasInPosition = true;
		}
	}
}
