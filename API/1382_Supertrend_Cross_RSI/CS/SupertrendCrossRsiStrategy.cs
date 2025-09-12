using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SuperTrend crossover with RSI-based exit and take profit.
/// </summary>
public class SupertrendCrossRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _superTrendType;
	private readonly StrategyParam<DataType> _rsiType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiUpper;
	private readonly StrategyParam<decimal> _rsiLower;
	private readonly StrategyParam<decimal> _takeProfitPercent;

	private decimal _superTrendValue;
	private decimal _prevSuperTrendValue;
	private decimal _prevClose;
	private bool _stReady;

	private decimal _rsiValue;
	private decimal _entryPrice;
	private decimal _takeProfitPrice;

	/// <summary>
	/// ATR period for SuperTrend calculation.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for SuperTrend.
	/// </summary>
	public decimal Factor
	{
		get => _factor.Value;
		set => _factor.Value = value;
	}

	/// <summary>
	/// Primary candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Timeframe for SuperTrend.
	/// </summary>
	public DataType SuperTrendType
	{
		get => _superTrendType.Value;
		set => _superTrendType.Value = value;
	}

	/// <summary>
	/// Timeframe for RSI.
	/// </summary>
	public DataType RsiType
	{
		get => _rsiType.Value;
		set => _rsiType.Value = value;
	}

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI upper threshold.
	/// </summary>
	public decimal RsiUpper
	{
		get => _rsiUpper.Value;
		set => _rsiUpper.Value = value;
	}

	/// <summary>
	/// RSI lower threshold.
	/// </summary>
	public decimal RsiLower
	{
		get => _rsiLower.Value;
		set => _rsiLower.Value = value;
	}

	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public SupertrendCrossRsiStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for SuperTrend", "SuperTrend");

		_factor = Param(nameof(Factor), 3.42m)
			.SetGreaterThanZero()
			.SetDisplay("Factor", "ATR multiplier for SuperTrend", "SuperTrend");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "Timeframes");

		_superTrendType = Param(nameof(SuperTrendType), TimeSpan.FromMinutes(120).TimeFrame())
			.SetDisplay("SuperTrend TF", "Timeframe for SuperTrend", "Timeframes");

		_rsiType = Param(nameof(RsiType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("RSI TF", "Timeframe for RSI", "Timeframes");

		_rsiLength = Param(nameof(RsiLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI calculation length", "RSI");

		_rsiUpper = Param(nameof(RsiUpper), 95m)
			.SetDisplay("RSI Upper", "Upper exit level", "RSI");

		_rsiLower = Param(nameof(RsiLower), 5m)
			.SetDisplay("RSI Lower", "Lower exit level", "RSI");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 30m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var st = new SuperTrend { Length = AtrPeriod, Multiplier = Factor };
		SubscribeCandles(SuperTrendType).BindEx(st, OnSuperTrend).Start();

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		SubscribeCandles(RsiType).Bind(rsi, OnRsi).Start();

		SubscribeCandles(CandleType).Bind(OnMain).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, SubscribeCandles(CandleType));
			DrawIndicator(area, st);
			DrawOwnTrades(area);
		}
	}

	private void OnSuperTrend(ICandleMessage candle, IIndicatorValue value)
	{
		if (!value.IsFinal)
			return;

		var st = (SuperTrendIndicatorValue)value;
		_superTrendValue = st.Value;
		_stReady = true;
	}

	private void OnRsi(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_rsiValue = rsi;
	}

	private void OnMain(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || !_stReady)
			return;

		var longCond = _prevClose < _prevSuperTrendValue && candle.ClosePrice > _superTrendValue;
		var shortCond = _prevClose > _prevSuperTrendValue && candle.ClosePrice < _superTrendValue;

		if (longCond && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			_entryPrice = candle.ClosePrice;
			_takeProfitPrice = _entryPrice * (1 + TakeProfitPercent / 100m);
			BuyMarket(volume);
		}
		else if (shortCond && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			_entryPrice = candle.ClosePrice;
			_takeProfitPrice = _entryPrice * (1 - TakeProfitPercent / 100m);
			SellMarket(volume);
		}

		if (Position > 0 && (_rsiValue >= RsiUpper || candle.HighPrice >= _takeProfitPrice))
		{
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0 && (_rsiValue <= RsiLower || candle.LowPrice <= _takeProfitPrice))
		{
			BuyMarket(Math.Abs(Position));
		}

		_prevClose = candle.ClosePrice;
		_prevSuperTrendValue = _superTrendValue;
	}
}
