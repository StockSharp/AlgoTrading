namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Gold scalping strategy based on Break of Structure (BOS) and Change of Character (CHoCH).
/// </summary>
public class GoldScalpingBosChochStrategy : Strategy
{
	private readonly StrategyParam<int> _recentLength;
	private readonly StrategyParam<int> _swingLength;
	private readonly StrategyParam<decimal> _takeProfitFactor;
	private readonly StrategyParam<DataType> _candleType;

	private Highest _recentHigh = null!;
	private Lowest _recentLow = null!;
	private Highest _swingHigh = null!;
	private Lowest _swingLow = null!;

	private decimal _prevSwingHigh;
	private decimal _prevSwingLow;
	private decimal _prevClose;
	private decimal _stopLoss;
	private decimal _takeProfit;

	/// <summary>
	/// Lookback period for recent support/resistance.
	/// </summary>
	public int RecentLength
	{
		get => _recentLength.Value;
		set => _recentLength.Value = value;
	}

	/// <summary>
	/// Lookback period for swing detection.
	/// </summary>
	public int SwingLength
	{
		get => _swingLength.Value;
		set => _swingLength.Value = value;
	}

	/// <summary>
	/// Take profit multiplier over stop distance.
	/// </summary>
	public decimal TakeProfitFactor
	{
		get => _takeProfitFactor.Value;
		set => _takeProfitFactor.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="GoldScalpingBosChochStrategy"/>.
	/// </summary>
	public GoldScalpingBosChochStrategy()
	{
		_recentLength = Param(nameof(RecentLength), 10)
			.SetDisplay("Recent length", "Support/resistance period", "Parameters");

		_swingLength = Param(nameof(SwingLength), 5)
			.SetDisplay("Swing length", "Lookback for swings", "Parameters");

		_takeProfitFactor = Param(nameof(TakeProfitFactor), 2m)
			.SetDisplay("TP factor", "Take profit factor", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy", "General");
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

		_recentHigh = null!;
		_recentLow = null!;
		_swingHigh = null!;
		_swingLow = null!;
		_prevSwingHigh = _prevSwingLow = _prevClose = 0m;
		_stopLoss = _takeProfit = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_recentHigh = new Highest { Length = RecentLength };
		_recentLow = new Lowest { Length = RecentLength };
		_swingHigh = new Highest { Length = SwingLength };
		_swingLow = new Lowest { Length = SwingLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_recentHigh, _recentLow, _swingHigh, _swingLow, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal recentHigh, decimal recentLow, decimal swingHigh, decimal swingLow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_recentHigh.IsFormed || !_recentLow.IsFormed || !_swingHigh.IsFormed || !_swingLow.IsFormed)
		{
			_prevSwingHigh = swingHigh;
			_prevSwingLow = swingLow;
			_prevClose = candle.ClosePrice;
			return;
		}

		var lastSwingHigh = _prevSwingHigh;
		var lastSwingLow = _prevSwingLow;

		var bosBullish = candle.HighPrice > lastSwingHigh;
		var bosBearish = candle.LowPrice < lastSwingLow;

		var chochBullish = bosBearish && _prevClose <= lastSwingLow && candle.ClosePrice > lastSwingLow;
		var chochBearish = bosBullish && _prevClose >= lastSwingHigh && candle.ClosePrice < lastSwingHigh;

		var buyCondition = bosBullish && chochBullish;
		var sellCondition = bosBearish && chochBearish;

		var validLongTrade = buyCondition && recentLow < candle.ClosePrice;
		var validShortTrade = sellCondition && recentHigh > candle.ClosePrice;

		if (Position <= 0 && validLongTrade)
		{
			_stopLoss = recentLow;
			_takeProfit = candle.ClosePrice + (candle.ClosePrice - _stopLoss) * TakeProfitFactor;
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (Position >= 0 && validShortTrade)
		{
			_stopLoss = recentHigh;
			_takeProfit = candle.ClosePrice - (_stopLoss - candle.ClosePrice) * TakeProfitFactor;
			SellMarket(Volume + Math.Abs(Position));
		}

		if (Position > 0 && (candle.LowPrice <= _stopLoss || candle.ClosePrice >= _takeProfit))
		{
			ClosePosition();
		}
		else if (Position < 0 && (candle.HighPrice >= _stopLoss || candle.ClosePrice <= _takeProfit))
		{
			ClosePosition();
		}

		_prevSwingHigh = swingHigh;
		_prevSwingLow = swingLow;
		_prevClose = candle.ClosePrice;
	}
}
