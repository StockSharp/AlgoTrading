using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Countertrend strategy for SOL using Bollinger Bands and RSI.
/// Buys when price crosses above the lower band with low RSI and
/// sells when price crosses below the upper band with high RSI.
/// </summary>
public class BollingerRsiCountertrendSolStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerWidth;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _longRsi;
	private readonly StrategyParam<decimal> _shortRsi;
	private readonly StrategyParam<decimal> _shortProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevClose;
	private decimal _prevUpper;
	private decimal _prevLower;
	private decimal _prevBasis;
	private decimal _prevLow;
	private decimal? _longSlLevel;
	private decimal? _shortEntryPrice;

	/// <summary>
	/// Bollinger period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger width multiplier.
	/// </summary>
	public decimal BollingerWidth
	{
		get => _bollingerWidth.Value;
		set => _bollingerWidth.Value = value;
	}

	/// <summary>
	/// RSI period length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI threshold for long entries.
	/// </summary>
	public decimal LongRsi
	{
		get => _longRsi.Value;
		set => _longRsi.Value = value;
	}

	/// <summary>
	/// RSI threshold for short entries.
	/// </summary>
	public decimal ShortRsi
	{
		get => _shortRsi.Value;
		set => _shortRsi.Value = value;
	}

	/// <summary>
	/// Profit target for shorts in percent.
	/// </summary>
	public decimal ShortProfitPercent
	{
		get => _shortProfitPercent.Value;
		set => _shortProfitPercent.Value = value;
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
	/// Initializes <see cref="BollingerRsiCountertrendSolStrategy"/>.
	/// </summary>
	public BollingerRsiCountertrendSolStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Bollinger period", "Parameters");

		_bollingerWidth = Param(nameof(BollingerWidth), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Width", "Bollinger width", "Parameters");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Parameters");

		_longRsi = Param(nameof(LongRsi), 25m)
			.SetDisplay("Long RSI", "RSI threshold for longs", "Parameters");

		_shortRsi = Param(nameof(ShortRsi), 79m)
			.SetDisplay("Short RSI", "RSI threshold for shorts", "Parameters");

		_shortProfitPercent = Param(nameof(ShortProfitPercent), 3.5m)
			.SetDisplay("Short Profit %", "Short profit percent", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevClose = 0m;
		_prevUpper = 0m;
		_prevLower = 0m;
		_prevBasis = 0m;
		_prevLow = 0m;
		_longSlLevel = null;
		_shortEntryPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bollinger = new BollingerBands { Length = BollingerPeriod, Width = BollingerWidth };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(bollinger, rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var day = candle.OpenTime.DayOfWeek;
		var isWeekday = day >= DayOfWeek.Monday && day <= DayOfWeek.Friday;

		var longEntry = _prevClose < _prevLower && candle.ClosePrice > lower && rsiValue < LongRsi && isWeekday;
		var shortEntry = _prevClose > _prevUpper && candle.ClosePrice < upper && rsiValue > ShortRsi && isWeekday;

		var longTp = Position > 0 && _prevClose <= _prevUpper && candle.ClosePrice > upper;
		var shortTp1 = Position < 0 && _prevClose <= _prevBasis && candle.ClosePrice > middle;
		var shortTp2 = Position < 0 && _shortEntryPrice.HasValue &&
			(_shortEntryPrice.Value - candle.ClosePrice) / _shortEntryPrice.Value >= ShortProfitPercent / 100m;

		var longSl = Position > 0 && _longSlLevel.HasValue && candle.ClosePrice < _longSlLevel.Value;

		if (longEntry && Position <= 0)
		{
			_longSlLevel = Math.Min(candle.LowPrice, _prevLow);
			BuyMarket();
		}
		else if (shortEntry && Position >= 0)
		{
			_shortEntryPrice = candle.ClosePrice;
			SellMarket();
		}
		else if (Position > 0 && (longTp || longSl))
		{
			SellMarket();
		}
		else if (Position < 0 && (shortTp1 || shortTp2))
		{
			BuyMarket();
		}

		_prevClose = candle.ClosePrice;
		_prevUpper = upper;
		_prevLower = lower;
		_prevBasis = middle;
		_prevLow = candle.LowPrice;
	}
}
