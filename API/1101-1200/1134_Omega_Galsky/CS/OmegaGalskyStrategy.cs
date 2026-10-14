using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Omega Galsky strategy.
/// A long opens when the fast EMA crosses above the middle EMA while the close is above the slow EMA, a short on the opposite cross
/// with the close below the slow EMA; an opposite signal reverses the position. The stop and the target are fractions of the entry
/// price (SlPercentage and TpPercentage). Once price moves FixedRiskReward times the initial risk in favour, the stop moves to break-even.
/// </summary>
public class OmegaGalskyStrategy : Strategy
{
	private readonly StrategyParam<int> _ema8Period;
	private readonly StrategyParam<int> _ema21Period;
	private readonly StrategyParam<int> _ema89Period;
	private readonly StrategyParam<decimal> _fixedRiskReward;
	private readonly StrategyParam<decimal> _slPercentage;
	private readonly StrategyParam<decimal> _tpPercentage;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevFast;
	private decimal? _prevMiddle;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private bool _breakEvenDone;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int Ema8Period
	{
		get => _ema8Period.Value;
		set => _ema8Period.Value = value;
	}

	/// <summary>
	/// Middle EMA period.
	/// </summary>
	public int Ema21Period
	{
		get => _ema21Period.Value;
		set => _ema21Period.Value = value;
	}

	/// <summary>
	/// Slow EMA period used as the price confirmation.
	/// </summary>
	public int Ema89Period
	{
		get => _ema89Period.Value;
		set => _ema89Period.Value = value;
	}

	/// <summary>
	/// Profit in multiples of the initial risk that moves the stop to break-even.
	/// </summary>
	public decimal FixedRiskReward
	{
		get => _fixedRiskReward.Value;
		set => _fixedRiskReward.Value = value;
	}

	/// <summary>
	/// Stop loss as a fraction of the entry price.
	/// </summary>
	public decimal SlPercentage
	{
		get => _slPercentage.Value;
		set => _slPercentage.Value = value;
	}

	/// <summary>
	/// Take profit as a fraction of the entry price.
	/// </summary>
	public decimal TpPercentage
	{
		get => _tpPercentage.Value;
		set => _tpPercentage.Value = value;
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
	public OmegaGalskyStrategy()
	{
		_ema8Period = Param(nameof(Ema8Period), 8)
			.SetGreaterThanZero()
			.SetDisplay("EMA 8 Period", "Fast EMA period", "Indicators");

		_ema21Period = Param(nameof(Ema21Period), 21)
			.SetGreaterThanZero()
			.SetDisplay("EMA 21 Period", "Middle EMA period", "Indicators");

		_ema89Period = Param(nameof(Ema89Period), 89)
			.SetGreaterThanZero()
			.SetDisplay("EMA 89 Period", "Slow EMA period used as price confirmation", "Indicators");

		_fixedRiskReward = Param(nameof(FixedRiskReward), 1.0m)
			.SetNotNegative()
			.SetDisplay("Break-Even R", "Profit in multiples of risk that moves the stop to break-even", "Risk");

		_slPercentage = Param(nameof(SlPercentage), 0.001m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop loss as a fraction of entry price", "Risk");

		_tpPercentage = Param(nameof(TpPercentage), 0.0025m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take profit as a fraction of entry price", "Risk");

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
		ResetState();
	}

	private void ResetState()
	{
		_prevFast = null;
		_prevMiddle = null;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_breakEvenDone = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var ema8 = new ExponentialMovingAverage { Length = Ema8Period };
		var ema21 = new ExponentialMovingAverage { Length = Ema21Period };
		var ema89 = new ExponentialMovingAverage { Length = Ema89Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema8, ema21, ema89, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema8);
			DrawIndicator(area, ema21);
			DrawIndicator(area, ema89);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal middle, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var prevFast = _prevFast;
		var prevMiddle = _prevMiddle;
		_prevFast = fast;
		_prevMiddle = middle;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (ManagePosition(candle))
			return;

		if (prevFast is not decimal pf || prevMiddle is not decimal pm)
			return;

		var close = candle.ClosePrice;

		if (pf <= pm && fast > middle && close > slow && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			SetLevels(close, true);
		}
		else if (pf >= pm && fast < middle && close < slow && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			SetLevels(close, false);
		}
	}

	private void SetLevels(decimal entry, bool isLong)
	{
		_entryPrice = entry;
		_breakEvenDone = false;

		var stopDistance = entry * SlPercentage;
		var takeDistance = entry * TpPercentage;

		_stopPrice = SlPercentage > 0 ? (isLong ? entry - stopDistance : entry + stopDistance) : 0m;
		_takePrice = TpPercentage > 0 ? (isLong ? entry + takeDistance : entry - takeDistance) : 0m;
	}

	// Returns true when the position was closed on this candle.
	private bool ManagePosition(ICandleMessage candle)
	{
		var risk = _entryPrice * SlPercentage;

		if (Position > 0)
		{
			if (_stopPrice > 0 && candle.LowPrice <= _stopPrice)
			{
				SellMarket(Position);
				return true;
			}

			if (_takePrice > 0 && candle.HighPrice >= _takePrice)
			{
				SellMarket(Position);
				return true;
			}

			if (!_breakEvenDone && risk > 0 && candle.HighPrice - _entryPrice >= risk * FixedRiskReward)
			{
				_stopPrice = _entryPrice;
				_breakEvenDone = true;
			}
		}
		else if (Position < 0)
		{
			if (_stopPrice > 0 && candle.HighPrice >= _stopPrice)
			{
				BuyMarket(-Position);
				return true;
			}

			if (_takePrice > 0 && candle.LowPrice <= _takePrice)
			{
				BuyMarket(-Position);
				return true;
			}

			if (!_breakEvenDone && risk > 0 && _entryPrice - candle.LowPrice >= risk * FixedRiskReward)
			{
				_stopPrice = _entryPrice;
				_breakEvenDone = true;
			}
		}

		return false;
	}
}
