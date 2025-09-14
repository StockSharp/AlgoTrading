namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;
using StockSharp.BusinessEntities;

/// <summary>
/// Strategy based on MACD histogram as an approximation of the Ergodic Ticks Volume OSMA indicator.
/// </summary>
public class ErgodicTicksVolumeOsmaStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevHist;
	private decimal _prevPrevHist;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ErgodicTicksVolumeOsmaStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12).SetDisplay("Fast EMA").SetCanOptimize();
		_slowLength = Param(nameof(SlowLength), 26).SetDisplay("Slow EMA").SetCanOptimize();
		_signalLength = Param(nameof(SignalLength), 9).SetDisplay("Signal EMA").SetCanOptimize();
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame()).SetDisplay("Timeframe");
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
		_prevHist = 0m;
		_prevPrevHist = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_prevHist = 0m;
		_prevPrevHist = 0m;

		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastLength },
				LongMa = { Length = SlowLength },
			},
			SignalMa = { Length = SignalLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(macd, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal macd, decimal signal, decimal hist)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevPrevHist == 0m)
		{
			_prevPrevHist = hist;
			return;
		}

		if (_prevHist == 0m)
		{
			_prevHist = hist;
			return;
		}

		var rising = _prevHist >= _prevPrevHist && hist >= _prevHist;
		var falling = _prevHist <= _prevPrevHist && hist <= _prevHist;

		if (rising)
		{
			if (Position > 0)
			{
				// Already long, nothing to do.
			}
			else
			{
				if (Position < 0)
					ClosePosition();
				BuyMarket();
			}
		}
		else if (falling)
		{
			if (Position < 0)
			{
				// Already short, nothing to do.
			}
			else
			{
				if (Position > 0)
					ClosePosition();
				SellMarket();
			}
		}

		_prevPrevHist = _prevHist;
		_prevHist = hist;
	}
}
