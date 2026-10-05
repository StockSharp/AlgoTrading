using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Donchian breakout strategy with trend, volatility and volume filters.
/// A close above the EntryLength channel of the previous candles goes long when the close is above the EMA, RSI is above 50,
/// ATR is above its AtrSmaLength average and volume is above its VolumeSmaLength average; a close below the channel goes short
/// with the mirrored trend filters. An opposite entry breakout reverses the position.
/// A long also exits on a close below the ExitLength channel or at an ATR stop of AtrMultiplier times ATR from the entry; shorts mirror this.
/// </summary>
public class DonchianBreakoutStrategy : Strategy
{
	private const int _rsiLength = 14;

	private readonly StrategyParam<int> _entryLength;
	private readonly StrategyParam<int> _exitLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _volumeSmaLength;
	private readonly StrategyParam<int> _atrSmaLength;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeSma;
	private SimpleMovingAverage _atrSma;
	private decimal? _prevEntryUpper;
	private decimal? _prevEntryLower;
	private decimal? _prevExitUpper;
	private decimal? _prevExitLower;
	private decimal? _stopPrice;

	/// <summary>
	/// Candles of the entry channel.
	/// </summary>
	public int EntryLength { get => _entryLength.Value; set => _entryLength.Value = value; }

	/// <summary>
	/// Candles of the exit channel.
	/// </summary>
	public int ExitLength { get => _exitLength.Value; set => _exitLength.Value = value; }

	/// <summary>
	/// ATR length.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	/// <summary>
	/// ATR multiplier of the stop.
	/// </summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

	/// <summary>
	/// Trend EMA length.
	/// </summary>
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }

	/// <summary>
	/// Volume average length.
	/// </summary>
	public int VolumeSmaLength { get => _volumeSmaLength.Value; set => _volumeSmaLength.Value = value; }

	/// <summary>
	/// ATR average length.
	/// </summary>
	public int AtrSmaLength { get => _atrSmaLength.Value; set => _atrSmaLength.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public DonchianBreakoutStrategy()
	{
		_entryLength = Param(nameof(EntryLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Entry Length", "Candles of the entry channel", "Donchian");

		_exitLength = Param(nameof(ExitLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Exit Length", "Candles of the exit channel", "Donchian");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR length", "Risk");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetNotNegative()
			.SetDisplay("ATR Multiplier", "ATR multiplier of the stop", "Risk");

		_emaLength = Param(nameof(EmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Trend EMA length", "Filters");

		_volumeSmaLength = Param(nameof(VolumeSmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume SMA Length", "Volume average length", "Filters");

		_atrSmaLength = Param(nameof(AtrSmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("ATR SMA Length", "ATR average length", "Filters");

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
		_prevEntryUpper = null;
		_prevEntryLower = null;
		_prevExitUpper = null;
		_prevExitLower = null;
		_stopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var entryChannel = new DonchianChannels { Length = EntryLength };
		var exitChannel = new DonchianChannels { Length = ExitLength };
		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var rsi = new RelativeStrengthIndex { Length = _rsiLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		_volumeSma = new SimpleMovingAverage { Length = VolumeSmaLength };
		_atrSma = new SimpleMovingAverage { Length = AtrSmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(entryChannel, exitChannel, ema, rsi, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, entryChannel);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue entryValue, IIndicatorValue exitValue, IIndicatorValue emaValue, IIndicatorValue rsiValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Channels are measured on the candles before this one.
		var entryUpper = _prevEntryUpper;
		var entryLower = _prevEntryLower;
		var exitUpper = _prevExitUpper;
		var exitLower = _prevExitLower;

		if (entryValue.IsFormed && entryValue is IDonchianChannelsValue { UpperBand: decimal eu, LowerBand: decimal el })
		{
			_prevEntryUpper = eu;
			_prevEntryLower = el;
		}

		if (exitValue.IsFormed && exitValue is IDonchianChannelsValue { UpperBand: decimal xu, LowerBand: decimal xl })
		{
			_prevExitUpper = xu;
			_prevExitLower = xl;
		}

		var volumeAvg = _volumeSma.Process(candle.TotalVolume, candle.ServerTime, true);

		if (!atrValue.IsFormed)
			return;

		var atr = atrValue.GetValue<decimal>();
		var atrAvg = _atrSma.Process(atr, candle.ServerTime, true);

		if (!emaValue.IsFormed || !rsiValue.IsFormed || !volumeAvg.IsFormed || !atrAvg.IsFormed)
			return;

		if (entryUpper is not decimal upper || entryLower is not decimal lower || exitUpper is not decimal exitHigh || exitLower is not decimal exitLow)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var ema = emaValue.GetValue<decimal>();
		var rsi = rsiValue.GetValue<decimal>();
		var filtersOk = atr > atrAvg.GetValue<decimal>() && candle.TotalVolume > volumeAvg.GetValue<decimal>();

		var longSignal = close > upper && close > ema && rsi > 50m && filtersOk;
		var shortSignal = close < lower && close < ema && rsi < 50m && filtersOk;

		if (longSignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = AtrMultiplier > 0m ? close - atr * AtrMultiplier : null;
		}
		else if (shortSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = AtrMultiplier > 0m ? close + atr * AtrMultiplier : null;
		}
		else if (Position > 0 && (close < exitLow || (_stopPrice is decimal longStop && candle.LowPrice <= longStop)))
		{
			SellMarket(Position);
			_stopPrice = null;
		}
		else if (Position < 0 && (close > exitHigh || (_stopPrice is decimal shortStop && candle.HighPrice >= shortStop)))
		{
			BuyMarket(-Position);
			_stopPrice = null;
		}
	}
}
