using System;

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

	private EMA _ma;
	private decimal _prevClose;
	private decimal _prevMa;
	private bool _initialized;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }

	public MaMacdBbBackTesterStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
		_maLength = Param(nameof(MaLength), 20)
			.SetDisplay("MA Length", "MA period", "Indicators");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = 0;
		_prevMa = 0;
		_initialized = false;

		_ma = new EMA { Length = MaLength };

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

		var crossUp = _prevClose <= _prevMa && candle.ClosePrice > maVal;
		var crossDown = _prevClose >= _prevMa && candle.ClosePrice < maVal;

		if (crossUp && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (crossDown && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevClose = candle.ClosePrice;
		_prevMa = maVal;
	}
}
