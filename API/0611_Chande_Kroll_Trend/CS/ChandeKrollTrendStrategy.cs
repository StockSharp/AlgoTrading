using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Chande Kroll Trend strategy.
/// Goes long when price crosses above the lower stop and is above SMA.
/// Exits when price closes below the upper stop.
/// </summary>
public class ChandeKrollTrendStrategy : Strategy
{
	private readonly StrategyParam<CalcMode> _calcMode;
	private readonly StrategyParam<decimal> _riskMultiplier;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _stopLength;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr = null!;
	private DonchianChannels _donchian = null!;
	private SimpleMovingAverage _sma = null!;
	private Lowest _lowestClose = null!;

	private decimal _prevClose;
	private decimal _prevLowStop;
	private bool _hasPrev;
	private decimal _initialCapital;

	/// <summary>
	/// Position size calculation mode.
	/// </summary>
	public CalcMode CalcMode { get => _calcMode.Value; set => _calcMode.Value = value; }

	/// <summary>
	/// Risk multiplier for position sizing.
	/// </summary>
	public decimal RiskMultiplier { get => _riskMultiplier.Value; set => _riskMultiplier.Value = value; }

	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// ATR multiplier for stop calculation.
	/// </summary>
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

	/// <summary>
	/// Lookback period for Donchian channel.
	/// </summary>
	public int StopLength { get => _stopLength.Value; set => _stopLength.Value = value; }

	/// <summary>
	/// SMA period.
	/// </summary>
	public int SmaLength { get => _smaLength.Value; set => _smaLength.Value = value; }

	/// <summary>
	/// Candle type for strategy calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public ChandeKrollTrendStrategy()
	{
		_calcMode = Param(nameof(CalcMode), CalcMode.Exponential)
			.SetDisplay("Calc Mode", "Position size calculation mode", "General");

		_riskMultiplier = Param(nameof(RiskMultiplier), 5m)
			.SetDisplay("Risk Multiplier", "Risk multiplier for quantity", "Risk");

		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetDisplay("ATR Period", "ATR calculation period", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 3m)
			.SetDisplay("ATR Multiplier", "ATR stop multiplier", "Indicators");

		_stopLength = Param(nameof(StopLength), 21)
			.SetDisplay("Stop Length", "Lookback for Donchian extremes", "Indicators");

		_smaLength = Param(nameof(SmaLength), 21)
			.SetDisplay("SMA Length", "SMA period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for strategy", "General");
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

		_prevClose = default;
		_prevLowStop = default;
		_hasPrev = default;
		_initialCapital = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange { Length = AtrPeriod };
		_donchian = new DonchianChannels { Length = StopLength };
		_sma = new SimpleMovingAverage { Length = SmaLength };
		_lowestClose = new Lowest { Length = 1560 };

		_initialCapital = Portfolio.CurrentValue ?? 0m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_donchian, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _donchian);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var dc = (DonchianChannelsValue)donchianValue;
		if (dc.UpperBand is not decimal upper || dc.LowerBand is not decimal lower)
			return;

		var atrVal = _atr.Process(candle);
		var smaVal = _sma.Process(candle.ClosePrice, candle.OpenTime, true);
		var lowCloseVal = _lowestClose.Process(candle.ClosePrice, candle.OpenTime, true);

		if (!atrVal.IsFinal || !smaVal.IsFinal || !lowCloseVal.IsFinal || !_donchian.IsFormed)
		{
			_prevClose = candle.ClosePrice;
			_prevLowStop = lower + AtrMultiplier * (atrVal.IsFinal ? atrVal.ToDecimal() : 0m);
			_hasPrev = true;
			return;
		}

		var atr = atrVal.ToDecimal();
		var sma = smaVal.ToDecimal();
		var lowestClose = lowCloseVal.ToDecimal();

		var highStop = upper - AtrMultiplier * atr;
		var lowStop = lower + AtrMultiplier * atr;

		if (!_hasPrev)
		{
			_prevClose = candle.ClosePrice;
			_prevLowStop = lowStop;
			_hasPrev = true;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevClose = candle.ClosePrice;
			_prevLowStop = lowStop;
			return;
		}

		var longCondition = _prevClose <= _prevLowStop && candle.ClosePrice > lowStop && candle.ClosePrice > sma;

		if (longCondition && Position <= 0)
		{
			var qty = RiskMultiplier / lowestClose * 1000m;
			if (CalcMode == CalcMode.Exponential && _initialCapital > 0m)
			{
				var equity = Portfolio.CurrentValue ?? 0m;
				qty *= equity / _initialCapital;
			}

			BuyMarket(qty + Math.Abs(Position));
		}
		else if (Position > 0 && candle.ClosePrice < highStop)
		{
			SellMarket(Position);
		}

		_prevClose = candle.ClosePrice;
		_prevLowStop = lowStop;
	}
}

/// <summary>
/// Position size calculation modes.
/// </summary>
public enum CalcMode
{
	/// <summary>
	/// Use fixed multiplier.
	/// </summary>
	Linear,
	/// <summary>
	/// Scale by current equity.
	/// </summary>
	Exponential
}
