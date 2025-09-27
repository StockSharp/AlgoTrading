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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Blockbuster Bollinger breakout strategy converted from MetaTrader 4.
/// Enters on price excursions beyond Bollinger Bands with an additional offset.
/// Manages exits using point-based profit targets and stop-loss levels.
/// </summary>
public class BlockbusterBollingerStrategy : Strategy
{
	private readonly StrategyParam<decimal> _profitTargetPoints;
	private readonly StrategyParam<decimal> _lossLimitPoints;
	private readonly StrategyParam<decimal> _distancePoints;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private bool _isLongPosition;

	/// <summary>
	/// Profit target expressed in instrument points.
	/// </summary>
	public decimal ProfitTargetPoints
	{
		get => _profitTargetPoints.Value;
		set => _profitTargetPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in instrument points.
	/// </summary>
	public decimal LossLimitPoints
	{
		get => _lossLimitPoints.Value;
		set => _lossLimitPoints.Value = value;
	}

	/// <summary>
	/// Additional offset from the Bollinger band in points before entering a trade.
	/// </summary>
	public decimal DistancePoints
	{
		get => _distancePoints.Value;
		set => _distancePoints.Value = value;
	}

	/// <summary>
	/// Period of the Bollinger Bands indicator.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Deviation multiplier of the Bollinger Bands indicator.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Trading volume for new entries.
	/// </summary>
	public decimal TradeVolume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public BlockbusterBollingerStrategy()
	{
		_profitTargetPoints = Param(nameof(ProfitTargetPoints), 3m)
			.SetDisplay("Profit Target", "Target profit in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 20m, 1m);

		_lossLimitPoints = Param(nameof(LossLimitPoints), 20m)
			.SetDisplay("Loss Limit", "Stop-loss distance in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(5m, 50m, 5m);

		_distancePoints = Param(nameof(DistancePoints), 3m)
			.SetDisplay("Band Offset", "Extra distance beyond the band in points", "Signals")
			.SetCanOptimize(true)
			.SetOptimize(0m, 10m, 1m);

		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetDisplay("Bollinger Period", "Number of bars for Bollinger Bands", "Signals")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetDisplay("Bollinger Deviation", "Standard deviation multiplier", "Signals")
			.SetGreaterThanZero()
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_volume = Param(nameof(TradeVolume), 1m)
			.SetDisplay("Volume", "Trade volume for entries", "Orders")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used by the strategy", "General");
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

		_entryPrice = 0m;
		_isLongPosition = false;
		Volume = TradeVolume;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var bollinger = (BollingerBandsValue)bollingerValue;

		if (bollinger.UpBand is not decimal upperBand || bollinger.LowBand is not decimal lowerBand)
			return;

		var pointSize = GetPointSize();
		var offset = DistancePoints > 0m ? DistancePoints * pointSize : 0m;

		ManageOpenPosition(candle.ClosePrice, pointSize);

		var buySignal = candle.ClosePrice < lowerBand - offset;
		var sellSignal = candle.ClosePrice > upperBand + offset;

		if (buySignal && Position <= 0)
		{
			_entryPrice = candle.ClosePrice;
			_isLongPosition = true;
			BuyMarket(TradeVolume + Math.Abs(Position));
			LogInfo($"Buy signal. Close={candle.ClosePrice}, Lower band={lowerBand}, Offset={offset}");
		}
		else if (sellSignal && Position >= 0)
		{
			_entryPrice = candle.ClosePrice;
			_isLongPosition = false;
			SellMarket(TradeVolume + Math.Abs(Position));
			LogInfo($"Sell signal. Close={candle.ClosePrice}, Upper band={upperBand}, Offset={offset}");
		}

		ManageOpenPosition(candle.ClosePrice, pointSize);
	}

	private void ManageOpenPosition(decimal currentPrice, decimal pointSize)
	{
		if (Position == 0 || _entryPrice == 0m)
			return;

		if (pointSize <= 0m)
			pointSize = 1m;

		var profitPoints = _isLongPosition
			? (currentPrice - _entryPrice) / pointSize
			: (_entryPrice - currentPrice) / pointSize;

		if (ProfitTargetPoints > 0m && profitPoints >= ProfitTargetPoints)
		{
			ExitPosition();
			LogInfo($"Take profit triggered. Price={currentPrice}, Points={profitPoints:F2}");
			return;
		}

		if (LossLimitPoints > 0m && profitPoints <= -LossLimitPoints)
		{
			ExitPosition();
			LogInfo($"Stop loss triggered. Price={currentPrice}, Points={profitPoints:F2}");
		}
	}

	private void ExitPosition()
	{
		if (Position > 0)
		{
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		_entryPrice = 0m;
		_isLongPosition = false;
	}

	private decimal GetPointSize()
	{
		var step = Security?.PriceStep;
		return step is > 0m ? step.Value : 1m;
	}
}

