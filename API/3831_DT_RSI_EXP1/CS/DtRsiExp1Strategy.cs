using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the DT-RSI-EXP1 MetaTrader 4 expert advisor.
/// </summary>
public class DtRsiExp1Strategy : Strategy
{

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _trendCandleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;

	private readonly StrategyParam<int> _maxHistory;
	private decimal[] _rsiHistory = Array.Empty<decimal>();
	private int _historyCount;

	private decimal? _trendFilterValue;
	private decimal? _previousClose;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private decimal? _trailingStopPrice;
	private bool _isLongPosition;

	/// <summary>
	/// Initializes a new instance of <see cref="DtRsiExp1Strategy"/>.
	/// </summary>
	public DtRsiExp1Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Primary candles", "Time frame used for RSI calculations", "General");

		_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromMinutes(240).TimeFrame())
			.SetDisplay("Trend candles", "Higher time frame for the trend filter", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 47)
			.SetGreaterThanZero()
			.SetDisplay("RSI period", "Number of candles used by RSI", "Parameters");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 76m)
			.SetDisplay("Take profit", "Distance to the take profit in price steps", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 26m)
			.SetDisplay("Stop loss", "Distance to the stop loss in price steps", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 0m)
			.SetDisplay("Trailing stop", "Trailing distance in price steps (0 disables)", "Risk");
		_maxHistory = Param(nameof(MaxHistory), 503)
			.SetDisplay("RSI history", "Maximum number of stored RSI values", "Parameters")
			.SetRange(4, 2000);
	}

	/// <summary>
	/// Candle type that feeds the RSI logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Higher time frame used by the trend filter.
	/// </summary>
	public DataType TrendCandleType
	{
		get => _trendCandleType.Value;
		set => _trendCandleType.Value = value;
	}

	/// <summary>
	/// Relative Strength Index lookback.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in price steps.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}
	/// <summary>
	/// Maximum number of RSI values stored for pattern detection.
	/// </summary>
	public int MaxHistory
	{
		get => _maxHistory.Value;
		set => _maxHistory.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, TrendCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		var capacity = MaxHistory;
		if (_rsiHistory.Length != capacity)
			_rsiHistory = new decimal[capacity];
		else
			Array.Clear(_rsiHistory, 0, _rsiHistory.Length);
		_historyCount = 0;
		_trendFilterValue = null;
		_previousClose = null;
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_trailingStopPrice = null;
		_isLongPosition = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		var trendMa = new ExponentialMovingAverage
		{
			Length = 10
		};

		var rsiSubscription = SubscribeCandles(CandleType);
		rsiSubscription
			.Bind(rsi, ProcessPrimaryCandle)
			.Start();

		var trendSubscription = SubscribeCandles(TrendCandleType);
		trendSubscription
			.Bind(trendMa, ProcessTrendCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, rsiSubscription);
			DrawIndicator(area, rsi);
		}
	}

	private void ProcessTrendCandle(ICandleMessage candle, decimal trendValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Store the trend filter value from the higher time frame EMA.
		_trendFilterValue = trendValue;
	}

	private void ProcessPrimaryCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateRsiHistory(rsiValue);

		if (_historyCount < 4)
		{
			_previousClose = candle.ClosePrice;
			return;
		}

		var rsi0 = _rsiHistory[0];
		var rsi1 = _rsiHistory[1];
		var rsi2 = _rsiHistory[2];
		var peaks = new decimal?[500];
		var troughs = new decimal?[500];

		var limit = Math.Min(500, _historyCount - 3);
		for (var i = 0; i < limit; i++)
		{
			var prev = _rsiHistory[i + 1];
			var middle = _rsiHistory[i + 2];
			var next = _rsiHistory[i + 3];

			if (prev < middle && middle >= next)
				peaks[i + 2] = middle;

			if (prev > middle && middle <= next)
				troughs[i + 2] = middle;
		}

		var buySignal = TryBuildBuySignal(peaks, troughs, rsi0, rsi1, rsi2);
		var sellSignal = TryBuildSellSignal(peaks, troughs, rsi0, rsi1, rsi2);

		if (Position == 0)
		{
			if (buySignal)
			{
				BuyMarket();
				InitializeLongProtection(candle);
			}
			else if (sellSignal)
			{
				SellMarket();
				InitializeShortProtection(candle);
			}
		}
		else
		{
			ManageOpenPosition(candle, rsi0);
		}

		_previousClose = candle.ClosePrice;
	}

	private bool TryBuildBuySignal(decimal?[] peaks, decimal?[] troughs, decimal rsi0, decimal rsi1, decimal rsi2)
	{
		if (_trendFilterValue is not decimal trend || _previousClose is not decimal prevClose)
			return false;

		decimal? vol1 = null;
		decimal? vol2 = null;
		var pos1 = -1;
		var pos2 = -1;

		for (var i = 0; i < peaks.Length; i++)
		{
			if (peaks[i] is not decimal value)
				continue;

			if (value > 40m && vol1 is null)
			{
				vol1 = value;
				pos1 = i;
				continue;
			}

			if (value > 60m && vol1 is not null && value > vol1)
			{
				vol2 = value;
				pos2 = i;
				break;
			}
		}

		if (vol1 is null || vol2 is null || pos2 <= pos1)
			return false;

		for (var i = 0; i < pos2; i++)
		{
			if (troughs[i] is decimal trough && trough < 40m)
			{
				vol1 = null;
				vol2 = null;
				break;
			}
		}

		if (vol1 is null || vol2 is null)
			return false;

		decimal? volUp = null;
		decimal? volUp1 = null;

		if (pos2 > pos1)
		{
			var denominator = pos2 - pos1;
			if (denominator != 0)
			{
				var diff = vol1.Value - vol2.Value;
				volUp = vol1 + pos1 * diff / denominator;
				volUp1 = vol1 + (pos1 - 1) * diff / denominator;
			}
		}

		if (volUp is null || volUp1 is null)
			return false;

		var trendDown = trend < prevClose;
		var rsiCrossUp = rsi1 > volUp && rsi2 < volUp1;
		var neutralZone = rsi2 < 50m && rsi0 < 55m;

		return trendDown && rsiCrossUp && neutralZone && HasFreshSellStructure(troughs, pos2);
	}

	private bool TryBuildSellSignal(decimal?[] peaks, decimal?[] troughs, decimal rsi0, decimal rsi1, decimal rsi2)
	{
		if (_trendFilterValue is not decimal trend || _previousClose is not decimal prevClose)
			return false;

		decimal? vol3 = null;
		decimal? vol4 = null;
		var pos3 = -1;
		var pos4 = -1;

		for (var i = 0; i < troughs.Length; i++)
		{
			if (troughs[i] is not decimal value)
				continue;

			if (value < 60m && vol3 is null)
			{
				vol3 = value;
				pos3 = i;
				continue;
			}

			if (value < 40m && vol3 is not null && value < vol3)
			{
				vol4 = value;
				pos4 = i;
				break;
			}
		}

		if (vol3 is null || vol4 is null || pos4 <= pos3)
			return false;

		for (var i = 0; i < pos4; i++)
		{
			if (peaks[i] is decimal peak && peak > 60m)
			{
				vol3 = null;
				vol4 = null;
				break;
			}
		}

		if (vol3 is null || vol4 is null)
			return false;

		decimal? volDown = null;
		decimal? volDown1 = null;

		var denominator = pos4 - pos3;
		if (denominator != 0)
		{
			var diff = vol3.Value - vol4.Value;
			volDown = vol3 + pos3 * diff / denominator;
			volDown1 = vol3 + (pos3 - 1) * diff / denominator;
		}

		if (volDown is null || volDown1 is null)
			return false;

		var trendUp = trend > prevClose;
		var rsiCrossDown = rsi1 < volDown && rsi2 > volDown1;
		var neutralZone = rsi2 > 50m && rsi0 > 47m;

		return trendUp && rsiCrossDown && neutralZone && HasFreshBuyStructure(peaks, pos4);
	}

	private static bool HasFreshSellStructure(decimal?[] troughs, int pos2)
	{
		for (var i = 0; i < pos2; i++)
		{
			if (troughs[i] is decimal value && value < 40m)
				return false;
		}

		return true;
	}

	private static bool HasFreshBuyStructure(decimal?[] peaks, int pos4)
	{
		for (var i = 0; i < pos4; i++)
		{
			if (peaks[i] is decimal value && value > 60m)
				return false;
		}

		return true;
	}

	private void InitializeLongProtection(ICandleMessage candle)
	{
		_isLongPosition = true;
		_entryPrice = candle.ClosePrice;
		var stepStop = ConvertPoints(StopLossPoints);
		var stepTp = ConvertPoints(TakeProfitPoints);
		_stopPrice = candle.ClosePrice - stepStop;
		_takeProfitPrice = candle.ClosePrice + stepTp;
		_trailingStopPrice = null;
	}

	private void InitializeShortProtection(ICandleMessage candle)
	{
		_isLongPosition = false;
		_entryPrice = candle.ClosePrice;
		var stepStop = ConvertPoints(StopLossPoints);
		var stepTp = ConvertPoints(TakeProfitPoints);
		_stopPrice = candle.ClosePrice + stepStop;
		_takeProfitPrice = candle.ClosePrice - stepTp;
		_trailingStopPrice = null;
	}

	private void ManageOpenPosition(ICandleMessage candle, decimal rsi0)
	{
		if (Position > 0 && _isLongPosition)
		{
			if (rsi0 > 70m)
			{
				SellMarket(Position);
				ResetProtection();
				return;
			}

			if (_takeProfitPrice is decimal tp && candle.HighPrice >= tp)
			{
				SellMarket(Position);
				ResetProtection();
				return;
			}

			if (_stopPrice is decimal sl && candle.LowPrice <= sl)
			{
				SellMarket(Position);
				ResetProtection();
				return;
			}

			UpdateTrailingForLong(candle);
		}
		else if (Position < 0 && !_isLongPosition)
		{
			if (rsi0 < 30m)
			{
				BuyMarket(Math.Abs(Position));
				ResetProtection();
				return;
			}

			if (_takeProfitPrice is decimal tp && candle.LowPrice <= tp)
			{
				BuyMarket(Math.Abs(Position));
				ResetProtection();
				return;
			}

			if (_stopPrice is decimal sl && candle.HighPrice >= sl)
			{
				BuyMarket(Math.Abs(Position));
				ResetProtection();
				return;
			}

			UpdateTrailingForShort(candle);
		}
	}

	private void UpdateTrailingForLong(ICandleMessage candle)
	{
		if (TrailingStopPoints <= 0m || _entryPrice is not decimal entry)
			return;

		var trailingDistance = ConvertPoints(TrailingStopPoints);
		var profit = candle.ClosePrice - entry;
		if (profit <= trailingDistance)
			return;

		var candidate = candle.ClosePrice - trailingDistance;
		_trailingStopPrice = _trailingStopPrice is decimal stop
			? Math.Max(stop, candidate)
			: Math.Max(_stopPrice ?? candidate, candidate);

		if (_trailingStopPrice is decimal trailing && candle.LowPrice <= trailing)
		{
			SellMarket(Position);
			ResetProtection();
		}
	}

	private void UpdateTrailingForShort(ICandleMessage candle)
	{
		if (TrailingStopPoints <= 0m || _entryPrice is not decimal entry)
			return;

		var trailingDistance = ConvertPoints(TrailingStopPoints);
		var profit = entry - candle.ClosePrice;
		if (profit <= trailingDistance)
			return;

		var candidate = candle.ClosePrice + trailingDistance;
		_trailingStopPrice = _trailingStopPrice is decimal stop
			? Math.Min(stop, candidate)
			: Math.Min(_stopPrice ?? candidate, candidate);

		if (_trailingStopPrice is decimal trailing && candle.HighPrice >= trailing)
		{
			BuyMarket(Math.Abs(Position));
			ResetProtection();
		}
	}

	private void ResetProtection()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
		_trailingStopPrice = null;
		_isLongPosition = false;
	}

	private void UpdateRsiHistory(decimal value)
	{
		var limit = Math.Min(_historyCount, MaxHistory - 1);
		for (var i = limit; i >= 1; i--)
			_rsiHistory[i] = _rsiHistory[i - 1];

		_rsiHistory[0] = value;

		if (_historyCount < MaxHistory)
			_historyCount++;
	}

	private decimal ConvertPoints(decimal points)
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? points * step : points;
	}
}
