using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Chande Kroll Trend strategy.
/// The high stop is the highest high of the last StopLength candles minus AtrMultiplier times ATR(AtrPeriod), the low stop the lowest
/// low plus the same distance. Long only: buys when the close crosses above the low stop while above SMA(SmaLength), and closes the
/// long when the close falls below the high stop. The order size is RiskMultiplier percent of the capital divided by the lowest close
/// of the last 1560 candles; in Exponential mode the capital is the current equity, in Linear mode the starting capital.
/// </summary>
public class ChandeKrollTrendStrategy : Strategy
{
	/// <summary>
	/// Position sizing mode.
	/// </summary>
	public enum CalcModes
	{
		/// <summary>
		/// Size from the starting capital.
		/// </summary>
		Linear,

		/// <summary>
		/// Size from the current equity.
		/// </summary>
		Exponential,
	}

	private const int _sizingLookback = 1560;

	private readonly StrategyParam<CalcModes> _calcMode;
	private readonly StrategyParam<decimal> _riskMultiplier;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _stopLength;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<DataType> _candleType;

	private Lowest _lowestClose;
	private decimal? _prevClose;
	private decimal? _prevLowStop;
	private decimal _initialCapital;

	/// <summary>
	/// Position sizing mode.
	/// </summary>
	public CalcModes CalcMode
	{
		get => _calcMode.Value;
		set => _calcMode.Value = value;
	}

	/// <summary>
	/// Percent of the capital committed to a position.
	/// </summary>
	public decimal RiskMultiplier
	{
		get => _riskMultiplier.Value;
		set => _riskMultiplier.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the stops.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// Candles of the Donchian extremes.
	/// </summary>
	public int StopLength
	{
		get => _stopLength.Value;
		set => _stopLength.Value = value;
	}

	/// <summary>
	/// Period of the trend SMA.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
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
	/// Constructor.
	/// </summary>
	public ChandeKrollTrendStrategy()
	{
		_calcMode = Param(nameof(CalcMode), CalcModes.Exponential)
			.SetDisplay("Calc Mode", "Position sizing mode", "Sizing");

		_riskMultiplier = Param(nameof(RiskMultiplier), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Multiplier", "Percent of the capital committed to a position", "Sizing");

		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Stops");

		_atrMultiplier = Param(nameof(AtrMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiplier of the stops", "Stops");

		_stopLength = Param(nameof(StopLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Stop Length", "Candles of the Donchian extremes", "Stops");

		_smaLength = Param(nameof(SmaLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Period of the trend SMA", "Trend");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_lowestClose = null;
		_prevClose = null;
		_prevLowStop = null;
		_initialCapital = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = null;
		_prevLowStop = null;
		_initialCapital = Portfolio?.BeginValue is decimal begin && begin > 0 ? begin : Portfolio?.CurrentValue ?? 0m;
		_lowestClose = new Lowest { Length = _sizingLookback };

		var atr = new AverageTrueRange { Length = AtrPeriod };
		var highest = new Highest { Length = StopLength };
		var lowest = new Lowest { Length = StopLength };
		var sma = new SimpleMovingAverage { Length = SmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(atr, highest, lowest, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private decimal GetOrderVolume(decimal lowestClose)
	{
		var capital = CalcMode == CalcModes.Exponential ? _initialCapital + PnL : _initialCapital;

		if (capital <= 0 || lowestClose <= 0)
			return Volume;

		var volume = capital * RiskMultiplier / 100m / lowestClose;
		var step = Security?.VolumeStep ?? 1m;

		if (step > 0)
			volume = Math.Floor(volume / step) * step;

		return volume > 0 ? volume : Volume;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue, IIndicatorValue highestValue, IIndicatorValue lowestValue, IIndicatorValue smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		// The sizing low uses whatever history is available until the full lookback is reached.
		var lowestClose = _lowestClose.Process(new DecimalIndicatorValue(_lowestClose, close, candle.OpenTime) { IsFinal = true }).GetValue<decimal>();

		if (!atrValue.IsFormed || !highestValue.IsFormed || !lowestValue.IsFormed)
			return;

		var distance = AtrMultiplier * atrValue.GetValue<decimal>();
		var highStop = highestValue.GetValue<decimal>() - distance;
		var lowStop = lowestValue.GetValue<decimal>() + distance;

		var prevClose = _prevClose;
		var prevLowStop = _prevLowStop;
		_prevClose = close;
		_prevLowStop = lowStop;

		if (!smaValue.IsFormed || prevClose is not decimal pc || prevLowStop is not decimal pl)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			if (close < highStop)
				SellMarket(Position);

			return;
		}

		if (Position == 0 && pc <= pl && close > lowStop && close > smaValue.GetValue<decimal>())
			BuyMarket(GetOrderVolume(lowestClose));
	}
}
