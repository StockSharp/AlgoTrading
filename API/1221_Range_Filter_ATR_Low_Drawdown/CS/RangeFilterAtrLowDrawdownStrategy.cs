using System;

using Ecng.Common;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Range Filter strategy with ATR-based stop loss and take profit aiming for low drawdown.
/// </summary>
public class RangeFilterAtrLowDrawdownStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplierSl;
	private readonly StrategyParam<decimal> _atrMultiplierTp;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _avrng = new();
	private ExponentialMovingAverage _smooth = new();
	private AverageTrueRange _atr;

	private decimal _prevPrice;
	private decimal _filter;
	private int _trend;
	private bool _isFirst = true;
	private decimal _stopLoss;
	private decimal _takeProfit;

	/// <summary>
	/// Sampling period for range calculation.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Range multiplier.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
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
	/// ATR multiplier for stop loss.
	/// </summary>
	public decimal AtrMultiplierSl
	{
		get => _atrMultiplierSl.Value;
		set => _atrMultiplierSl.Value = value;
	}

	/// <summary>
	/// ATR multiplier for take profit.
	/// </summary>
	public decimal AtrMultiplierTp
	{
		get => _atrMultiplierTp.Value;
		set => _atrMultiplierTp.Value = value;
	}

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public RangeFilterAtrLowDrawdownStrategy()
	{
		_period = Param(nameof(Period), 14)
			.SetGreaterThanZero()
			.SetDisplay("Sampling Period", "Range filter sampling period", "Range Filter")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 5);

		_multiplier = Param(nameof(Multiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Range Multiplier", "Range multiplier", "Range Filter")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR calculation period", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 7);

		_atrMultiplierSl = Param(nameof(AtrMultiplierSl), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR SL Mult", "ATR multiplier for stop loss", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_atrMultiplierTp = Param(nameof(AtrMultiplierTp), 3m)
			.SetGreaterThanZero()
			.SetDisplay("ATR TP Mult", "ATR multiplier for take profit", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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

		_prevPrice = 0m;
		_filter = 0m;
		_trend = 0;
		_isFirst = true;
		_stopLoss = 0m;
		_takeProfit = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_atr = new AverageTrueRange { Length = AtrLength };
		_avrng.Length = Period;
		_smooth.Length = Period;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
			DrawCandles(area, subscription);
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var price = candle.ClosePrice;

		var avrng = _avrng.Process(Math.Abs(price - _prevPrice)).ToDecimal();
		var smooth = _smooth.Process(avrng).ToDecimal();
		var smrng = smooth * Multiplier;

		var prevFilt = _filter;

		if (_isFirst)
		{
			_filter = price;
			_isFirst = false;
		}
		else
		{
			if (price > prevFilt)
				_filter = Math.Max(prevFilt, price - smrng);
			else
				_filter = Math.Min(prevFilt, price + smrng);
		}

		if (_filter > prevFilt)
			_trend = 1;
		else if (_filter < prevFilt)
			_trend = -1;

		var crossedUp = _prevPrice <= prevFilt && price > _filter && _trend > 0;
		var crossedDown = _prevPrice >= prevFilt && price < _filter && _trend < 0;

		if (crossedUp && Position <= 0)
		{
			_stopLoss = price - atr * AtrMultiplierSl;
			_takeProfit = price + atr * AtrMultiplierTp;
			BuyMarket(Volume);
		}
		else if (crossedDown && Position >= 0)
		{
			_stopLoss = price + atr * AtrMultiplierSl;
			_takeProfit = price - atr * AtrMultiplierTp;
			SellMarket(Volume);
		}

		if (Position > 0)
		{
			if (price <= _stopLoss || price >= _takeProfit)
				SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			if (price >= _stopLoss || price <= _takeProfit)
				BuyMarket(Math.Abs(Position));
		}

		_prevPrice = price;
	}
}
