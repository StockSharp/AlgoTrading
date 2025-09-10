namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// CE ZLSMA 5MIN Candlechart Strategy.
/// </summary>
public class CeZlsma5MinCandlechartStrategy : Strategy
{
	private readonly StrategyParam<int> _zlsmaLength;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private LinearRegression _lsma1;
	private LinearRegression _lsma2;
	private AverageTrueRange _atr;
	private Highest _highestClose;
	private Lowest _lowestClose;

	private decimal _prevHaOpen;
	private decimal _prevHaClose;
	private decimal _longStopPrev;
	private decimal _shortStopPrev;
	private int _dir;

	public CeZlsma5MinCandlechartStrategy()
	{
		_zlsmaLength = Param(nameof(ZlsmaLength), 50)
			.SetDisplay("ZLSMA Length", "Length for ZLSMA calculation", "ZLSMA");

		_atrPeriod = Param(nameof(AtrPeriod), 1)
			.SetDisplay("ATR Period", "ATR period for Chandelier Exit", "Risk Management");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetDisplay("ATR Multiplier", "ATR multiplier for Chandelier Exit", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy calculation", "General");
	}

	public int ZlsmaLength
	{
		get => _zlsmaLength.Value;
		set => _zlsmaLength.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevHaOpen = 0;
		_prevHaClose = 0;
		_longStopPrev = 0;
		_shortStopPrev = 0;
		_dir = 1;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_lsma1 = new LinearRegression { Length = ZlsmaLength };
		_lsma2 = new LinearRegression { Length = ZlsmaLength };
		_atr = new AverageTrueRange { Length = AtrPeriod };
		_highestClose = new Highest { Length = AtrPeriod };
		_lowestClose = new Lowest { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (candle.State != CandleStates.Finished)
			return;

		decimal haOpen, haClose, haHigh, haLow;

		if (_prevHaOpen == 0)
		{
			haOpen = (candle.OpenPrice + candle.ClosePrice) / 2m;
			haClose = (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m;
			haHigh = candle.HighPrice;
			haLow = candle.LowPrice;
		}
		else
		{
			haOpen = (_prevHaOpen + _prevHaClose) / 2m;
			haClose = (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m;
			haHigh = Math.Max(Math.Max(candle.HighPrice, haOpen), haClose);
			haLow = Math.Min(Math.Min(candle.LowPrice, haOpen), haClose);
		}

		var lsmaValue = _lsma1.Process(haClose, candle.ServerTime, true).ToDecimal();
		var lsma2Value = _lsma2.Process(lsmaValue, candle.ServerTime, true).ToDecimal();
		var zlsma = lsmaValue + (lsmaValue - lsma2Value);

		var atrValue = _atr.Process(haHigh, haLow, haClose, candle.ServerTime, true).ToDecimal();
		var highestClose = _highestClose.Process(haClose, candle.ServerTime, true).ToDecimal();
		var lowestClose = _lowestClose.Process(haClose, candle.ServerTime, true).ToDecimal();

		var longStop = highestClose - atrValue * AtrMultiplier;
		if (_longStopPrev != 0m)
			longStop = _prevHaClose > _longStopPrev ? Math.Max(longStop, _longStopPrev) : longStop;

		var shortStop = lowestClose + atrValue * AtrMultiplier;
		if (_shortStopPrev != 0m)
			shortStop = _prevHaClose < _shortStopPrev ? Math.Min(shortStop, _shortStopPrev) : shortStop;

		var prevDir = _dir;
		_dir = haClose > _shortStopPrev ? 1 : haClose < _longStopPrev ? -1 : _dir;

		var buySignal = _dir == 1 && prevDir == -1 && haClose > zlsma && haClose > haOpen;

		if (buySignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (Position > 0 && haClose < zlsma)
			SellMarket(Math.Abs(Position));

		_prevHaOpen = haOpen;
		_prevHaClose = haClose;
		_longStopPrev = longStop;
		_shortStopPrev = shortStop;
	}
}

