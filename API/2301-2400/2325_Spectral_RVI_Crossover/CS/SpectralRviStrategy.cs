using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Spectral RVI crossover strategy.
/// Applies smoothing to RVI average and signal and trades on their crossovers.
/// </summary>
public class SpectralRviStrategy : Strategy
{
	private readonly StrategyParam<int> _rviLength;
	private readonly StrategyParam<int> _smoothLength;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _smoothRvi;
	private SimpleMovingAverage _smoothSig;

	private decimal? _prevSmRvi;
	private decimal? _prevSmSig;

	public int RviLength { get => _rviLength.Value; set => _rviLength.Value = value; }
	public int SmoothLength { get => _smoothLength.Value; set => _smoothLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public SpectralRviStrategy()
	{
		_rviLength = Param(nameof(RviLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RVI Length", "Length for RVI", "General");

		_smoothLength = Param(nameof(SmoothLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Smooth Length", "Smoothing length", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_smoothRvi = null;
		_smoothSig = null;
		_prevSmRvi = null;
		_prevSmSig = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevSmRvi = null;
		_prevSmSig = null;
		_smoothRvi = new SimpleMovingAverage { Length = SmoothLength };
		_smoothSig = new SimpleMovingAverage { Length = SmoothLength };

		var rvi = new RelativeVigorIndex();

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(rvi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rvi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue rviVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (rviVal is not IRelativeVigorIndexValue rviTyped)
			return;

		if (rviTyped.Average is not decimal avg || rviTyped.Signal is not decimal sig)
			return;

		var t = candle.CloseTime;
		var smRviResult = _smoothRvi.Process(avg, t, true);
		var smSigResult = _smoothSig.Process(sig, t, true);

		if (!_smoothRvi.IsFormed || !_smoothSig.IsFormed)
			return;

		var smRvi = smRviResult.ToDecimal();
		var smSig = smSigResult.ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevSmRvi = smRvi;
			_prevSmSig = smSig;
			return;
		}

		if (_prevSmRvi is decimal prevR && _prevSmSig is decimal prevS)
		{
			if (prevR <= prevS && smRvi > smSig && Position <= 0)
				BuyMarket();
			else if (prevR >= prevS && smRvi < smSig && Position >= 0)
				SellMarket();
		}

		_prevSmRvi = smRvi;
		_prevSmSig = smSig;
	}
}
