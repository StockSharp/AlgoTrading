using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Variable Index Dynamic Average crossover strategy converted from the "iVIDyA Simple" MetaTrader expert.
/// </summary>
public class IvidyaSimpleStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _cmoPeriod;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<AppliedPriceType> _appliedPrice;
	private readonly StrategyParam<DataType> _candleType;

	private ChandeMomentumOscillator _cmo = null!;
	private readonly List<decimal> _vidyaHistory = new();

	/// <summary>
	/// Trading volume used for entries.
	/// </summary>
	public decimal TradeVolume { get => _tradeVolume.Value; set => _tradeVolume.Value = value; }

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public int StopLossPoints { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public int TakeProfitPoints { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }

	/// <summary>
	/// Chande Momentum Oscillator length that drives the adaptive smoothing.
	/// </summary>
	public int CmoPeriod { get => _cmoPeriod.Value; set => _cmoPeriod.Value = value; }

	/// <summary>
	/// EMA length that defines the base smoothing coefficient for VIDYA.
	/// </summary>
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }

	/// <summary>
	/// Number of completed candles used to shift the VIDYA line.
	/// </summary>
	public int MaShift { get => _maShift.Value; set => _maShift.Value = value; }

	/// <summary>
	/// Candle price used as input for the VIDYA calculation.
	/// </summary>
	public AppliedPriceType AppliedPrice { get => _appliedPrice.Value; set => _appliedPrice.Value = value; }

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="IvidyaSimpleStrategy"/>.
	/// </summary>
	public IvidyaSimpleStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Volume", "Trade volume", "Trading")
			.SetGreaterThanZero();

		_stopLossPoints = Param(nameof(StopLossPoints), 150)
			.SetDisplay("Stop Loss", "Stop-loss in points", "Risk")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 460)
			.SetDisplay("Take Profit", "Take-profit in points", "Risk")
			.SetCanOptimize(true);

		_cmoPeriod = Param(nameof(CmoPeriod), 15)
			.SetDisplay("CMO Period", "Length of the Chande Momentum Oscillator", "Indicator")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_emaPeriod = Param(nameof(EmaPeriod), 12)
			.SetDisplay("EMA Period", "Base EMA length used by VIDYA", "Indicator")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_maShift = Param(nameof(MaShift), 1)
			.SetDisplay("MA Shift", "Number of completed candles for VIDYA shift", "Indicator")
			.SetNotNegative()
			.SetCanOptimize(true);

		_appliedPrice = Param(nameof(AppliedPrice), AppliedPriceType.Close)
			.SetDisplay("Applied Price", "Price source for VIDYA", "Indicator")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_cmo = new ChandeMomentumOscillator { Length = CmoPeriod };
		_vidyaHistory.Clear();

		StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_cmo, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal cmoValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var appliedPrice = GetAppliedPrice(candle);
		var alpha = 2m / (EmaPeriod + 1m);
		var momentumFactor = Math.Abs(cmoValue) / 100m;
		var smoothingFactor = alpha * momentumFactor;

		var previousVidya = _vidyaHistory.Count == 0 ? appliedPrice : _vidyaHistory[^1];
		var currentVidya = smoothingFactor * appliedPrice + (1m - smoothingFactor) * previousVidya;

		_vidyaHistory.Add(currentVidya);
		TrimHistory();

		if (!TryGetShiftedVidya(out var shiftedVidya))
			return;

		TryTrade(candle, shiftedVidya);
	}

	private void TryTrade(ICandleMessage candle, decimal vidya)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var open = candle.OpenPrice;
		var close = candle.ClosePrice;

		var crossUp = open < vidya && close > vidya;
		var crossDown = open > vidya && close < vidya;

		if (crossUp && Position <= 0)
		{
			var volume = TradeVolume + (Position < 0 ? -Position : 0m);

			if (volume > 0m)
			{
				BuyMarket(volume);
				ApplyProtection(close, Position + volume);
			}
		}
		else if (crossDown && Position >= 0)
		{
			var volume = TradeVolume + (Position > 0 ? Position : 0m);

			if (volume > 0m)
			{
				SellMarket(volume);
				ApplyProtection(close, Position - volume);
			}
		}
	}

	private void ApplyProtection(decimal referencePrice, decimal resultingPosition)
	{
		if (StopLossPoints > 0)
			SetStopLoss(StopLossPoints, referencePrice, resultingPosition);

		if (TakeProfitPoints > 0)
			SetTakeProfit(TakeProfitPoints, referencePrice, resultingPosition);
	}

	private bool TryGetShiftedVidya(out decimal vidya)
	{
		var index = _vidyaHistory.Count - 1 - MaShift;

		if (index < 0)
		{
			vidya = default;
			return false;
		}

		vidya = _vidyaHistory[index];
		return true;
	}

	private void TrimHistory()
	{
		var maxSize = Math.Max(2, MaShift + 2);

		while (_vidyaHistory.Count > maxSize)
			_vidyaHistory.RemoveAt(0);
	}

	private decimal GetAppliedPrice(ICandleMessage candle)
	{
		return AppliedPrice switch
		{
			AppliedPriceType.Open => candle.OpenPrice,
			AppliedPriceType.High => candle.HighPrice,
			AppliedPriceType.Low => candle.LowPrice,
			AppliedPriceType.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceType.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceType.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}

	/// <summary>
	/// Price sources compatible with MetaTrader applied price options.
	/// </summary>
	public enum AppliedPriceType
	{
		Close,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted,
	}
}
