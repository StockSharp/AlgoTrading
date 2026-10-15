using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Omar MMR strategy (long only).
/// Buys when the close is above EMA C, EMA A is above EMA B, the MACD line crosses above its signal line and RSI is
/// between 29 and 70. Positions are closed only by the percent take-profit and stop-loss.
/// </summary>
public class OmarMmrStrategy : Strategy
{
	private const decimal _rsiLower = 29m;
	private const decimal _rsiUpper = 70m;

	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _emaALength;
	private readonly StrategyParam<int> _emaBLength;
	private readonly StrategyParam<int> _emaCLength;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevMacd;
	private decimal? _prevSignal;

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// EMA A period.
	/// </summary>
	public int EmaALength
	{
		get => _emaALength.Value;
		set => _emaALength.Value = value;
	}

	/// <summary>
	/// EMA B period.
	/// </summary>
	public int EmaBLength
	{
		get => _emaBLength.Value;
		set => _emaBLength.Value = value;
	}

	/// <summary>
	/// EMA C period.
	/// </summary>
	public int EmaCLength
	{
		get => _emaCLength.Value;
		set => _emaCLength.Value = value;
	}

	/// <summary>
	/// MACD fast EMA period.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// MACD slow EMA period.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// MACD signal line period.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Take-profit percentage. 0 disables it.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage. 0 disables it.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
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
	/// Constructor.
	/// </summary>
	public OmarMmrStrategy()
	{
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "RSI");

		_emaALength = Param(nameof(EmaALength), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA A Length", "Fast EMA period", "Moving Averages");

		_emaBLength = Param(nameof(EmaBLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA B Length", "Medium EMA period", "Moving Averages");

		_emaCLength = Param(nameof(EmaCLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("EMA C Length", "Slow EMA period", "Moving Averages");

		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "MACD fast EMA period", "MACD");

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "MACD slow EMA period", "MACD");

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "MACD signal line period", "MACD");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 1.5m)
			.SetNotNegative()
			.SetDisplay("Take Profit %", "Take-profit percentage", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 2.0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevMacd = null;
		_prevSignal = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevMacd = null;
		_prevSignal = null;

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var emaA = new ExponentialMovingAverage { Length = EmaALength };
		var emaB = new ExponentialMovingAverage { Length = EmaBLength };
		var emaC = new ExponentialMovingAverage { Length = EmaCLength };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastLength },
				LongMa = { Length = MacdSlowLength },
			},
			SignalMa = { Length = MacdSignalLength },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(rsi, emaA, emaB, emaC, macd, ProcessCandle)
			.Start();

		StartProtection(
			TakeProfitPercent > 0 ? new Unit(TakeProfitPercent, UnitTypes.Percent) : new Unit(),
			StopLossPercent > 0 ? new Unit(StopLossPercent, UnitTypes.Percent) : new Unit(),
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, emaA);
			DrawIndicator(area, emaB);
			DrawIndicator(area, emaC);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, macd);
				DrawIndicator(oscillators, rsi);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rsiValue, IIndicatorValue emaAValue, IIndicatorValue emaBValue, IIndicatorValue emaCValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFormed)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
			return;

		var prevMacd = _prevMacd;
		var prevSignal = _prevSignal;
		_prevMacd = macd;
		_prevSignal = signal;

		if (!rsiValue.IsFormed || !emaAValue.IsFormed || !emaBValue.IsFormed || !emaCValue.IsFormed)
			return;

		if (prevMacd is not decimal lastMacd || prevSignal is not decimal lastSignal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var rsi = rsiValue.ToDecimal();
		var emaA = emaAValue.ToDecimal();
		var emaB = emaBValue.ToDecimal();
		var emaC = emaCValue.ToDecimal();

		var macdCrossUp = lastMacd <= lastSignal && macd > signal;

		if (Position == 0
			&& candle.ClosePrice > emaC
			&& emaA > emaB
			&& macdCrossUp
			&& rsi > _rsiLower && rsi < _rsiUpper)
		{
			BuyMarket(Volume);
		}
	}
}
