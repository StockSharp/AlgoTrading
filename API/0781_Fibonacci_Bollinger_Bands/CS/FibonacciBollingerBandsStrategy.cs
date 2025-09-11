namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Fibonacci & Bollinger Bands Strategy.
/// </summary>
public class FibonacciBollingerBandsStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<int> _fibPeriod;

	private decimal? _fibHigh;
	private decimal? _fibLow;
	private decimal _prevClose;
	private decimal _prevHighest;
	private decimal _prevLowest;
	private bool _isInitialized;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int BollingerLength { get => _bollingerLength.Value; set => _bollingerLength.Value = value; }
	public decimal BollingerMultiplier { get => _bollingerMultiplier.Value; set => _bollingerMultiplier.Value = value; }
	public int FibPeriod { get => _fibPeriod.Value; set => _fibPeriod.Value = value; }

	public FibonacciBollingerBandsStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_bollingerLength = Param(nameof(BollingerLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Length", "Period of Bollinger Bands", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Multiplier", "Standard deviation multiplier", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1m, 4m, 0.5m);

		_fibPeriod = Param(nameof(FibPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Fibonacci Lookback", "Bars for Fibonacci levels", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 10);
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bollinger = new BollingerBands
		{
			Length = BollingerLength,
			Width = BollingerMultiplier
		};

		var highest = new Highest { Length = FibPeriod };
		var lowest = new Lowest { Length = FibPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(bollinger, highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawIndicator(area, highest);
			DrawIndicator(area, lowest);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_isInitialized)
		{
			_prevClose = candle.ClosePrice;
			_prevHighest = highest;
			_prevLowest = lowest;
			_isInitialized = true;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevClose = candle.ClosePrice;
			_prevHighest = highest;
			_prevLowest = lowest;
			return;
		}

		if (_prevClose <= _prevHighest && candle.ClosePrice > highest)
			_fibHigh = candle.HighPrice;

		if (_prevClose >= _prevLowest && candle.ClosePrice < lowest)
			_fibLow = candle.LowPrice;

		if (_fibHigh is decimal fibHigh && _fibLow is decimal fibLow)
		{
			var extension = fibLow - 0.618m * (fibHigh - fibLow);

			if (candle.ClosePrice < lower && candle.ClosePrice > extension && Position <= 0)
			{
				BuyMarket();
			}
			else if (Position > 0 && (candle.ClosePrice > upper || candle.ClosePrice >= fibHigh))
			{
				SellMarket();
			}
		}

		_prevClose = candle.ClosePrice;
		_prevHighest = highest;
		_prevLowest = lowest;
	}
}
