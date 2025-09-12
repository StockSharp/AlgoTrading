using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy using the first 15-minute range with volume filter and ATR trailing stop.
/// </summary>
public class BreakoutNiftyBnStrategy : Strategy
{
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _volumeMaLength;
	private readonly StrategyParam<DataType> _candleType;

	private SMA _volumeSma;
	private ATR _atr;

	private DateTime _currentDay;
	private decimal _firstRangeHigh;
	private decimal _firstRangeLow;
	private bool _tradeTaken;
	private decimal _trailSl;
	private decimal _targetPrice;

	private readonly TimeSpan _sessionStart = new(9, 15, 0);
	private readonly TimeSpan _rangeEnd = new(9, 30, 0);
	private readonly TimeSpan _closeTime = new(15, 0, 0);

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ATR multiplier for trailing stop.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Volume SMA period.
	/// </summary>
	public int VolumeMaLength
	{
		get => _volumeMaLength.Value;
		set => _volumeMaLength.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BreakoutNiftyBnStrategy"/> class.
	/// </summary>
	public BreakoutNiftyBnStrategy()
	{
		_atrLength = Param(nameof(AtrLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "General");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR stop multiplier", "General");

		_volumeMaLength = Param(nameof(VolumeMaLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Volume MA", "Volume SMA period", "General");

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
		_currentDay = default;
		_firstRangeHigh = 0m;
		_firstRangeLow = decimal.MaxValue;
		_tradeTaken = false;
		_trailSl = 0m;
		_targetPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_volumeSma = new SMA { Length = VolumeMaLength };
		_atr = new ATR { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_atr, ProcessCandle).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var day = candle.OpenTime.Date;
		if (day != _currentDay)
		{
			_currentDay = day;
			_firstRangeHigh = 0m;
			_firstRangeLow = decimal.MaxValue;
			_tradeTaken = false;
			_trailSl = 0m;
			_targetPrice = 0m;
		}

		var time = candle.OpenTime.TimeOfDay;

		if (time >= _sessionStart && time < _rangeEnd)
		{
			_firstRangeHigh = Math.Max(_firstRangeHigh, candle.HighPrice);
			_firstRangeLow = Math.Min(_firstRangeLow, candle.LowPrice);
			return;
		}

		var afterRange = time >= _rangeEnd;
		var targetRange = _firstRangeHigh - _firstRangeLow;

		var volumeMa = _volumeSma.Process(candle.TotalVolume, candle.OpenTime, true).ToDecimal();
		var volumeOk = candle.TotalVolume > volumeMa;

		var trailOffset = atr * AtrMultiplier;

		if (!_tradeTaken && targetRange > 0m)
		{
			var breakout = afterRange && candle.ClosePrice > _firstRangeHigh && volumeOk;
			var breakdown = afterRange && candle.ClosePrice < _firstRangeLow && volumeOk;

			if (breakout && Position <= 0)
			{
				var vol = Volume + Math.Abs(Position);
				BuyMarket(vol);
				_trailSl = candle.ClosePrice - trailOffset;
				_targetPrice = candle.ClosePrice + targetRange;
				_tradeTaken = true;
			}
			else if (breakdown && Position >= 0)
			{
				var vol = Volume + Math.Abs(Position);
				SellMarket(vol);
				_trailSl = candle.ClosePrice + trailOffset;
				_targetPrice = candle.ClosePrice - targetRange;
				_tradeTaken = true;
			}
		}

		if (Position > 0)
		{
			_trailSl = Math.Max(_trailSl, candle.ClosePrice - trailOffset);

			if (candle.ClosePrice <= _trailSl || candle.HighPrice >= _targetPrice || time >= _closeTime)
				SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			_trailSl = Math.Min(_trailSl, candle.ClosePrice + trailOffset);

			if (candle.ClosePrice >= _trailSl || candle.LowPrice <= _targetPrice || time >= _closeTime)
				BuyMarket(Math.Abs(Position));
		}
	}
}
