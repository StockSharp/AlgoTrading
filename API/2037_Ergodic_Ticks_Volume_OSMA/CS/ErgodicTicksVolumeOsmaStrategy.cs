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
/// Strategy based on MACD histogram as an approximation of the Ergodic Ticks Volume OSMA indicator.
/// </summary>
public class ErgodicTicksVolumeOsmaStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _macd;
	private decimal _prevHist;
	private decimal _prevPrevHist;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ErgodicTicksVolumeOsmaStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12).SetDisplay("Fast EMA", "Fast EMA length", "Indicators");
		_slowLength = Param(nameof(SlowLength), 26).SetDisplay("Slow EMA", "Slow EMA length", "Indicators");
		_signalLength = Param(nameof(SignalLength), 9).SetDisplay("Signal EMA", "Signal EMA length", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame()).SetDisplay("Timeframe", "Timeframe", "General");
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
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHist = 0m;
		_prevPrevHist = 0m;

		_macd = new MovingAverageConvergenceDivergenceSignal(
			new MovingAverageConvergenceDivergence
			{
				ShortMa = { Length = FastLength },
				LongMa = { Length = SlowLength },
			},
			new ExponentialMovingAverage { Length = SignalLength }
		);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_macd, ProcessCandle)
			.Start();

		StartProtection(null, null);
	}

	private void ProcessCandle(ICandleMessage candle, decimal macdVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_macd.IsFormed)
			return;

		var signalVal = _macd.SignalMa.GetCurrentValue();
		var hist = macdVal - signalVal;

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

		if (rising && Position <= 0)
		{
			BuyMarket();
		}
		else if (falling && Position >= 0)
		{
			SellMarket();
		}

		_prevPrevHist = _prevHist;
		_prevHist = hist;
	}
}
