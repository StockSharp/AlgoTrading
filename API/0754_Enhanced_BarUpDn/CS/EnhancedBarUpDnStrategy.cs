namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on Enhanced BarUpDn signals with Bollinger Bands filter.
/// </summary>
public class EnhancedBarUpDnStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplierSl;
	private readonly StrategyParam<decimal> _atrMultiplierTp;

	private BollingerBands _bollinger;
	private SimpleMovingAverage _trendMa;
	private AverageTrueRange _atr;

	private decimal _entryPrice;
	private decimal _prevClose;

	/// <summary>
	/// Type of candles for calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Bollinger Bands length.
	/// </summary>
	public int BbLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands multiplier.
	/// </summary>
	public decimal BbMultiplier
	{
		get => _bbMultiplier.Value;
		set => _bbMultiplier.Value = value;
	}

	/// <summary>
	/// Moving average length for trend filter.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// ATR calculation length.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop-loss.
	/// </summary>
	public decimal AtrMultiplierSl
	{
		get => _atrMultiplierSl.Value;
		set => _atrMultiplierSl.Value = value;
	}

	/// <summary>
	/// ATR multiplier for take-profit.
	/// </summary>
	public decimal AtrMultiplierTp
	{
		get => _atrMultiplierTp.Value;
		set => _atrMultiplierTp.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public EnhancedBarUpDnStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_bbLength = Param(nameof(BbLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_bbMultiplier = Param(nameof(BbMultiplier), 2m)
			.SetRange(0.1m, 10m)
			.SetDisplay("BB Multiplier", "Bollinger Bands multiplier", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1m, 4m, 0.5m);

		_maLength = Param(nameof(MaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Trend MA length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 10);

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR calculation length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 1);

		_atrMultiplierSl = Param(nameof(AtrMultiplierSl), 2m)
			.SetRange(0.1m, 10m)
			.SetDisplay("ATR SL Mult", "ATR multiplier for stop-loss", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 4m, 0.5m);

		_atrMultiplierTp = Param(nameof(AtrMultiplierTp), 3m)
			.SetRange(0.1m, 10m)
			.SetDisplay("ATR TP Mult", "ATR multiplier for take-profit", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 6m, 0.5m);
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

		_entryPrice = default;
		_prevClose = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_bollinger = new BollingerBands
		{
			Length = BbLength,
			Width = BbMultiplier
		};

		_trendMa = new SimpleMovingAverage
		{
			Length = MaLength
		};

		_atr = new AverageTrueRange
		{
			Length = AtrLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_bollinger, _trendMa, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
			DrawIndicator(area, _trendMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower, decimal trendValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_bollinger.IsFormed || !_trendMa.IsFormed || !_atr.IsFormed)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var open = candle.OpenPrice;
		var close = candle.ClosePrice;
		var high = candle.HighPrice;
		var low = candle.LowPrice;

		var isUptrend = close > trendValue;
		var isDowntrend = close < trendValue;

		var barUp = close > open && open > _prevClose && isUptrend && close > lower;
		var barDn = close < open && open < _prevClose && isDowntrend && close < upper;

		if (Position == 0)
		{
			if (barUp)
			{
				BuyMarket();
				_entryPrice = close;
			}
			else if (barDn)
			{
				SellMarket();
				_entryPrice = close;
			}
		}
		else if (Position > 0)
		{
			var stop = _entryPrice - atrValue * AtrMultiplierSl;
			var target = _entryPrice + atrValue * AtrMultiplierTp;

			if (low <= stop || high >= target)
			{
				SellMarket(Position);
				_entryPrice = default;
			}
		}
		else if (Position < 0)
		{
			var stop = _entryPrice + atrValue * AtrMultiplierSl;
			var target = _entryPrice - atrValue * AtrMultiplierTp;

			if (high >= stop || low <= target)
			{
				BuyMarket(Math.Abs(Position));
				_entryPrice = default;
			}
		}

		_prevClose = close;
	}
}
