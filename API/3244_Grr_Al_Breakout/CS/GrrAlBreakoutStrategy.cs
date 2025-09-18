using System;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the MetaTrader expert advisor grr-al.
/// Captures one breakout per candle once the price travels a configurable distance from the candle open.
/// Applies symmetric stop-loss and take-profit protections expressed in points.
/// </summary>
public class GrrAlBreakoutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<decimal> _riskFraction;
	private readonly StrategyParam<int> _deltaPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _pointSize;
	private DateTimeOffset? _currentCandleTime;
	private decimal _anchorPrice;
	private bool _hasTriggered;

	/// <summary>
	/// Base order volume used when <see cref="RiskFraction"/> is zero.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Fraction of the maximum available volume to risk per trade (0 disables risk-based sizing).
	/// </summary>
	public decimal RiskFraction
	{
		get => _riskFraction.Value;
		set => _riskFraction.Value = value;
	}

	/// <summary>
	/// Distance in points that price has to move away from the candle open to trigger an entry.
	/// </summary>
	public int DeltaPoints
	{
		get => _deltaPoints.Value;
		set => _deltaPoints.Value = value;
	}

	/// <summary>
	/// Protective stop size expressed in points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Profit target expressed in points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Candle type that defines the trading timeframe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="GrrAlBreakoutStrategy"/>.
	/// </summary>
	public GrrAlBreakoutStrategy()
	{
		_volume = Param(nameof(Volume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Base order volume", "Trading");

		_riskFraction = Param(nameof(RiskFraction), 0m)
		.SetGreaterOrEqualToZero()
		.SetLessOrEqualTo(1m)
		.SetDisplay("Risk Fraction", "Fraction of maximum volume used when sizing trades", "Trading");

		_deltaPoints = Param(nameof(DeltaPoints), 30)
		.SetGreaterThanZero()
		.SetDisplay("Breakout Distance", "Required distance from the candle open in points", "Logic")
		.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 100)
		.SetGreaterOrEqualToZero()
		.SetDisplay("Stop Loss", "Protective stop in points", "Risk")
		.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 700)
		.SetGreaterOrEqualToZero()
		.SetDisplay("Take Profit", "Profit target in points", "Risk")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
		.SetDisplay("Candle Type", "Timeframe used for anchoring the breakout", "Data");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		UpdatePointSize();

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State == CandleStates.Empty)
			return;

		if (_pointSize <= 0m)
		{
			UpdatePointSize();
			if (_pointSize <= 0m)
				return;
		}

		if (_currentCandleTime != candle.OpenTime)
		{
			_currentCandleTime = candle.OpenTime;
			_anchorPrice = candle.OpenPrice > 0m ? candle.OpenPrice : candle.ClosePrice;
			_hasTriggered = false;
		}

		if (_hasTriggered)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var anchor = _anchorPrice;
		if (anchor <= 0m)
			return;

		var lastPrice = candle.ClosePrice;
		if (lastPrice <= 0m)
			return;

		var deltaPrice = DeltaPoints * _pointSize;
		if (deltaPrice <= 0m)
			return;

		if (lastPrice - anchor >= deltaPrice)
		{
			ExecuteSell(lastPrice);
		}
		else if (anchor - lastPrice >= deltaPrice)
		{
			ExecuteBuy(lastPrice);
		}
	}

	private void ExecuteBuy(decimal price)
	{
		var volume = CalculateTradeVolume();
		if (volume <= 0m)
			return;

		var resultingPosition = Position + volume;
		BuyMarket(volume);
		_hasTriggered = true;

		if (resultingPosition > 0m)
			ApplyProtection(price, resultingPosition);
	}

	private void ExecuteSell(decimal price)
	{
		var volume = CalculateTradeVolume();
		if (volume <= 0m)
			return;

		var resultingPosition = Position - volume;
		SellMarket(volume);
		_hasTriggered = true;

		if (resultingPosition < 0m)
			ApplyProtection(price, resultingPosition);
	}

	private void ApplyProtection(decimal price, decimal resultingPosition)
	{
		if (TakeProfitPoints > 0)
			SetTakeProfit(TakeProfitPoints, price, resultingPosition);

		if (StopLossPoints > 0)
			SetStopLoss(StopLossPoints, price, resultingPosition);
	}

	private decimal CalculateTradeVolume()
	{
		var volume = Volume;

		if (RiskFraction > 0m)
		{
			var security = Security;
			if (security != null)
			{
				var max = security.VolumeMax ?? decimal.MaxValue;
				var riskVolume = max * RiskFraction;
				if (riskVolume > 0m)
					volume = Math.Min(volume, riskVolume);
			}
		}

		return NormalizeVolume(volume);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
			volume = step * Math.Round(volume / step, MidpointRounding.AwayFromZero);

		var min = security.VolumeMin ?? 0m;
		if (min > 0m && volume < min)
			return 0m;

		var max = security.VolumeMax ?? decimal.MaxValue;
		if (volume > max)
			volume = max;

		return volume;
	}

	private void UpdatePointSize()
	{
		var security = Security;
		if (security == null)
		{
			_pointSize = 0m;
			return;
		}

		var step = security.PriceStep ?? 0m;
		if (step > 0m)
		{
			_pointSize = step;
			return;
		}

		if (security.Decimals is int decimals && decimals > 0)
		{
			_pointSize = (decimal)Math.Pow(10, -decimals);
			return;
		}

		_pointSize = 0m;
	}
}

