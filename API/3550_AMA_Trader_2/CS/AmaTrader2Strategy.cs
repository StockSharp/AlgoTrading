namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Adaptive moving average strategy with RSI confirmation.
/// Simplified from the "AMA Trader 2" MetaTrader expert using EMA + RSI.
/// </summary>
public class AmaTrader2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _rsiLevelUp;
	private readonly StrategyParam<decimal> _rsiLevelDown;

	private RelativeStrengthIndex _rsi;
	private ExponentialMovingAverage _ema;
	private decimal? _prevPrice;
	private decimal? _prevEma;

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// EMA period.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal RsiLevelUp
	{
		get => _rsiLevelUp.Value;
		set => _rsiLevelUp.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiLevelDown
	{
		get => _rsiLevelDown.Value;
		set => _rsiLevelDown.Value = value;
	}

	public AmaTrader2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for calculations", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_emaLength = Param(nameof(EmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Adaptive MA period", "Indicators");

		_rsiLevelUp = Param(nameof(RsiLevelUp), 55m)
			.SetDisplay("RSI Up", "RSI bullish confirmation threshold", "Signals");

		_rsiLevelDown = Param(nameof(RsiLevelDown), 45m)
			.SetDisplay("RSI Down", "RSI bearish confirmation threshold", "Signals");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevPrice = null;
		_prevEma = null;

		_rsi = new RelativeStrengthIndex { Length = RsiLength };
		_ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, _ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_rsi.IsFormed || !_ema.IsFormed)
		{
			_prevPrice = candle.ClosePrice;
			_prevEma = emaValue;
			return;
		}

		if (_prevPrice is null || _prevEma is null)
		{
			_prevPrice = candle.ClosePrice;
			_prevEma = emaValue;
			return;
		}

		var volume = Volume;
		if (volume <= 0)
			volume = 1;

		// Price crosses above EMA + RSI confirms bullish
		var priceAboveEma = candle.ClosePrice > emaValue;
		var prevBelowEma = _prevPrice < _prevEma;

		if (priceAboveEma && prevBelowEma && rsiValue > RsiLevelUp)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));

			if (Position <= 0)
				BuyMarket(volume);
		}
		// Price crosses below EMA + RSI confirms bearish
		else if (!priceAboveEma && !prevBelowEma && rsiValue < RsiLevelDown)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));

			if (Position >= 0)
				SellMarket(volume);
		}

		_prevPrice = candle.ClosePrice;
		_prevEma = emaValue;
	}
}
