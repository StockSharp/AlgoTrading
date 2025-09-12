using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// RSI, volume, MACD and EMA combo strategy.
/// Goes long when all bullish conditions are met and short for bearish.
/// Exits when RSI crosses the 50 level.
/// </summary>
public class RsiVolumeMacdEmaComboStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _overbought;
	private readonly StrategyParam<int> _oversold;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private bool _isFirst;

	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int Overbought { get => _overbought.Value; set => _overbought.Value = value; }
	public int Oversold { get => _oversold.Value; set => _oversold.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }
	public int SmaLength { get => _smaLength.Value; set => _smaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RsiVolumeMacdEmaComboStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 200);
		_rsiLength = Param(nameof(RsiLength), 14);
		_overbought = Param(nameof(Overbought), 50);
		_oversold = Param(nameof(Oversold), 50);
		_slowLength = Param(nameof(SlowLength), 26);
		_fastLength = Param(nameof(FastLength), 12);
		_signalLength = Param(nameof(SignalLength), 9);
		_smaLength = Param(nameof(SmaLength), 20);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevRsi = 0m;
		_isFirst = true;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ema = new EMA { Length = EmaLength };
		var rsi = new RSI { Length = RsiLength };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastLength },
				LongMa = { Length = SlowLength },
			},
			SignalMa = { Length = SignalLength }
		};
		var volSma = new SMA { Length = SmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, rsi).BindEx(macd, Process).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}

		void Process(ICandleMessage candle, decimal emaVal, decimal rsiVal, IIndicatorValue macdVal)
		{
			if (candle.State != CandleStates.Finished)
				return;

			var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdVal;
			if (macdTyped.Macd is not decimal macdLine || macdTyped.Signal is not decimal signalLine)
				return;

			var volVal = volSma.Process(candle.TotalVolume);
			if (!volVal.IsFinal || !ema.IsFormed || !rsi.IsFormed || !macd.IsFormed)
				return;

			var volAvg = volVal.ToDecimal();

			var buy = macdLine > signalLine && rsiVal > Overbought && candle.ClosePrice > emaVal && candle.TotalVolume > volAvg;
			var @short = macdLine < signalLine && rsiVal < Oversold && candle.ClosePrice < emaVal && candle.TotalVolume > volAvg;
			var sell = !_isFirst && _prevRsi >= 50m && rsiVal < 50m;
			var cover = !_isFirst && _prevRsi <= 50m && rsiVal > 50m;

			if (buy && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			if (@short && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
			if (sell && Position > 0)
				SellMarket(Position);
			if (cover && Position < 0)
				BuyMarket(-Position);

			_prevRsi = rsiVal;
			_isFirst = false;
		}
	}
}
