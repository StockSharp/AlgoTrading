using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Big body bar strategy with ATR trailing stop.
/// </summary>
public class IuBbbBigBodyBarStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _bigBodyThreshold;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrFactor;

	private decimal _sumBody;
	private int _bodyCount;
	private decimal? _atrStop;
	private decimal _entryPrice;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Big body threshold multiplier.
	/// </summary>
	public decimal BigBodyThreshold { get => _bigBodyThreshold.Value; set => _bigBodyThreshold.Value = value; }

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	/// <summary>
	/// ATR factor for trailing stop.
	/// </summary>
	public decimal AtrFactor { get => _atrFactor.Value; set => _atrFactor.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="IuBbbBigBodyBarStrategy"/>.
	/// </summary>
	public IuBbbBigBodyBarStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use.", "General");

		_bigBodyThreshold = Param(nameof(BigBodyThreshold), 1.5m)
			.SetDisplay("Big Body Threshold", "Multiplier of average body.", "Parameters");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Period", "ATR indicator period.", "Indicators");

		_atrFactor = Param(nameof(AtrFactor), 2m)
			.SetDisplay("ATR Factor", "ATR multiplier for trailing stop.", "Risk Management");
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
		_sumBody = 0m;
		_bodyCount = 0;
		_atrStop = null;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_sumBody = 0m;
		_bodyCount = 0;
		_atrStop = null;
		_entryPrice = 0m;

		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);

		// Track running average of body sizes
		_sumBody += body;
		_bodyCount++;
		var avgBody = _sumBody / _bodyCount;

		if (_bodyCount < 20 || avgBody <= 0m)
			return;

		var longCond = body > avgBody * BigBodyThreshold && candle.ClosePrice > candle.OpenPrice;
		var shortCond = body > avgBody * BigBodyThreshold && candle.ClosePrice < candle.OpenPrice;

		// Exit logic first
		if (Position > 0)
		{
			if (_atrStop is null)
				_atrStop = _entryPrice - atr * AtrFactor;
			else
				_atrStop = Math.Max(_atrStop.Value, candle.ClosePrice - atr * AtrFactor);

			if (candle.LowPrice <= _atrStop)
			{
				SellMarket();
				_atrStop = null;
			}
			return;
		}
		else if (Position < 0)
		{
			if (_atrStop is null)
				_atrStop = _entryPrice + atr * AtrFactor;
			else
				_atrStop = Math.Min(_atrStop.Value, candle.ClosePrice + atr * AtrFactor);

			if (candle.HighPrice >= _atrStop)
			{
				BuyMarket();
				_atrStop = null;
			}
			return;
		}

		// Entry logic
		if (longCond)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
		}
		else if (shortCond)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
		}
	}
}
