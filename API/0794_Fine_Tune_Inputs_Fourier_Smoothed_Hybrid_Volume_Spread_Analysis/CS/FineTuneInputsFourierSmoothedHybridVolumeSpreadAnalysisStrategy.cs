using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class FineTuneInputsFourierSmoothedHybridVolumeSpreadAnalysisStrategy : Strategy
{
	private readonly StrategyParam<bool> _useSmoothedVolume;
	private readonly StrategyParam<int> _volumeLength;
	private readonly StrategyParam<int> _priceLength;
	private readonly StrategyParam<int> _vdmaLength;
	private readonly StrategyParam<bool> _enableCloseAll;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage? _closeEma;
	private ExponentialMovingAverage? _openEma;
	private ExponentialMovingAverage? _volumeEma;
	private ExponentialMovingAverage? _vdma;
	private decimal _prevVolume;

	public bool UseSmoothedVolume
	{
		get => _useSmoothedVolume.Value;
		set => _useSmoothedVolume.Value = value;
	}

	public int VolumeLength
	{
		get => _volumeLength.Value;
		set => _volumeLength.Value = value;
	}

	public int PriceLength
	{
		get => _priceLength.Value;
		set => _priceLength.Value = value;
	}

	public int VdmaLength
	{
		get => _vdmaLength.Value;
		set => _vdmaLength.Value = value;
	}

	public bool EnableCloseAll
	{
		get => _enableCloseAll.Value;
		set => _enableCloseAll.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public FineTuneInputsFourierSmoothedHybridVolumeSpreadAnalysisStrategy()
	{
		_useSmoothedVolume = Param(nameof(UseSmoothedVolume), true)
			.SetDisplay("Smoothed Volume", "Use EMA smoothed volume", "Source");

		_volumeLength = Param(nameof(VolumeLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Volume Length", "EMA period for volume", "Source")
			.SetCanOptimize(true);

		_priceLength = Param(nameof(PriceLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Price Length", "EMA period for price", "Calculations")
			.SetCanOptimize(true);

		_vdmaLength = Param(nameof(VdmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("VDMA Length", "EMA period for vd", "Calculations")
			.SetCanOptimize(true);

		_enableCloseAll = Param(nameof(EnableCloseAll), false)
			.SetDisplay("Enable Close All", "Close position when signal neutral", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevVolume = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_closeEma = new ExponentialMovingAverage { Length = PriceLength };
		_openEma = new ExponentialMovingAverage { Length = PriceLength };
		_volumeEma = new ExponentialMovingAverage { Length = VolumeLength };
		_vdma = new ExponentialMovingAverage { Length = VdmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_closeEma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal closeEmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var openEmaValue = _openEma!.Process(candle.OpenPrice);
		if (!openEmaValue.IsFinal || !openEmaValue.TryGetValue(out decimal openEma))
			return;

		decimal volumeFactor;
		if (UseSmoothedVolume)
		{
			var volEmaValue = _volumeEma!.Process(candle.TotalVolume);
			if (!volEmaValue.IsFinal || !volEmaValue.TryGetValue(out decimal volEma) || volEma == 0m)
				return;

			volumeFactor = candle.TotalVolume / volEma;
		}
		else
		{
			if (_prevVolume == 0m)
			{
				_prevVolume = candle.TotalVolume;
				return;
			}

			volumeFactor = candle.TotalVolume / _prevVolume;
			_prevVolume = candle.TotalVolume;
		}

		var vd = closeEmaValue * volumeFactor - openEma * volumeFactor;
		var vdmaValue = _vdma!.Process(vd);
		if (!vdmaValue.IsFinal || !vdmaValue.TryGetValue(out decimal vdma))
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var longCondition = vdma > 0m && vd > 0m;
		var shortCondition = vdma < 0m && vd < 0m;

		if (longCondition && Position <= 0)
			BuyMarket();
		else if (shortCondition && Position >= 0)
			SellMarket();
		else if (!longCondition && !shortCondition && EnableCloseAll && Position != 0)
			ClosePosition();
	}
}
