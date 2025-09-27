using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that enters long positions when volume and price change exceed thresholds
/// and VIX is below a specified level. Uses a volatility-based stop and closes
/// positions after a fixed number of periods.
/// </summary>
public class MaxPainStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<decimal> _priceChangeMultiplier;
	private readonly StrategyParam<decimal> _stopLossMultiplier;
	private readonly StrategyParam<decimal> _vixThreshold;
	private readonly StrategyParam<int> _holdPeriods;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _vixCandleType;

	private SimpleMovingAverage _volumeSma;
	private Shift _shift;
	private StandardDeviation _volatility;

	private int _barIndex;
	private int? _entryBar;
	private decimal? _vixClose;

	/// <summary>
	/// Lookback period for calculations.
	/// </summary>
	public int LookbackPeriod { get => _lookbackPeriod.Value; set => _lookbackPeriod.Value = value; }

	/// <summary>
	/// Multiplier for volume threshold.
	/// </summary>
	public decimal VolumeMultiplier { get => _volumeMultiplier.Value; set => _volumeMultiplier.Value = value; }

	/// <summary>
	/// Multiplier for price change threshold.
	/// </summary>
	public decimal PriceChangeMultiplier { get => _priceChangeMultiplier.Value; set => _priceChangeMultiplier.Value = value; }

	/// <summary>
	/// Multiplier for volatility-based stop-loss.
	/// </summary>
	public decimal StopLossMultiplier { get => _stopLossMultiplier.Value; set => _stopLossMultiplier.Value = value; }

	/// <summary>
	/// VIX level below which trading is allowed.
	/// </summary>
	public decimal VixThreshold { get => _vixThreshold.Value; set => _vixThreshold.Value = value; }

	/// <summary>
	/// Number of periods to hold the position.
	/// </summary>
	public int HoldPeriods { get => _holdPeriods.Value; set => _holdPeriods.Value = value; }

	/// <summary>
	/// Candle type for the main security.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Candle type for VIX data.
	/// </summary>
	public DataType VixCandleType { get => _vixCandleType.Value; set => _vixCandleType.Value = value; }

	/// <summary>
	/// Security representing VIX data.
	/// </summary>
	public Security VixSecurity { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="MaxPainStrategy"/> class.
	/// </summary>
	public MaxPainStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 70)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Period for volume and price change", "Parameters")
			.SetCanOptimize(true);

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 1m)
			.SetRange(1m, 5m)
			.SetDisplay("Volume Multiplier", "Threshold multiplier for volume", "Parameters")
			.SetCanOptimize(true);

		_priceChangeMultiplier = Param(nameof(PriceChangeMultiplier), 0.029m)
			.SetRange(0.01m, 0.1m)
			.SetDisplay("Price Change Multiplier", "Threshold for price movement", "Parameters")
			.SetCanOptimize(true);

		_stopLossMultiplier = Param(nameof(StopLossMultiplier), 2.4m)
			.SetGreaterThanZero()
			.SetDisplay("Stop-Loss Multiplier", "Multiplier for volatility-based stop-loss", "Risk Management")
			.SetCanOptimize(true);

		_vixThreshold = Param(nameof(VixThreshold), 44m)
			.SetGreaterThanZero()
			.SetDisplay("VIX Threshold", "VIX level below which trading is allowed", "Filters")
			.SetCanOptimize(true);

		_holdPeriods = Param(nameof(HoldPeriods), 8)
			.SetGreaterThanZero()
			.SetDisplay("Hold Periods", "Number of periods to hold the position", "Parameters")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Main candle timeframe", "General");

		_vixCandleType = Param(nameof(VixCandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("VIX Candle Type", "VIX data timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		var list = new List<(Security, DataType)> { (Security, CandleType) };

		if (VixSecurity != null)
			list.Add((VixSecurity, VixCandleType));

		return list;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_barIndex = 0;
		_entryBar = null;
		_vixClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (VixSecurity == null)
			throw new InvalidOperationException("VixSecurity is not specified.");

		_volumeSma = new SimpleMovingAverage { Length = LookbackPeriod };
		_shift = new Shift { Length = LookbackPeriod };
		_volatility = new StandardDeviation { Length = LookbackPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_shift, _volatility, ProcessCandle)
			.Start();

		var vixSubscription = SubscribeCandles(VixCandleType, security: VixSecurity);
		vixSubscription
			.Bind(ProcessVix)
			.Start();
	}

	private void ProcessVix(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_vixClose = candle.ClosePrice;
	}

	private void ProcessCandle(ICandleMessage candle, decimal prevClose, decimal volatility)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		var volValue = _volumeSma.Process(candle.TotalVolume);
		if (!volValue.IsFinal || volValue is not DecimalIndicatorValue dv)
			return;

		if (!_shift.IsFormed || !_volatility.IsFormed || _vixClose is not decimal vix)
			return;

		var avgVolume = dv.Value;
		var priceChange = Math.Abs(candle.ClosePrice - prevClose);

		var painZone = candle.TotalVolume > avgVolume * VolumeMultiplier &&
			priceChange > prevClose * PriceChangeMultiplier;

		var vixOk = vix < VixThreshold;

		if (painZone && vixOk && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Position.Abs());

			BuyMarket(Volume);
			_entryBar = _barIndex;

			var stopPrice = candle.ClosePrice - StopLossMultiplier * volatility;
			SellStop(Position, stopPrice);
		}

		if (Position > 0 && _entryBar.HasValue && _barIndex >= _entryBar + HoldPeriods)
		{
			SellMarket(Position);
			_entryBar = null;
		}
	}
}