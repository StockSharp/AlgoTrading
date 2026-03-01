using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Aeron JJN breakout strategy.
/// Places stop orders at last opposite candle open after doji detection.
/// </summary>
public class AeronJjnStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _dojiDiff1;
	private readonly StrategyParam<decimal> _dojiDiff2;
	private readonly StrategyParam<bool> _trailSl;
	private readonly StrategyParam<int> _trailPips;
	private readonly StrategyParam<int> _resetTime;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevOpen;
	private decimal _prevClose;
	private bool _hasPrev;

	private decimal _lastBullishOpen;
	private decimal _lastBearishOpen;

	private decimal _buyStopLevel;
	private decimal _sellStopLevel;
	private bool _hasBuyStop;
	private bool _hasSellStop;
	private DateTimeOffset _buyStopTime;
	private DateTimeOffset _sellStopTime;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _targetPrice;
	private decimal _pendingAtr;
	private decimal _tickSize;

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// Body size threshold for previous candle.
	/// </summary>
	public decimal DojiDiff1 { get => _dojiDiff1.Value; set => _dojiDiff1.Value = value; }

	/// <summary>
	/// Body size threshold when searching for last opposite candle.
	/// </summary>
	public decimal DojiDiff2 { get => _dojiDiff2.Value; set => _dojiDiff2.Value = value; }

	/// <summary>
	/// Enable trailing stop.
	/// </summary>
	public bool TrailSl { get => _trailSl.Value; set => _trailSl.Value = value; }

	/// <summary>
	/// Trailing distance in pips.
	/// </summary>
	public int TrailPips { get => _trailPips.Value; set => _trailPips.Value = value; }

	/// <summary>
	/// Minutes before canceling stop orders.
	/// </summary>
	public int ResetTime { get => _resetTime.Value; set => _resetTime.Value = value; }

	/// <summary>
	/// Working candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="AeronJjnStrategy"/> class.
	/// </summary>
	public AeronJjnStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Indicator");

		_dojiDiff1 = Param(nameof(DojiDiff1), 0.001m)
			.SetDisplay("Doji Diff1", "Prev candle body threshold", "Signal");

		_dojiDiff2 = Param(nameof(DojiDiff2), 0.0004m)
			.SetDisplay("Doji Diff2", "Search body threshold", "Signal");

		_trailSl = Param(nameof(TrailSl), true)
			.SetDisplay("Use Trailing", "Enable trailing stop", "Risk");

		_trailPips = Param(nameof(TrailPips), 10)
			.SetDisplay("Trail Pips", "Trailing distance", "Risk");

		_resetTime = Param(nameof(ResetTime), 10)
			.SetDisplay("Reset Minutes", "Cancel unfilled stop after minutes", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Working timeframe", "General");
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
		_prevOpen = _prevClose = 0m;
		_hasPrev = false;
		_lastBullishOpen = _lastBearishOpen = 0m;
		_hasBuyStop = _hasSellStop = false;
		_buyStopLevel = _sellStopLevel = 0m;
		_entryPrice = _stopPrice = _targetPrice = _pendingAtr = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_tickSize = Security?.PriceStep ?? 0.0001m;

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(atr, ProcessCandle).Start();

		StartProtection(null, null);
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		CheckStopOrders(candle);
		ManagePosition(candle);
		CancelExpiredOrders(candle.OpenTime);

		if (_hasPrev)
			TryEnter(candle, atrValue);

		UpdateLastCandleInfo(candle);

		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
		_hasPrev = true;
	}

	private void CheckStopOrders(ICandleMessage candle)
	{
		if (_hasBuyStop && candle.HighPrice >= _buyStopLevel && Position <= 0)
		{
			BuyMarket();
			_entryPrice = _buyStopLevel;
			_stopPrice = _entryPrice - _pendingAtr;
			_targetPrice = _entryPrice + _pendingAtr;
			_pendingAtr = 0m;
			_hasBuyStop = false;
			_hasSellStop = false;
		}
		else if (_hasSellStop && candle.LowPrice <= _sellStopLevel && Position >= 0)
		{
			SellMarket();
			_entryPrice = _sellStopLevel;
			_stopPrice = _entryPrice + _pendingAtr;
			_targetPrice = _entryPrice - _pendingAtr;
			_pendingAtr = 0m;
			_hasSellStop = false;
			_hasBuyStop = false;
		}
	}

	private void ManagePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (TrailSl)
			{
				var trail = candle.ClosePrice - TrailPips * _tickSize;
				if (_stopPrice < trail)
					_stopPrice = trail;
			}

			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _targetPrice)
			{
				SellMarket();
				_entryPrice = _stopPrice = _targetPrice = 0m;
			}
		}
		else if (Position < 0)
		{
			if (TrailSl)
			{
				var trail = candle.ClosePrice + TrailPips * _tickSize;
				if (_stopPrice > trail)
					_stopPrice = trail;
			}

			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _targetPrice)
			{
				BuyMarket();
				_entryPrice = _stopPrice = _targetPrice = 0m;
			}
		}
		else
		{
			_entryPrice = _stopPrice = _targetPrice = 0m;
		}

		if (Position != 0)
		{
			_hasBuyStop = false;
			_hasSellStop = false;
		}
	}

	private void CancelExpiredOrders(DateTimeOffset time)
	{
		var life = TimeSpan.FromMinutes(ResetTime);

		if (_hasBuyStop && time - _buyStopTime >= life)
			_hasBuyStop = false;

		if (_hasSellStop && time - _sellStopTime >= life)
			_hasSellStop = false;
	}

	private void TryEnter(ICandleMessage candle, decimal atrValue)
	{
		var prevBullish = _prevClose > _prevOpen;
		var prevBearish = _prevClose < _prevOpen;
		var prevBody = Math.Abs(_prevClose - _prevOpen);

		if (candle.ClosePrice > candle.OpenPrice && prevBearish && prevBody > DojiDiff1)
		{
			if (candle.ClosePrice < _lastBearishOpen && Position <= 0 && !_hasBuyStop)
			{
				_buyStopLevel = _lastBearishOpen;
				_hasBuyStop = true;
				_buyStopTime = candle.OpenTime;
				_pendingAtr = atrValue;
			}
		}
		else if (candle.ClosePrice < candle.OpenPrice && prevBullish && prevBody > DojiDiff1)
		{
			if (candle.ClosePrice > _lastBullishOpen && Position >= 0 && !_hasSellStop)
			{
				_sellStopLevel = _lastBullishOpen;
				_hasSellStop = true;
				_sellStopTime = candle.OpenTime;
				_pendingAtr = atrValue;
			}
		}
	}

	private void UpdateLastCandleInfo(ICandleMessage candle)
	{
		var body = candle.ClosePrice - candle.OpenPrice;

		if (body > 0 && body > DojiDiff2)
			_lastBullishOpen = candle.OpenPrice;
		else if (body < 0 && -body > DojiDiff2)
			_lastBearishOpen = candle.OpenPrice;
	}
}
