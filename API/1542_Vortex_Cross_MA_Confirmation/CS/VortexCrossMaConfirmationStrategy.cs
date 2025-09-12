using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Vortex indicator cross with moving average confirmation.
/// </summary>
public class VortexCrossMaConfirmationStrategy : Strategy
{
	private readonly StrategyParam<int> _vortexLength;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<int> _smoothingLength;
	private readonly StrategyParam<MaTypeEnum> _maType;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _sma = null!;
	private LengthIndicator<decimal> _smoothMa = null!;
	private SimpleMovingAverage _vmpAvg = null!;
	private SimpleMovingAverage _vmmAvg = null!;
	private SimpleMovingAverage _trAvg = null!;

	private decimal? _prevHigh;
	private decimal? _prevLow;
	private decimal? _prevClose;
	private decimal? _prevVip;
	private decimal? _prevVim;

	/// <summary>
	/// Vortex period.
	/// </summary>
	public int VortexLength
	{
		get => _vortexLength.Value;
		set => _vortexLength.Value = value;
	}

	/// <summary>
	/// SMA length before smoothing.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	/// <summary>
	/// Smoothing length.
	/// </summary>
	public int SmoothingLength
	{
		get => _smoothingLength.Value;
		set => _smoothingLength.Value = value;
	}

	/// <summary>
	/// Smoothing method.
	/// </summary>
	public MaTypeEnum MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Moving average types.
	/// </summary>
	public enum MaTypeEnum
	{
		SMA,
		EMA,
		RMA,
		WMA,
		VWMA,
		ALMA,
		HMA
	}

	/// <summary>
	/// Initializes a new instance of <see cref="VortexCrossMaConfirmationStrategy"/>.
	/// </summary>
	public VortexCrossMaConfirmationStrategy()
	{
		_vortexLength = Param(nameof(VortexLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Vortex Length", "Period for Vortex indicator", "General")
			.SetCanOptimize(true);

		_smaLength = Param(nameof(SmaLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Base SMA length", "General")
			.SetCanOptimize(true);

		_smoothingLength = Param(nameof(SmoothingLength), 1)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing Length", "Length for additional smoothing", "General")
			.SetCanOptimize(true);

		_maType = Param(nameof(MaType), MaTypeEnum.SMA)
			.SetDisplay("MA Type", "Smoothing method", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_sma = new SimpleMovingAverage { Length = SmaLength };
		_smoothMa = MaType switch
		{
			MaTypeEnum.EMA => new ExponentialMovingAverage { Length = SmoothingLength },
			MaTypeEnum.RMA => new SmoothedMovingAverage { Length = SmoothingLength },
			MaTypeEnum.WMA => new WeightedMovingAverage { Length = SmoothingLength },
			MaTypeEnum.VWMA => new VolumeWeightedMovingAverage { Length = SmoothingLength },
			MaTypeEnum.ALMA => new ArnaudLegouxMovingAverage { Length = SmoothingLength },
			MaTypeEnum.HMA => new HullMovingAverage { Length = SmoothingLength },
			_ => new SimpleMovingAverage { Length = SmoothingLength }
		};

		_vmpAvg = new SimpleMovingAverage { Length = VortexLength };
		_vmmAvg = new SimpleMovingAverage { Length = VortexLength };
		_trAvg = new SimpleMovingAverage { Length = VortexLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.WhenNew(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawIndicator(area, _smoothMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var smaValue = _sma.Process(candle.ClosePrice);
		var smoothValue = _smoothMa.Process(smaValue.ToDecimal());

		if (_prevHigh == null)
		{
			_prevHigh = candle.HighPrice;
			_prevLow = candle.LowPrice;
			_prevClose = candle.ClosePrice;
			return;
		}

		var vmp = Math.Abs(candle.HighPrice - _prevLow.Value);
		var vmm = Math.Abs(candle.LowPrice - _prevHigh.Value);
		var tr = Math.Max(candle.HighPrice - candle.LowPrice, Math.Max(Math.Abs(candle.HighPrice - _prevClose.Value), Math.Abs(candle.LowPrice - _prevClose.Value)));

		var vmpValue = _vmpAvg.Process(vmp);
		var vmmValue = _vmmAvg.Process(vmm);
		var trValue = _trAvg.Process(tr);

		_prevHigh = candle.HighPrice;
		_prevLow = candle.LowPrice;
		_prevClose = candle.ClosePrice;

		if (!smoothValue.IsFinal || !vmpValue.IsFinal || !vmmValue.IsFinal || !trValue.IsFinal)
			return;

		var vip = vmpValue.ToDecimal() / trValue.ToDecimal();
		var vim = vmmValue.ToDecimal() / trValue.ToDecimal();

		if (_prevVip == null || _prevVim == null)
		{
			_prevVip = vip;
			_prevVim = vim;
			return;
		}

		bool longCondition = _prevVip < _prevVim && vip > vim && candle.ClosePrice > smoothValue.ToDecimal();
		bool shortCondition = _prevVip > _prevVim && vip < vim && candle.ClosePrice < smoothValue.ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevVip = vip;
			_prevVim = vim;
			return;
		}

		if (longCondition && Position <= 0)
		{
			var qty = Volume + (Position < 0 ? -Position : 0m);
			BuyMarket(qty);
		}
		else if (shortCondition && Position >= 0)
		{
			var qty = Volume + (Position > 0 ? Position : 0m);
			SellMarket(qty);
		}

		_prevVip = vip;
		_prevVim = vim;
	}
}
