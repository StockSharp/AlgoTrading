using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Triangle breakout strategy for BTC.
/// Trades breakouts of simple moving average triangle with volume confirmation and ATR stops.
/// </summary>
public class TriangleBreakoutBtcMark804Strategy : Strategy
{
	private readonly StrategyParam<int> _triangleLength;
	private readonly StrategyParam<int> _volumeSmaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<decimal> _atrMultiplierSl;
	private readonly StrategyParam<decimal> _atrMultiplierTp;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _upper = null!;
	private SimpleMovingAverage _lower = null!;
	private SimpleMovingAverage _volumeSma = null!;
	private AverageTrueRange _atr = null!;

	private decimal? _prevClose;
	private decimal? _prevUpper;
	private decimal? _prevLower;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Triangle lookback length.
	/// </summary>
	public int TriangleLength { get => _triangleLength.Value; set => _triangleLength.Value = value; }

	/// <summary>
	/// Volume SMA length.
	/// </summary>
	public int VolumeSmaLength { get => _volumeSmaLength.Value; set => _volumeSmaLength.Value = value; }

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	/// <summary>
	/// Volume spike multiplier.
	/// </summary>
	public decimal VolumeMultiplier { get => _volumeMultiplier.Value; set => _volumeMultiplier.Value = value; }

	/// <summary>
	/// ATR stop-loss multiplier.
	/// </summary>
	public decimal AtrMultiplierSl { get => _atrMultiplierSl.Value; set => _atrMultiplierSl.Value = value; }

	/// <summary>
	/// ATR take-profit multiplier.
	/// </summary>
	public decimal AtrMultiplierTp { get => _atrMultiplierTp.Value; set => _atrMultiplierTp.Value = value; }

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="TriangleBreakoutBtcMark804Strategy"/>.
	/// </summary>
	public TriangleBreakoutBtcMark804Strategy()
	{
		_triangleLength = Param(nameof(TriangleLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Triangle Length", "Lookback for SMA lines", "General")
			.SetCanOptimize(true);

		_volumeSmaLength = Param(nameof(VolumeSmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume SMA Length", "Lookback for volume average", "General")
			.SetCanOptimize(true);

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "General")
			.SetCanOptimize(true);

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 1.5m)
			.SetDisplay("Volume Multiplier", "Volume spike multiplier", "General")
			.SetCanOptimize(true);

		_atrMultiplierSl = Param(nameof(AtrMultiplierSl), 1m)
			.SetDisplay("ATR SL Multiplier", "Stop loss ATR multiplier", "General")
			.SetCanOptimize(true);

		_atrMultiplierTp = Param(nameof(AtrMultiplierTp), 1.5m)
			.SetDisplay("ATR TP Multiplier", "Take profit ATR multiplier", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_upper = null!;
		_lower = null!;
		_volumeSma = null!;
		_atr = null!;
		_prevClose = null;
		_prevUpper = null;
		_prevLower = null;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_upper = new SimpleMovingAverage { Length = TriangleLength };
		_lower = new SimpleMovingAverage { Length = TriangleLength };
		_volumeSma = new SimpleMovingAverage { Length = VolumeSmaLength };
		_atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenNew(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _upper);
			DrawIndicator(area, _lower);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var upperVal = _upper.Process(candle.HighPrice, candle.CloseTime, true);
		var lowerVal = _lower.Process(candle.LowPrice, candle.CloseTime, true);
		var volVal = _volumeSma.Process(candle.TotalVolume, candle.CloseTime, true);
		var atrVal = _atr.Process(candle);

		if (!upperVal.IsFinal || !lowerVal.IsFinal || !volVal.IsFinal || !atrVal.IsFinal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var upper = upperVal.ToDecimal();
		var lower = lowerVal.ToDecimal();
		var volumeAvg = volVal.ToDecimal();
		var atr = atrVal.ToDecimal();

		var breakoutUp = _prevClose is decimal pc && _prevUpper is decimal pu && pc <= pu && candle.ClosePrice > upper;
		var breakoutDown = _prevClose is decimal pc2 && _prevLower is decimal pl && pc2 >= pl && candle.ClosePrice < lower;
		var volConfirmed = candle.TotalVolume > volumeAvg * VolumeMultiplier;

		if (breakoutUp && volConfirmed && Position <= 0)
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			BuyMarket(volume);
			_stopPrice = candle.ClosePrice - atr * AtrMultiplierSl;
			_takePrice = candle.ClosePrice + atr * AtrMultiplierTp;
		}
		else if (breakoutDown && volConfirmed && Position >= 0)
		{
			var volume = Volume + (Position > 0 ? Position : 0m);
			SellMarket(volume);
			_stopPrice = candle.ClosePrice + atr * AtrMultiplierSl;
			_takePrice = candle.ClosePrice - atr * AtrMultiplierTp;
		}
		else if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
				BuyMarket(Math.Abs(Position));
		}

		_prevClose = candle.ClosePrice;
		_prevUpper = upper;
		_prevLower = lower;
	}
}
