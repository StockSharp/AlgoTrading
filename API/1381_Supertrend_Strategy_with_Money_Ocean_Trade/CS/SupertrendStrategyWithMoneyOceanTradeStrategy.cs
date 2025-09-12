using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SuperTrend crossover strategy with trend change confirmation.
/// </summary>
public class SupertrendStrategyWithMoneyOceanTradeStrategy : Strategy
{
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<DataType> _candleType;

	private int _prevDir;
	private decimal _prevSuperTrend;
	private decimal _prevClose;

	/// <summary>
	/// ATR period for SuperTrend calculation.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Multiplier for SuperTrend calculation.
	/// </summary>
	public decimal Factor
	{
		get => _factor.Value;
		set => _factor.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public SupertrendStrategyWithMoneyOceanTradeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_atrLength = Param(nameof(AtrLength), 6)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period for SuperTrend", "SuperTrend");

		_factor = Param(nameof(Factor), 0.25m)
			.SetGreaterThanZero()
			.SetDisplay("Factor", "ATR multiplier for SuperTrend", "SuperTrend");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var st = new SuperTrend { Length = AtrLength, Multiplier = Factor };
		var sub = SubscribeCandles(CandleType);
		sub.BindEx(st, Process).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawIndicator(area, st);
			DrawOwnTrades(area);
		}
	}

	private void Process(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var st = (SuperTrendIndicatorValue)value;
		var dir = st.IsUpTrend ? 1 : -1;
		var stVal = st.Value;

		if (_prevDir != 0 && dir != _prevDir)
		{
			if (dir > _prevDir && _prevClose < _prevSuperTrend && candle.ClosePrice > stVal && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
			}
			else if (dir < _prevDir && _prevClose > _prevSuperTrend && candle.ClosePrice < stVal && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
			}
		}

		_prevDir = dir;
		_prevSuperTrend = stVal;
		_prevClose = candle.ClosePrice;
	}
}
