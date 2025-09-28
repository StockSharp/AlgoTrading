namespace StockSharp.Samples.Strategies;

using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

using StockSharp.Algo;

/// <summary>
/// Moving average crossover strategy with martingale money management converted from the MT5 "MovingAverageMartinGale" expert advisor.
/// Scales trade volume and protective distances after losses while resetting to the base configuration after profitable trades.
/// </summary>
public class MovingAverageMartingaleStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _startingVolume;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<decimal> _volumeMultiplier;
	private readonly StrategyParam<decimal> _targetMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private SMA _sma;
	private decimal? _previousClose;
	private decimal? _previousMa;
	private decimal? _currentClose;
	private decimal? _currentMa;
	private decimal _pipSize;

	private decimal _currentVolume;
	private decimal _currentTakeProfitPoints;
	private decimal _currentStopLossPoints;
	private decimal _lastRealizedPnL;
	private decimal _previousPosition;
	private decimal _lastTradeResult;

	/// <summary>
	/// Initializes a new instance of the <see cref="MovingAverageMartingaleStrategy"/> class.
	/// </summary>
	public MovingAverageMartingaleStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA period", "Length of the simple moving average used for entries.", "Indicator")
			.SetCanOptimize(true);

		_startingVolume = Param(nameof(StartingVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Starting volume", "Base order volume used after profitable trades.", "Money management")
			.SetCanOptimize(true);

		_maxVolume = Param(nameof(MaxVolume), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Maximum volume", "Upper limit for martingale scaling.", "Money management")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 100)
			.SetNotNegative()
			.SetDisplay("Take profit (points)", "Initial profit target distance expressed in MetaTrader points.", "Risk")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 300)
			.SetNotNegative()
			.SetDisplay("Stop loss (points)", "Initial stop-loss distance expressed in MetaTrader points.", "Risk")
			.SetCanOptimize(true);

		_volumeMultiplier = Param(nameof(VolumeMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Volume multiplier", "Factor applied to the next trade volume after a loss.", "Money management")
			.SetCanOptimize(true);

		_targetMultiplier = Param(nameof(TargetMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Target multiplier", "Factor applied to stop-loss and take-profit distances after a loss.", "Money management")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Primary timeframe processed by the strategy.", "General");
	}

	/// <summary>
	/// Moving average period used for generating signals.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Base position volume restored after profitable trades.
	/// </summary>
	public decimal StartingVolume
	{
		get => _startingVolume.Value;
		set => _startingVolume.Value = value;
	}

	/// <summary>
	/// Maximum position volume allowed by the martingale logic.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Initial take-profit distance expressed in MetaTrader points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Initial stop-loss distance expressed in MetaTrader points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Multiplier applied to the trade volume after a losing trade.
	/// </summary>
	public decimal VolumeMultiplier
	{
		get => _volumeMultiplier.Value;
		set => _volumeMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier applied to stop-loss and take-profit distances after a losing trade.
	/// </summary>
	public decimal TargetMultiplier
	{
		get => _targetMultiplier.Value;
		set => _targetMultiplier.Value = value;
	}

	/// <summary>
	/// Candle type used to read market data.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_sma = null;
		_previousClose = null;
		_previousMa = null;
		_currentClose = null;
		_currentMa = null;
		_pipSize = 0m;

		_currentVolume = 0m;
		_currentTakeProfitPoints = 0m;
		_currentStopLossPoints = 0m;
		_lastRealizedPnL = 0m;
		_previousPosition = 0m;
		_lastTradeResult = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		_currentVolume = NormalizeVolume(StartingVolume);
		_currentTakeProfitPoints = TakeProfitPoints;
		_currentStopLossPoints = StopLossPoints;
		_lastRealizedPnL = PnL;
		_previousPosition = Position;
		_lastTradeResult = 0m;

		_sma = new SMA { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_sma, ProcessCandle)
			.Start();
	}

	/// <inheritdoc />
	protected override void OnPositionReceived(Position position)
	{
		base.OnPositionReceived(position);

		if (_previousPosition != 0m && Position == 0m)
		{
			var tradePnL = PnL - _lastRealizedPnL;
			_lastRealizedPnL = PnL;
			_lastTradeResult = tradePnL;
		}

		_previousPosition = Position;
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_currentClose is null)
		{
			_currentClose = candle.ClosePrice;
			_currentMa = maValue;
			return;
		}

		if (_previousClose is null)
		{
			_previousClose = _currentClose;
			_previousMa = _currentMa;
			_currentClose = candle.ClosePrice;
			_currentMa = maValue;
			return;
		}

		if (_sma?.IsFormed != true)
		{
			_previousClose = _currentClose;
			_previousMa = _currentMa;
			_currentClose = candle.ClosePrice;
			_currentMa = maValue;
			return;
		}

		var previousClose = _previousClose.Value;
		var previousMa = _previousMa!.Value;
		var currentClose = _currentClose.Value;
		var currentMa = _currentMa!.Value;

		var crossedBelowPrice = previousMa < previousClose && currentMa > currentClose;
		var crossedAbovePrice = previousMa > previousClose && currentMa < currentClose;

		if (Position == 0m && (crossedBelowPrice || crossedAbovePrice))
		{
			ApplyMartingaleAdjustments();
		}

		if (Position == 0m)
		{
			var volume = NormalizeVolume(_currentVolume);

			if (volume <= 0m)
			{
				ShiftBuffers(candle, maValue);
				return;
			}

			if (crossedBelowPrice)
			{
				ApplyProtection();
				SellMarket(volume);
			}
			else if (crossedAbovePrice)
			{
				ApplyProtection();
				BuyMarket(volume);
			}
		}

		ShiftBuffers(candle, maValue);
	}

	private void ApplyMartingaleAdjustments()
	{
		if (_lastTradeResult < 0m)
		{
			var nextVolume = Math.Min(_currentVolume * VolumeMultiplier, MaxVolume);
			_currentVolume = NormalizeVolume(nextVolume);

			_currentTakeProfitPoints = Math.Min(_currentTakeProfitPoints * TargetMultiplier, 100000m);
			_currentStopLossPoints = Math.Min(_currentStopLossPoints * TargetMultiplier, 100000m);
		}
		else if (_lastTradeResult > 0m)
		{
			_currentVolume = NormalizeVolume(StartingVolume);
			_currentTakeProfitPoints = TakeProfitPoints;
			_currentStopLossPoints = StopLossPoints;
		}

		_lastTradeResult = 0m;
	}

	private void ApplyProtection()
	{
		var stopDistance = _currentStopLossPoints > 0m ? _currentStopLossPoints * _pipSize : 0m;
		var takeDistance = _currentTakeProfitPoints > 0m ? _currentTakeProfitPoints * _pipSize : 0m;

		StartProtection(
			stopLoss: stopDistance > 0m ? new Unit(stopDistance, UnitTypes.Absolute) : null,
			takeProfit: takeDistance > 0m ? new Unit(takeDistance, UnitTypes.Absolute) : null);

		Volume = NormalizeVolume(_currentVolume);
	}

	private void ShiftBuffers(ICandleMessage candle, decimal maValue)
	{
		_previousClose = _currentClose;
		_previousMa = _currentMa;
		_currentClose = candle.ClosePrice;
		_currentMa = maValue;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var step = Security?.VolumeStep ?? 1m;
		if (step <= 0m)
			step = 1m;

		var minVolume = Security?.MinVolume ?? step;
		if (volume < minVolume)
			volume = minVolume;

		var multiplier = volume / step;
		var rounded = Math.Round(multiplier, MidpointRounding.AwayFromZero) * step;

		if (rounded < minVolume)
			rounded = minVolume;

		var maxVolume = Security?.MaxVolume;
		if (maxVolume is decimal max && rounded > max)
			rounded = max;

		rounded = Math.Min(rounded, MaxVolume);

		return Math.Max(rounded, step);
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			step = 1m;

		var decimals = Security?.Decimals ?? 0;
		if (decimals == 3 || decimals == 5)
			step *= 10m;

		return step;
	}
}

