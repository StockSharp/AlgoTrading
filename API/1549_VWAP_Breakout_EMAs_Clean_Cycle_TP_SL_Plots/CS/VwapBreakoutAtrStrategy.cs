using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// VWAP breakout strategy with ATR based stop-loss and take-profit.
/// </summary>
public class VwapBreakoutAtrStrategy : Strategy
{
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _stopAtrMultiplier;
	private readonly StrategyParam<decimal> _takeAtrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private VWAP _vwap = null!;
	private AverageTrueRange _atr = null!;

	private decimal _prevClose;
	private decimal _prevVwap;
	private bool _hasPrev;
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	/// <summary>
	/// ATR multiplier for stop-loss.
	/// </summary>
	public decimal StopAtrMultiplier { get => _stopAtrMultiplier.Value; set => _stopAtrMultiplier.Value = value; }

	/// <summary>
	/// ATR multiplier for take-profit.
	/// </summary>
	public decimal TakeAtrMultiplier { get => _takeAtrMultiplier.Value; set => _takeAtrMultiplier.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public VwapBreakoutAtrStrategy()
	{
		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Parameters");

		_stopAtrMultiplier = Param(nameof(StopAtrMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Stop ATR Mult", "ATR multiplier for stop", "Parameters");

		_takeAtrMultiplier = Param(nameof(TakeAtrMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Take ATR Mult", "ATR multiplier for take profit", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");
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

		_prevClose = 0m;
		_prevVwap = 0m;
		_hasPrev = false;
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_vwap = new VWAP();
		_atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_vwap, _atr, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal vwap, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_vwap.IsFormed || !_atr.IsFormed)
			return;

		if (_hasPrev)
		{
			var crossOver = _prevClose <= _prevVwap && candle.ClosePrice > vwap;
			var crossUnder = _prevClose >= _prevVwap && candle.ClosePrice < vwap;

			if (crossOver && Position <= 0)
			{
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice - atrValue * StopAtrMultiplier;
				_takePrice = _entryPrice + atrValue * TakeAtrMultiplier;
				BuyMarket();
			}
			else if (crossUnder && Position >= 0)
			{
				_entryPrice = candle.ClosePrice;
				_stopPrice = _entryPrice + atrValue * StopAtrMultiplier;
				_takePrice = _entryPrice - atrValue * TakeAtrMultiplier;
				SellMarket();
			}
		}

		_prevClose = candle.ClosePrice;
		_prevVwap = vwap;
		_hasPrev = true;

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
				SellMarket();
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
				BuyMarket();
		}
	}
}

