using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ChopFlow ATR Scalp strategy.
/// Inside the trading session, when the Choppiness Index is below ChopThreshold the market is trending: a flat position goes long
/// with OBV above its EMA and short with OBV below it. The exit is a symmetric stop and target placed AtrMultiplier ATRs from the entry.
/// </summary>
public class ChopFlowAtrScalpStrategy : Strategy
{
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _chopLength;
	private readonly StrategyParam<decimal> _chopThreshold;
	private readonly StrategyParam<int> _obvEmaLength;
	private readonly StrategyParam<string> _sessionInput;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Queue<(decimal tr, decimal high, decimal low)> _chopBars = new();
	private ExponentialMovingAverage _obvEma;
	private decimal? _prevClose;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ATR multiple for the stop and the target.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Choppiness Index period.
	/// </summary>
	public int ChopLength
	{
		get => _chopLength.Value;
		set => _chopLength.Value = value;
	}

	/// <summary>
	/// Choppiness level below which entries are allowed.
	/// </summary>
	public decimal ChopThreshold
	{
		get => _chopThreshold.Value;
		set => _chopThreshold.Value = value;
	}

	/// <summary>
	/// EMA period applied to OBV.
	/// </summary>
	public int ObvEmaLength
	{
		get => _obvEmaLength.Value;
		set => _obvEmaLength.Value = value;
	}

	/// <summary>
	/// Trading session as HHMM-HHMM in UTC.
	/// </summary>
	public string SessionInput
	{
		get => _sessionInput.Value;
		set => _sessionInput.Value = value;
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
	public ChopFlowAtrScalpStrategy()
	{
		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiple for the stop and the target", "Risk");

		_chopLength = Param(nameof(ChopLength), 14)
			.SetRange(2, 1000)
			.SetDisplay("Chop Length", "Choppiness Index period", "Indicators");

		_chopThreshold = Param(nameof(ChopThreshold), 60m)
			.SetDisplay("Chop Threshold", "Choppiness level below which entries are allowed", "Indicators");

		_obvEmaLength = Param(nameof(ObvEmaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("OBV EMA Length", "EMA period applied to OBV", "Indicators");

		_sessionInput = Param(nameof(SessionInput), "1700-1600")
			.SetDisplay("Session", "Trading session as HHMM-HHMM in UTC", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_obvEma = null;
	}

	private void ResetState()
	{
		_chopBars.Clear();
		_prevClose = null;
		_stopPrice = null;
		_takePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var atr = new AverageTrueRange { Length = AtrLength };
		var obv = new OnBalanceVolume();
		_obvEma = new ExponentialMovingAverage { Length = ObvEmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(atr, obv, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var volumeArea = CreateChartArea();
			if (volumeArea != null)
			{
				DrawIndicator(volumeArea, obv);
				DrawIndicator(volumeArea, _obvEma);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue, IIndicatorValue obvValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var chop = UpdateChoppiness(candle);

		if (!obvValue.IsFormed)
			return;

		var obv = obvValue.ToDecimal();
		var obvEmaValue = _obvEma.Process(obv, candle.OpenTime, true);

		if (Position > 0 && _stopPrice is decimal longStop && _takePrice is decimal longTake)
		{
			if (candle.LowPrice <= longStop || candle.HighPrice >= longTake)
			{
				SellMarket(Position);
				_stopPrice = null;
				_takePrice = null;
			}
			return;
		}

		if (Position < 0 && _stopPrice is decimal shortStop && _takePrice is decimal shortTake)
		{
			if (candle.HighPrice >= shortStop || candle.LowPrice <= shortTake)
			{
				BuyMarket(-Position);
				_stopPrice = null;
				_takePrice = null;
			}
			return;
		}

		if (Position != 0 || !atrValue.IsFormed || !obvEmaValue.IsFormed || chop is not decimal chopValue)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (chopValue >= ChopThreshold || !InSession(candle.OpenTime))
			return;

		var obvEma = obvEmaValue.ToDecimal();
		var distance = atrValue.ToDecimal() * AtrMultiplier;
		var close = candle.ClosePrice;

		if (obv > obvEma)
		{
			BuyMarket(Volume);
			_stopPrice = close - distance;
			_takePrice = close + distance;
		}
		else if (obv < obvEma)
		{
			SellMarket(Volume);
			_stopPrice = close + distance;
			_takePrice = close - distance;
		}
	}

	private decimal? UpdateChoppiness(ICandleMessage candle)
	{
		var tr = _prevClose is decimal prev
			? Math.Max(candle.HighPrice, prev) - Math.Min(candle.LowPrice, prev)
			: candle.HighPrice - candle.LowPrice;
		_prevClose = candle.ClosePrice;

		_chopBars.Enqueue((tr, candle.HighPrice, candle.LowPrice));
		while (_chopBars.Count > ChopLength)
			_chopBars.Dequeue();

		if (_chopBars.Count < ChopLength)
			return null;

		var range = _chopBars.Max(b => b.high) - _chopBars.Min(b => b.low);
		var sumTr = _chopBars.Sum(b => b.tr);

		if (range <= 0 || sumTr <= 0)
			return null;

		return 100m * (decimal)(Math.Log10((double)(sumTr / range)) / Math.Log10(ChopLength));
	}

	private bool InSession(DateTime time)
	{
		var parts = (SessionInput ?? string.Empty).Split('-');

		if (parts.Length != 2
			|| !int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var start)
			|| !int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var end))
			return true;

		var startMinutes = start / 100 * 60 + start % 100;
		var endMinutes = end / 100 * 60 + end % 100;
		var minutes = (int)time.TimeOfDay.TotalMinutes;

		if (startMinutes == endMinutes)
			return true;

		// A session whose start is after its end runs overnight.
		return startMinutes < endMinutes
			? minutes >= startMinutes && minutes < endMinutes
			: minutes >= startMinutes || minutes < endMinutes;
	}
}
