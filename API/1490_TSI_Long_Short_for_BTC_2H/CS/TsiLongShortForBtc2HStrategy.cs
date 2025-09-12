namespace StockSharp.Samples.Strategies;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// True Strength Index breakout strategy.
/// Opens long when TSI breaks above prior high and short when it breaks below prior low.
/// </summary>
public class TsiLongShortForBtc2HStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _lookback;

	private TrueStrengthIndex _tsi;
	private Highest _highest;
	private Lowest _lowest;
	private decimal _prevTsi;
	private decimal _prevHigh;
	private decimal _prevLow;
	private int _count;

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Long EMA length for TSI.
	/// </summary>
	public int LongLength
	{
		get => _longLength.Value;
		set => _longLength.Value = value;
	}

	/// <summary>
	/// Short EMA length for TSI.
	/// </summary>
	public int ShortLength
	{
		get => _shortLength.Value;
		set => _shortLength.Value = value;
	}

	/// <summary>
	/// Lookback period for highest/lowest.
	/// </summary>
	public int Lookback
	{
		get => _lookback.Value;
		set => _lookback.Value = value;
	}


	public TsiLongShortForBtc2HStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(2).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
		_longLength = Param(nameof(LongLength), 25)
		.SetDisplay("Long Length", "Long EMA for TSI", "Indicators");
		_shortLength = Param(nameof(ShortLength), 13)
		.SetDisplay("Short Length", "Short EMA for TSI", "Indicators");
		_lookback = Param(nameof(Lookback), 100)
		.SetDisplay("Lookback", "Bars for highs/lows", "Indicators");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_tsi = new TrueStrengthIndex { LongLength = LongLength, ShortLength = ShortLength };
		_highest = new Highest { Length = Lookback };
		_lowest = new Lowest { Length = Lookback };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _tsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var tsiVal = _tsi.Process(candle.ClosePrice, candle.ServerTime).ToDecimal();
		var high = _highest.Process(tsiVal, candle.ServerTime).ToDecimal();
		var low = _lowest.Process(tsiVal, candle.ServerTime).ToDecimal();

		if (_count < Lookback)
		{
			_count++;
			_prevTsi = tsiVal;
			_prevHigh = high;
			_prevLow = low;
			return;
		}

		if (IsFormedAndOnlineAndAllowTrading())
		{
			var longCon = _prevTsi <= _prevHigh && tsiVal > _prevHigh;
			var shortCon = _prevTsi >= _prevLow && tsiVal < _prevLow;

			if (longCon && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (shortCon && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}

		_prevTsi = tsiVal;
		_prevHigh = high;
		_prevLow = low;
	}
}