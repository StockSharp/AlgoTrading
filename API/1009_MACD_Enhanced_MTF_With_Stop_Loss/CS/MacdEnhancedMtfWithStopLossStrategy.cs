using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD crossover strategy with ATR-based stop loss.
/// </summary>
public class MacdEnhancedMtfWithStopLossStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _stopAtrMult;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _emaFast;
	private EMA _emaSlow;
	private AverageTrueRange _atr;
	private decimal _prevMacd;
	private bool _initialized;
	private decimal _stopPrice;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal StopAtrMult { get => _stopAtrMult.Value; set => _stopAtrMult.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MacdEnhancedMtfWithStopLossStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12).SetDisplay("Fast", "Fast EMA", "MACD");
		_slowLength = Param(nameof(SlowLength), 26).SetDisplay("Slow", "Slow EMA", "MACD");
		_atrLength = Param(nameof(AtrLength), 14).SetDisplay("ATR", "ATR period", "Risk");
		_stopAtrMult = Param(nameof(StopAtrMult), 2m).SetDisplay("SL Mult", "ATR stop mult", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_prevMacd = 0; _initialized = false; _stopPrice = 0;

		_emaFast = new EMA { Length = FastLength };
		_emaSlow = new EMA { Length = SlowLength };
		_atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_emaFast, _emaSlow, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _emaFast);
			DrawIndicator(area, _emaSlow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_emaFast.IsFormed || !_emaSlow.IsFormed || !_atr.IsFormed)
			return;

		var macd = fast - slow;

		if (!_initialized) { _prevMacd = macd; _initialized = true; return; }

		var crossUp = _prevMacd <= 0 && macd > 0;
		var crossDown = _prevMacd >= 0 && macd < 0;

		// Stop loss check
		if (Position > 0 && candle.ClosePrice <= _stopPrice)
		{
			SellMarket(Math.Abs(Position));
			_prevMacd = macd;
			return;
		}
		if (Position < 0 && candle.ClosePrice >= _stopPrice)
		{
			BuyMarket(Math.Abs(Position));
			_prevMacd = macd;
			return;
		}

		if (crossUp && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = candle.ClosePrice - atr * StopAtrMult;
		}
		else if (crossDown && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = candle.ClosePrice + atr * StopAtrMult;
		}

		_prevMacd = macd;
	}
}
