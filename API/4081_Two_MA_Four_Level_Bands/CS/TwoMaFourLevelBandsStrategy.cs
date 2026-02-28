using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Two MA with four level bands: trades when fast MA crosses slow MA
/// or its offset bands. Exits on opposite crossover.
/// </summary>
public class TwoMaFourLevelBandsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _bandMultiplier;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _entryPrice;

	public TwoMaFourLevelBandsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_fastPeriod = Param(nameof(FastPeriod), 14)
			.SetDisplay("Fast MA", "Fast EMA period.", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 180)
			.SetDisplay("Slow MA", "Slow SMA period.", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR for band calculation.", "Indicators");

		_bandMultiplier = Param(nameof(BandMultiplier), 1.5m)
			.SetDisplay("Band Mult", "ATR multiplier for offset bands.", "Bands");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	public decimal BandMultiplier
	{
		get => _bandMultiplier.Value;
		set => _bandMultiplier.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevFast = 0;
		_prevSlow = 0;
		_entryPrice = 0;

		var fast = new ExponentialMovingAverage { Length = FastPeriod };
		var slow = new SimpleMovingAverage { Length = SlowPeriod };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fast, slow, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastVal, decimal slowVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevFast == 0 || _prevSlow == 0)
		{
			_prevFast = fastVal;
			_prevSlow = slowVal;
			return;
		}

		var bandOffset = atrVal * BandMultiplier;

		// Check crossovers at multiple band levels
		var bullish = false;
		var bearish = false;

		// Main line cross
		if (_prevFast <= _prevSlow && fastVal > slowVal) bullish = true;
		if (_prevFast >= _prevSlow && fastVal < slowVal) bearish = true;

		// Upper band cross
		if (_prevFast <= _prevSlow + bandOffset && fastVal > slowVal + bandOffset) bullish = true;
		if (_prevFast >= _prevSlow + bandOffset && fastVal < slowVal + bandOffset) bearish = true;

		// Lower band cross
		if (_prevFast <= _prevSlow - bandOffset && fastVal > slowVal - bandOffset) bullish = true;
		if (_prevFast >= _prevSlow - bandOffset && fastVal < slowVal - bandOffset) bearish = true;

		// Upper band 2
		if (_prevFast <= _prevSlow + bandOffset * 2 && fastVal > slowVal + bandOffset * 2) bullish = true;
		if (_prevFast >= _prevSlow + bandOffset * 2 && fastVal < slowVal + bandOffset * 2) bearish = true;

		// Lower band 2
		if (_prevFast <= _prevSlow - bandOffset * 2 && fastVal > slowVal - bandOffset * 2) bullish = true;
		if (_prevFast >= _prevSlow - bandOffset * 2 && fastVal < slowVal - bandOffset * 2) bearish = true;

		// Exit
		if (Position > 0 && bearish)
		{
			SellMarket();
			_entryPrice = 0;
		}
		else if (Position < 0 && bullish)
		{
			BuyMarket();
			_entryPrice = 0;
		}

		// Entry
		if (Position == 0)
		{
			if (bullish)
			{
				_entryPrice = candle.ClosePrice;
				BuyMarket();
			}
			else if (bearish)
			{
				_entryPrice = candle.ClosePrice;
				SellMarket();
			}
		}

		_prevFast = fastVal;
		_prevSlow = slowVal;
	}
}
