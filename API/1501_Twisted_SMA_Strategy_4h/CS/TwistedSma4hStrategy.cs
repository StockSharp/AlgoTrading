using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Twisted SMA strategy for 4h candles.
/// Opens long when three SMAs align bullish and price is above main SMA while KAMA is moving.
/// Exits when SMAs align bearish.
/// </summary>
public class TwistedSma4hStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _midLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _mainSmaLength;
	private readonly StrategyParam<int> _kamaLength;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _fastSma;
	private SimpleMovingAverage _midSma;
	private SimpleMovingAverage _slowSma;
	private SimpleMovingAverage _mainSma;
	private KAMA _kama;

	private decimal _prevKama;

	/// <summary>
	/// Fast SMA length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Middle SMA length.
	/// </summary>
	public int MidLength
	{
		get => _midLength.Value;
		set => _midLength.Value = value;
	}

	/// <summary>
	/// Slow SMA length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Main SMA length.
	/// </summary>
	public int MainSmaLength
	{
		get => _mainSmaLength.Value;
		set => _mainSmaLength.Value = value;
	}

	/// <summary>
	/// KAMA length.
	/// </summary>
	public int KamaLength
	{
		get => _kamaLength.Value;
		set => _kamaLength.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="TwistedSma4hStrategy"/> class.
	/// </summary>
	public TwistedSma4hStrategy()
	{
		_fastLength = Param(nameof(FastLength), 4)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA Length", "Length of the fastest SMA", "SMA")
			.SetCanOptimize(true)
			.SetOptimize(2, 10, 1);

		_midLength = Param(nameof(MidLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Middle SMA Length", "Length of the middle SMA", "SMA")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_slowLength = Param(nameof(SlowLength), 18)
			.SetGreaterThanZero()
			.SetDisplay("Slow SMA Length", "Length of the slow SMA", "SMA")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 1);

		_mainSmaLength = Param(nameof(MainSmaLength), 100)
			.SetGreaterThanZero()
			.SetDisplay("Main SMA Length", "Length of the main SMA", "SMA")
			.SetCanOptimize(true)
			.SetOptimize(50, 200, 10);

		_kamaLength = Param(nameof(KamaLength), 25)
			.SetGreaterThanZero()
			.SetDisplay("KAMA Length", "Length of KAMA", "KAMA")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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
		_prevKama = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastSma = new SimpleMovingAverage { Length = FastLength };
		_midSma = new SimpleMovingAverage { Length = MidLength };
		_slowSma = new SimpleMovingAverage { Length = SlowLength };
		_mainSma = new SimpleMovingAverage { Length = MainSmaLength };
		_kama = new KAMA { Length = KamaLength };
		_prevKama = 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastSma, _midSma, _slowSma, _mainSma, _kama, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastSma);
			DrawIndicator(area, _midSma);
			DrawIndicator(area, _slowSma);
			DrawIndicator(area, _mainSma);
			DrawIndicator(area, _kama);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal mid, decimal slow, decimal main, decimal kamaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastSma.IsFormed || !_midSma.IsFormed || !_slowSma.IsFormed || !_mainSma.IsFormed || !_kama.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var isFlat = _prevKama != 0m && Math.Abs(kamaValue - _prevKama) / _prevKama < 0.001m;

		var longCond = fast > mid && mid > slow && candle.ClosePrice > main && !isFlat;
		var shortCond = fast < mid && mid < slow;

		if (longCond && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (shortCond && Position > 0)
			SellMarket(Math.Abs(Position));

		_prevKama = kamaValue;
	}
}

