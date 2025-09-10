using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy uses Bollinger Bands breakout with moving average trend filter and ATR-based exits.
/// </summary>
public class BbsrExtremeStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrStopMultiplier;
	private readonly StrategyParam<decimal> _atrProfitMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevLower;
	private decimal _prevUpper;
	private decimal _prevClose;
	private decimal _prevMa;
	private bool _isInitialized;
	private decimal _entryPrice;

	/// <summary>
	/// Period for Bollinger Bands calculation.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for Bollinger Bands.
	/// </summary>
	public decimal BollingerMultiplier
	{
		get => _bollingerMultiplier.Value;
		set => _bollingerMultiplier.Value = value;
	}

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop calculation.
	/// </summary>
	public decimal AtrStopMultiplier
	{
		get => _atrStopMultiplier.Value;
		set => _atrStopMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier for take profit.
	/// </summary>
	public decimal AtrProfitMultiplier
	{
		get => _atrProfitMultiplier.Value;
		set => _atrProfitMultiplier.Value = value;
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
	/// Initializes the strategy.
	/// </summary>
	public BbsrExtremeStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Bollinger Bands length", "Indicators");

		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Multiplier", "Standard deviation multiplier", "Indicators");

		_maLength = Param(nameof(MaLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Length for moving average", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR indicator period", "Risk Management");

		_atrStopMultiplier = Param(nameof(AtrStopMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Stop Multiplier", "ATR multiplier for stop", "Risk Management");

		_atrProfitMultiplier = Param(nameof(AtrProfitMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Profit Multiplier", "ATR multiplier for take profit", "Risk Management");

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
		_prevLower = default;
		_prevUpper = default;
		_prevClose = default;
		_prevMa = default;
		_isInitialized = false;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerMultiplier
		};

		var ma = new ExponentialMovingAverage
		{
			Length = MaLength
		};

		var atr = new AverageTrueRange
		{
			Length = AtrLength
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(bollinger, ma, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower, decimal maValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_isInitialized)
		{
			_prevLower = lower;
			_prevUpper = upper;
			_prevClose = candle.ClosePrice;
			_prevMa = maValue;
			_isInitialized = true;
			return;
		}

		var bull = _prevClose < _prevLower && candle.ClosePrice > lower && maValue > _prevMa;
		var bear = _prevClose > _prevUpper && candle.ClosePrice < upper && maValue < _prevMa;

		if (bull && Position <= 0)
		{
			CancelActiveOrders();
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_entryPrice = candle.ClosePrice;
		}
		else if (bear && Position >= 0)
		{
			CancelActiveOrders();
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_entryPrice = candle.ClosePrice;
		}

		var stop = AtrStopMultiplier * atrValue;
		var profit = AtrProfitMultiplier * atrValue;

		if (Position > 0)
		{
			var stopPrice = _entryPrice - stop;
			var profitPrice = _entryPrice + profit;

			if (candle.LowPrice <= stopPrice || candle.HighPrice >= profitPrice)
			{
				SellMarket(Position);
				_entryPrice = 0m;
			}
		}
		else if (Position < 0)
		{
			var stopPrice = _entryPrice + stop;
			var profitPrice = _entryPrice - profit;

			if (candle.HighPrice >= stopPrice || candle.LowPrice <= profitPrice)
			{
				BuyMarket(-Position);
				_entryPrice = 0m;
			}
		}

		_prevLower = lower;
		_prevUpper = upper;
		_prevClose = candle.ClosePrice;
		_prevMa = maValue;
	}
}
