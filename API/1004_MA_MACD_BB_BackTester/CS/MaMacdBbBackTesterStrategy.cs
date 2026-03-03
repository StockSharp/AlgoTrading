using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using MA crossover with price for entry/exit.
/// </summary>
public class MaMacdBbBackTesterStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maLength;

	private ExponentialMovingAverage _ma;
	private decimal _prevClose;
	private decimal _prevMa;
	private bool _initialized;
	private int _cooldown;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }

	public MaMacdBbBackTesterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
		_maLength = Param(nameof(MaLength), 20)
			.SetDisplay("MA Length", "MA period", "Indicators");
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

		_prevClose = default;
		_prevMa = default;
		_initialized = false;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ma = new ExponentialMovingAverage { Length = MaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ma.IsFormed)
			return;

		if (!_initialized)
		{
			_prevClose = candle.ClosePrice;
			_prevMa = maVal;
			_initialized = true;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevClose = candle.ClosePrice;
			_prevMa = maVal;
			return;
		}

		var crossUp = _prevClose <= _prevMa && candle.ClosePrice > maVal;
		var crossDown = _prevClose >= _prevMa && candle.ClosePrice < maVal;

		if (crossUp && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
			_cooldown = 10;
		}
		else if (crossDown && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
			_cooldown = 10;
		}

		_prevClose = candle.ClosePrice;
		_prevMa = maVal;
	}
}
