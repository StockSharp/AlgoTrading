using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Double SMA crossover strategy with ATR-based stop-loss.
/// </summary>
public class AtrStopLossDoubleSmaStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _fastSma;
	private SimpleMovingAverage _slowSma;
	private AverageTrueRange _atr;

	private bool _isInitialized;
	private bool _wasFastLessThanSlow;
	private decimal _entryPrice;
	private decimal _atrStopDistance;
	private bool _isLong;

	/// <summary>
	/// Fast SMA period.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow SMA period.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Use ATR-based stop-loss.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
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
	/// ATR multiple for stop distance.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public AtrStopLossDoubleSmaStrategy()
	{
		_fastLength = Param(nameof(FastLength), 15)
		.SetGreaterThanZero()
		.SetDisplay("Fast SMA", "Period of the fast SMA", "Moving Average")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 5);

		_slowLength = Param(nameof(SlowLength), 45)
		.SetGreaterThanZero()
		.SetDisplay("Slow SMA", "Period of the slow SMA", "Moving Average")
		.SetCanOptimize(true)
		.SetOptimize(30, 90, 5);

		_useStopLoss = Param(nameof(UseStopLoss), true)
		.SetDisplay("Use Stop Loss", "Enable ATR-based stop loss", "Risk Management");

		_atrLength = Param(nameof(AtrLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Length", "ATR calculation length", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(10, 30, 5);

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Multiplier", "ATR multiple for stop distance", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 0.5m);

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
		_isInitialized = false;
		_wasFastLessThanSlow = false;
		_entryPrice = 0;
		_atrStopDistance = 0;
		_isLong = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastSma = new SimpleMovingAverage { Length = FastLength };
		_slowSma = new SimpleMovingAverage { Length = SlowLength };
		_atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_fastSma, _slowSma, _atr, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastSma);
			DrawIndicator(area, _slowSma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (!_isInitialized)
		{
			if (_fastSma.IsFormed && _slowSma.IsFormed && _atr.IsFormed)
			{
				_wasFastLessThanSlow = fastValue < slowValue;
				_isInitialized = true;
			}
			return;
		}

		var isFastLessThanSlow = fastValue < slowValue;

		if (_wasFastLessThanSlow != isFastLessThanSlow)
		{
			if (!isFastLessThanSlow)
			{
				if (Position <= 0)
				{
					_entryPrice = candle.ClosePrice;
					_atrStopDistance = atrValue * AtrMultiplier;
					_isLong = true;
					BuyMarket();
				}
			}
			else
			{
				if (Position >= 0)
				{
					_entryPrice = candle.ClosePrice;
					_atrStopDistance = atrValue * AtrMultiplier;
					_isLong = false;
					SellMarket();
				}
			}

			_wasFastLessThanSlow = isFastLessThanSlow;
		}

		if (!UseStopLoss || _entryPrice == 0)
		return;

		if (_isLong && Position > 0)
		{
			var stopPrice = _entryPrice - _atrStopDistance;
			if (candle.LowPrice <= stopPrice)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (!_isLong && Position < 0)
		{
			var stopPrice = _entryPrice + _atrStopDistance;
			if (candle.HighPrice >= stopPrice)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}
	}
}
