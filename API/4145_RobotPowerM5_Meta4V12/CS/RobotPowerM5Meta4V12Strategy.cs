using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RobotPowerM5: RSI momentum with EMA filter and ATR stops.
/// </summary>
public class RobotPowerM5Meta4V12Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _atrLength;

	private decimal _prevRsi;
	private decimal _entryPrice;
	private int _cooldown;

	public RobotPowerM5Meta4V12Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe.", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetDisplay("RSI Length", "RSI period.", "Indicators");

		_emaLength = Param(nameof(EmaLength), 50)
			.SetDisplay("EMA Length", "Trend filter.", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period.", "Indicators");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevRsi = 0;
		_entryPrice = 0;
		_cooldown = 0;
	}

		protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevRsi = 0;
		_entryPrice = 0;
		_cooldown = 0;

		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ema, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiVal, decimal emaVal, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevRsi == 0 || atrVal <= 0)
		{
			_prevRsi = rsiVal;
			return;
		}

		if (_cooldown > 0) { _cooldown--; _prevRsi = rsiVal; return; }

		var close = candle.ClosePrice;

		if (Position > 0)
		{
			if (close >= _entryPrice + atrVal * 2.5m || close <= _entryPrice - atrVal * 1.5m || rsiVal > 80)
			{
				SellMarket();
				_entryPrice = 0;
				_cooldown = 10;
			}
		}
		else if (Position < 0)
		{
			if (close <= _entryPrice - atrVal * 2.5m || close >= _entryPrice + atrVal * 1.5m || rsiVal < 20)
			{
				BuyMarket();
				_entryPrice = 0;
				_cooldown = 10;
			}
		}

		if (Position == 0)
		{
			if (rsiVal > 65 && _prevRsi <= 65 && close > emaVal)
			{
				_entryPrice = close;
				BuyMarket();
				_cooldown = 10;
			}
			else if (rsiVal < 35 && _prevRsi >= 35 && close < emaVal)
			{
				_entryPrice = close;
				SellMarket();
				_cooldown = 10;
			}
		}

		_prevRsi = rsiVal;
	}
}
