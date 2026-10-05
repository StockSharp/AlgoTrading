using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dual RSI Differential strategy.
/// The differential is RSI(LongRsiPeriod) minus RSI(ShortRsiPeriod). When it crosses below RsiDiffLevel the strategy goes long and
/// when it crosses above RsiDiffLevel it goes short, reversing an opposite position. With UseHoldDays a position is closed after
/// HoldDays days. Condition selects the sides that use the TakeProfitPerc and StopLossPerc exits.
/// </summary>
public class DualRsiDifferentialStrategy : Strategy
{
	/// <summary>
	/// Sides that use take profit and stop loss.
	/// </summary>
	public enum Conditions
	{
		/// <summary>
		/// No take profit or stop loss.
		/// </summary>
		None,

		/// <summary>
		/// Long positions only.
		/// </summary>
		Long,

		/// <summary>
		/// Short positions only.
		/// </summary>
		Short,

		/// <summary>
		/// Both sides.
		/// </summary>
		Both,
	}

	private readonly StrategyParam<int> _shortRsiPeriod;
	private readonly StrategyParam<int> _longRsiPeriod;
	private readonly StrategyParam<decimal> _rsiDiffLevel;
	private readonly StrategyParam<bool> _useHoldDays;
	private readonly StrategyParam<int> _holdDays;
	private readonly StrategyParam<Conditions> _condition;
	private readonly StrategyParam<decimal> _takeProfitPerc;
	private readonly StrategyParam<decimal> _stopLossPerc;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevDiff;
	private decimal _entryPrice;
	private DateTime _entryTime;

	/// <summary>
	/// Short RSI period.
	/// </summary>
	public int ShortRsiPeriod
	{
		get => _shortRsiPeriod.Value;
		set => _shortRsiPeriod.Value = value;
	}

	/// <summary>
	/// Long RSI period.
	/// </summary>
	public int LongRsiPeriod
	{
		get => _longRsiPeriod.Value;
		set => _longRsiPeriod.Value = value;
	}

	/// <summary>
	/// Threshold of the RSI differential.
	/// </summary>
	public decimal RsiDiffLevel
	{
		get => _rsiDiffLevel.Value;
		set => _rsiDiffLevel.Value = value;
	}

	/// <summary>
	/// Close positions after the holding period.
	/// </summary>
	public bool UseHoldDays
	{
		get => _useHoldDays.Value;
		set => _useHoldDays.Value = value;
	}

	/// <summary>
	/// Holding period in days.
	/// </summary>
	public int HoldDays
	{
		get => _holdDays.Value;
		set => _holdDays.Value = value;
	}

	/// <summary>
	/// Sides that use take profit and stop loss.
	/// </summary>
	public Conditions Condition
	{
		get => _condition.Value;
		set => _condition.Value = value;
	}

	/// <summary>
	/// Take profit percentage from entry price.
	/// </summary>
	public decimal TakeProfitPerc
	{
		get => _takeProfitPerc.Value;
		set => _takeProfitPerc.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPerc
	{
		get => _stopLossPerc.Value;
		set => _stopLossPerc.Value = value;
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
	public DualRsiDifferentialStrategy()
	{
		_shortRsiPeriod = Param(nameof(ShortRsiPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Short RSI", "Short RSI period", "Indicators");

		_longRsiPeriod = Param(nameof(LongRsiPeriod), 42)
			.SetGreaterThanZero()
			.SetDisplay("Long RSI", "Long RSI period", "Indicators");

		_rsiDiffLevel = Param(nameof(RsiDiffLevel), 5m)
			.SetDisplay("RSI Diff Level", "Threshold of the RSI differential", "Indicators");

		_useHoldDays = Param(nameof(UseHoldDays), true)
			.SetDisplay("Use Hold Days", "Close positions after the holding period", "Exit");

		_holdDays = Param(nameof(HoldDays), 5)
			.SetGreaterThanZero()
			.SetDisplay("Hold Days", "Holding period in days", "Exit");

		_condition = Param(nameof(Condition), Conditions.None)
			.SetDisplay("Condition", "Sides that use take profit and stop loss", "Risk");

		_takeProfitPerc = Param(nameof(TakeProfitPerc), 15m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take profit percentage from entry price", "Risk");

		_stopLossPerc = Param(nameof(StopLossPerc), 10m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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
		_prevDiff = null;
		_entryPrice = 0m;
		_entryTime = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var shortRsi = new RelativeStrengthIndex { Length = ShortRsiPeriod };
		var longRsi = new RelativeStrengthIndex { Length = LongRsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(shortRsi, longRsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, shortRsi);
				DrawIndicator(oscillators, longRsi);
			}
		}
	}

	private bool UsesRiskExits(bool isLong)
	{
		return Condition == Conditions.Both || (isLong ? Condition == Conditions.Long : Condition == Conditions.Short);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue shortValue, IIndicatorValue longValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!shortValue.IsFormed || !longValue.IsFormed)
			return;

		var diff = longValue.GetValue<decimal>() - shortValue.GetValue<decimal>();
		var prevDiff = _prevDiff;
		_prevDiff = diff;

		if (prevDiff is not decimal prev)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (Position != 0)
		{
			var isLong = Position > 0;
			var exit = UseHoldDays && candle.CloseTime - _entryTime >= TimeSpan.FromDays(HoldDays);

			if (!exit && UsesRiskExits(isLong) && _entryPrice > 0)
			{
				var take = TakeProfitPerc / 100m;
				var stop = StopLossPerc / 100m;

				if (isLong)
					exit = (take > 0 && candle.HighPrice >= _entryPrice * (1 + take)) || (stop > 0 && candle.LowPrice <= _entryPrice * (1 - stop));
				else
					exit = (take > 0 && candle.LowPrice <= _entryPrice * (1 - take)) || (stop > 0 && candle.HighPrice >= _entryPrice * (1 + stop));
			}

			if (exit)
			{
				if (isLong)
					SellMarket(Position);
				else
					BuyMarket(-Position);

				return;
			}
		}

		var longSignal = prev >= RsiDiffLevel && diff < RsiDiffLevel;
		var shortSignal = prev <= RsiDiffLevel && diff > RsiDiffLevel;

		if (longSignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryPrice = close;
			_entryTime = candle.CloseTime;
		}
		else if (shortSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_entryPrice = close;
			_entryTime = candle.CloseTime;
		}
	}
}
