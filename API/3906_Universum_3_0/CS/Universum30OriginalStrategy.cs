using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Universum 3.0 strategy converted from the original MQL4 expert.
/// </summary>
public class Universum30OriginalStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _demarkerPeriod;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<int> _lossesLimit;
	private readonly StrategyParam<bool> _fastOptimize;

	private DeMarker? _deMarker;
	private decimal _pointValue;
	private decimal _takeProfitOffset;
	private decimal _stopLossOffset;
	private decimal _spreadPoints;
	private decimal _currentVolume;
	private int _consecutiveLosses;
	private decimal _lastPnL;

	/// <summary>
	/// Candle type for indicator processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// DeMarker indicator period.
	/// </summary>
	public int DemarkerPeriod
	{
		get => _demarkerPeriod.Value;
		set => _demarkerPeriod.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Base trading volume used after profitable trades.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Maximum allowed number of consecutive losing trades.
	/// </summary>
	public int LossesLimit
	{
		get => _lossesLimit.Value;
		set => _lossesLimit.Value = value;
	}

	/// <summary>
	/// When true disables adaptive volume calculation for faster optimisation.
	/// </summary>
	public bool FastOptimize
	{
		get => _fastOptimize.Value;
		set => _fastOptimize.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters to match the original expert.
	/// </summary>
	public Universum30OriginalStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for indicator calculations", "Data");

		_demarkerPeriod = Param(nameof(DemarkerPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("DeMarker Period", "Length of the DeMarker oscillator", "Indicator");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Target profit distance in points", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Maximum loss distance in points", "Risk");

		_baseVolume = Param(nameof(BaseVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Volume", "Initial lot size used after profitable trades", "Trading");

		_lossesLimit = Param(nameof(LossesLimit), 1_000_000)
			.SetGreaterThanZero()
			.SetDisplay("Losses Limit", "Maximum consecutive losses before the strategy stops", "Risk");

		_fastOptimize = Param(nameof(FastOptimize), true)
			.SetDisplay("Fast Optimisation", "Disable martingale sizing during optimisation runs", "Trading");
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

		_deMarker = null;
		_pointValue = 0m;
		_takeProfitOffset = 0m;
		_stopLossOffset = 0m;
		_spreadPoints = 0m;
		_currentVolume = 0m;
		_consecutiveLosses = 0;
		_lastPnL = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		UpdatePointValue();

		_currentVolume = BaseVolume;
		_consecutiveLosses = 0;
		_lastPnL = 0m;

		StartProtection(
			takeProfit: new Unit(_takeProfitOffset, UnitTypes.Absolute),
			stopLoss: new Unit(_stopLossOffset, UnitTypes.Absolute));

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		_deMarker = new DeMarker
		{
			Length = DemarkerPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_deMarker, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _deMarker);
			DrawOwnTrades(area);
		}
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (_pointValue <= 0m)
			return;

		if (!message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj) ||
			!message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj))
		{
			return;
		}

		if (bidObj is not decimal bid || askObj is not decimal ask)
			return;

		if (ask <= bid || bid <= 0m)
			return;

		_spreadPoints = (ask - bid) / _pointValue;
	}

	private void ProcessCandle(ICandleMessage candle, decimal demarkerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0)
			return;

		if (_consecutiveLosses >= LossesLimit)
		{
			LogInfo("Loss limit reached ({_consecutiveLosses}). Stopping strategy.");
			Stop();
			return;
		}

		var volume = FastOptimize ? BaseVolume : _currentVolume;
		volume = NormalizeVolume(volume);

		if (volume <= 0m)
			return;

		if (demarkerValue > 0.5m)
		{
			BuyMarket(volume);
		}
		else
		{
			SellMarket(volume);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position != 0)
			return;

		var tradePnL = PnL - _lastPnL;
		_lastPnL = PnL;

		if (tradePnL > 0m)
		{
			_currentVolume = BaseVolume;
			_consecutiveLosses = 0;
			return;
		}

		if (tradePnL < 0m)
		{
			_consecutiveLosses++;

			if (_consecutiveLosses >= LossesLimit)
			{
				LogInfo("Loss limit reached ({_consecutiveLosses}). Stopping strategy.");
				Stop();
				return;
			}

			if (!FastOptimize)
			{
				var multiplier = CalculateVolumeMultiplier();
				if (multiplier > 0m)
				{
					_currentVolume = NormalizeVolume(_currentVolume * multiplier);
				}
			}
		}
	}

	private void UpdatePointValue()
	{
		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
			priceStep = 0.0001m;

		var decimals = Security?.Decimals ?? 0;
		var digitsAdjust = (decimals == 3 || decimals == 5) ? 10m : 1m;

		_pointValue = priceStep * digitsAdjust;
		_takeProfitOffset = TakeProfitPoints * _pointValue;
		_stopLossOffset = StopLossPoints * _pointValue;
	}

	private decimal CalculateVolumeMultiplier()
	{
		var spread = Math.Max(0m, _spreadPoints);
		var denominator = TakeProfitPoints - spread;
		if (denominator <= 0m)
			return 1m;

		var numerator = TakeProfitPoints + StopLossPoints;
		if (numerator <= 0m)
			return 1m;

		return numerator / denominator;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (volume <= 0m)
			return 0m;

		var minVolume = Security?.MinVolume ?? 0m;
		var maxVolume = Security?.MaxVolume ?? 0m;
		var step = Security?.VolumeStep ?? 0m;

		if (minVolume > 0m && volume < minVolume)
			volume = minVolume;

		if (step > 0m)
		{
			var steps = Math.Ceiling(volume / step);
			volume = steps * step;
		}

		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume;

		return volume;
	}
}
