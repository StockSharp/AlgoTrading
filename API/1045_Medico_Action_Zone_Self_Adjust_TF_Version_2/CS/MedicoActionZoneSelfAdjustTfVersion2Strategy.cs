using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with higher timeframe confirmation.
/// </summary>
public class MedicoActionZoneSelfAdjustTfVersion2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _higherCandleType;
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;

	private ExponentialMovingAverage _fastEmaCur;
	private ExponentialMovingAverage _slowEmaCur;
	private ExponentialMovingAverage _fastEmaHtf;
	private ExponentialMovingAverage _slowEmaHtf;

	private decimal _fastEmaHtfValue;
	private decimal _slowEmaHtfValue;
	private decimal _closeHtf;
	private decimal _prevFast;
	private decimal _prevSlow;

	public MedicoActionZoneSelfAdjustTfVersion2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");

		_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromDays(7).TimeFrame())
			.SetDisplay("Higher Candle Type", "EMA calculation timeframe", "General");

		_fastEmaLength = Param(nameof(FastEmaLength), 12)
			.SetDisplay("Fast EMA Length", "Short EMA period", "Indicators");

		_slowEmaLength = Param(nameof(SlowEmaLength), 26)
			.SetDisplay("Slow EMA Length", "Long EMA period", "Indicators");
	}

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public DataType HigherCandleType { get => _higherCandleType.Value; set => _higherCandleType.Value = value; }
	public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }
	public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> HigherCandleType == CandleType
			? [(Security, CandleType)]
			: [(Security, CandleType), (Security, HigherCandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_fastEmaHtfValue = 0m;
		_slowEmaHtfValue = 0m;
		_closeHtf = 0m;
		_prevFast = 0m;
		_prevSlow = 0m;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastEmaCur = new ExponentialMovingAverage { Length = FastEmaLength };
		_slowEmaCur = new ExponentialMovingAverage { Length = SlowEmaLength };
		_fastEmaHtf = new ExponentialMovingAverage { Length = FastEmaLength };
		_slowEmaHtf = new ExponentialMovingAverage { Length = SlowEmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastEmaCur, _slowEmaCur, ProcessCandle)
			.Start();

		if (HigherCandleType != CandleType)
		{
			var higherSubscription = SubscribeCandles(HigherCandleType);
			higherSubscription
				.Bind(_fastEmaHtf, _slowEmaHtf, ProcessHigher)
				.Start();
		}
	}

	private void ProcessHigher(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_fastEmaHtfValue = fast;
		_slowEmaHtfValue = slow;
		_closeHtf = candle.ClosePrice;
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (HigherCandleType == CandleType)
		{
			if (!_fastEmaCur.IsFormed || !_slowEmaCur.IsFormed)
				return;
		}
		else
		{
			if (!_fastEmaHtf.IsFormed || !_slowEmaHtf.IsFormed)
				return;
		}

		var emaFast = HigherCandleType == CandleType ? fast : _fastEmaHtfValue;
		var emaSlow = HigherCandleType == CandleType ? slow : _slowEmaHtfValue;
		var closeHtf = HigherCandleType == CandleType ? candle.ClosePrice : _closeHtf;

		var buySignal = _prevFast <= _prevSlow && emaFast > emaSlow && closeHtf > emaFast;
		var sellSignal = _prevFast >= _prevSlow && emaFast < emaSlow && closeHtf < emaSlow;

		if (buySignal && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (sellSignal && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}

		_prevFast = emaFast;
		_prevSlow = emaSlow;
	}
}

