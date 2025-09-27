using System;

using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Fusion of standardized RSI and CCI with dynamic bands.
/// </summary>
public class RsiCciFusionStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<decimal> _rsiWeight;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _rsiSma;
	private StandardDeviation _rsiStd;
	private SimpleMovingAverage _cciSma;
	private StandardDeviation _cciStd;
	private SimpleMovingAverage _combinedSma;
	private StandardDeviation _combinedStd;
	private SimpleMovingAverage _rescaledSma;
	private StandardDeviation _rescaledStd;

	private decimal _prevRescaled;
	private decimal _prevUpper;
	private decimal _prevLower;
	private bool _isInitialized;

	/// <summary>
	/// Period length for RSI and CCI.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Weight of RSI in fusion.
	/// </summary>
	public decimal RsiWeight
	{
		get => _rsiWeight.Value;
		set => _rsiWeight.Value = value;
	}

	/// <summary>
	/// Allow short positions.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="RsiCciFusionStrategy"/>.
	/// </summary>
	public RsiCciFusionStrategy()
	{
		_length = Param(nameof(Length), 14)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Period for RSI and CCI", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_rsiWeight = Param(nameof(RsiWeight), 0.5m)
			.SetDisplay("RSI Weight", "Weight of RSI in fusion", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(0m, 1m, 0.1m);

		_enableShort = Param(nameof(EnableShort), false)
			.SetDisplay("Enable Short", "Allow short positions", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevRescaled = 0m;
		_prevUpper = 0m;
		_prevLower = 0m;
		_isInitialized = false;
		_rsiSma = null;
		_rsiStd = null;
		_cciSma = null;
		_cciStd = null;
		_combinedSma = null;
		_combinedStd = null;
		_rescaledSma = null;
		_rescaledStd = null;
	}

	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(StockSharp.BusinessEntities.Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var rsi = new RelativeStrengthIndex { Length = Length };
		var cci = new CommodityChannelIndex { Length = Length };

		_rsiSma = new SimpleMovingAverage { Length = Length };
		_rsiStd = new StandardDeviation { Length = Length };
		_cciSma = new SimpleMovingAverage { Length = Length };
		_cciStd = new StandardDeviation { Length = Length };
		_combinedSma = new SimpleMovingAverage { Length = Length };
		_combinedStd = new StandardDeviation { Length = Length };
		_rescaledSma = new SimpleMovingAverage { Length = Length };
		_rescaledStd = new StandardDeviation { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(rsi, cci, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var rsiMean = _rsiSma!.Process(rsiValue, candle.OpenTime, true).ToDecimal();
		var rsiStd = _rsiStd!.Process(rsiValue, candle.OpenTime, true).ToDecimal();
		var rsiZ = rsiStd != 0m ? (rsiValue - rsiMean) / rsiStd : 0m;

		var cciMean = _cciSma!.Process(cciValue, candle.OpenTime, true).ToDecimal();
		var cciStd = _cciStd!.Process(cciValue, candle.OpenTime, true).ToDecimal();
		var cciZ = cciStd != 0m ? (cciValue - cciMean) / cciStd : 0m;

		var cciWeight = 1m - RsiWeight;
		var combined = RsiWeight * rsiZ + cciWeight * cciZ;

		var combinedMean = _combinedSma!.Process(combined, candle.OpenTime, true).ToDecimal();
		var combinedStd = _combinedStd!.Process(combined, candle.OpenTime, true).ToDecimal();
		var rescaled = combined * combinedStd + combinedMean;

		var rescaledMean = _rescaledSma!.Process(rescaled, candle.OpenTime, true).ToDecimal();
		var rescaledStd = _rescaledStd!.Process(rescaled, candle.OpenTime, true).ToDecimal();
		var upperBand = rescaledMean + rescaledStd;
		var lowerBand = rescaledMean - rescaledStd;

		if (!_isInitialized)
		{
			_prevRescaled = rescaled;
			_prevUpper = upperBand;
			_prevLower = lowerBand;
			_isInitialized = true;
			return;
		}

		var buySignal = _prevRescaled <= _prevLower && rescaled > lowerBand;
		var sellSignal = _prevRescaled >= _prevUpper && rescaled < upperBand;

		if (buySignal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));

		if (sellSignal)
		{
			if (EnableShort && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
			else if (!EnableShort && Position > 0)
				SellMarket(Position);
		}

		_prevRescaled = rescaled;
		_prevUpper = upperBand;
		_prevLower = lowerBand;
	}
}
