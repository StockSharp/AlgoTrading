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
/// Port of the MetaTrader expert advisor Basic ATR stop_take expert adviser.
/// Opens a single directional position and protects it with ATR-based stop-loss and take-profit levels.
/// </summary>
public class BasicAtrStopTakeStrategy : Strategy
{
	private readonly StrategyParam<TradeDirections> _tradeDirection;
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _stopLossFactor;
	private readonly StrategyParam<decimal> _takeProfitFactor;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr = null!;

	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _targetPrice;
	private bool _hasStopLevel;
	private bool _hasTakeLevel;

	/// <summary>
	/// Trading direction copied from the original expert advisor.
	/// </summary>
	public enum TradeDirections
	{
		/// <summary>
		/// Do not open new positions.
		/// </summary>
		None,

		/// <summary>
		/// Long-only mode.
		/// </summary>
		Buy,

		/// <summary>
		/// Short-only mode.
		/// </summary>
		Sell,
	}

	/// <summary>
	/// Selected market side for the ATR-protected trade.
	/// </summary>
	public TradeDirections Direction
	{
		get => _tradeDirection.Value;
		set => _tradeDirection.Value = value;
	}

	/// <summary>
	/// Order volume sent to the market when conditions allow a new trade.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <summary>
	/// Lookback period of the Average True Range indicator.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier applied to ATR to determine the protective stop distance.
	/// </summary>
	public decimal StopLossFactor
	{
		get => _stopLossFactor.Value;
		set => _stopLossFactor.Value = value;
	}

	/// <summary>
	/// Multiplier applied to ATR to determine the profit target distance.
	/// </summary>
	public decimal TakeProfitFactor
	{
		get => _takeProfitFactor.Value;
		set => _takeProfitFactor.Value = value;
	}

	/// <summary>
	/// Candle type that drives ATR calculation and trade management.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes the ATR stop/take strategy parameters.
	/// </summary>
	public BasicAtrStopTakeStrategy()
	{
		_tradeDirection = Param(nameof(Direction), TradeDirections.Buy)
			.SetDisplay("Trade Direction", "Market side used for the ATR trade", "Trading");

		_tradeVolume = Param(nameof(TradeVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order volume for the single entry", "Trading");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Lookback period for the Average True Range", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 1);

		_stopLossFactor = Param(nameof(StopLossFactor), 1.5m)
			.SetNotNegative()
			.SetDisplay("Stop Factor", "ATR multiplier applied to the stop-loss", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3.0m, 0.5m);

		_takeProfitFactor = Param(nameof(TakeProfitFactor), 2.0m)
			.SetNotNegative()
			.SetDisplay("Take Factor", "ATR multiplier applied to the take-profit", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 4.0m, 0.5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for ATR and trade management", "General");
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
		ResetTradeLevels();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange
		{
			Length = AtrPeriod,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_atr, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_atr.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0m)
		{
			if (_hasStopLevel && candle.LowPrice <= _stopPrice)
			{
				SellMarket(Position);
				ResetTradeLevels();
				return;
			}

			if (_hasTakeLevel && candle.HighPrice >= _targetPrice)
			{
				SellMarket(Position);
				ResetTradeLevels();
				return;
			}
		}
		else if (Position < 0m)
		{
			var volume = Math.Abs(Position);

			if (_hasStopLevel && candle.HighPrice >= _stopPrice)
			{
				BuyMarket(volume);
				ResetTradeLevels();
				return;
			}

			if (_hasTakeLevel && candle.LowPrice <= _targetPrice)
			{
				BuyMarket(volume);
				ResetTradeLevels();
				return;
			}
		}
		else
		{
			ResetTradeLevels();

			if (Direction == TradeDirections.None)
				return;

			if (atrValue <= 0m)
				return;

			var volume = TradeVolume;
			if (volume <= 0m)
				return;

			switch (Direction)
			{
				case TradeDirections.Buy:
				{
					BuyMarket(volume);
					_entryPrice = candle.ClosePrice;
					_stopPrice = _entryPrice - atrValue * StopLossFactor;
					_targetPrice = _entryPrice + atrValue * TakeProfitFactor;
					break;
				}

				case TradeDirections.Sell:
				{
					SellMarket(volume);
					_entryPrice = candle.ClosePrice;
					_stopPrice = _entryPrice + atrValue * StopLossFactor;
					_targetPrice = _entryPrice - atrValue * TakeProfitFactor;
					break;
				}
			}

			_hasStopLevel = StopLossFactor > 0m;
			_hasTakeLevel = TakeProfitFactor > 0m;

			if (!_hasStopLevel && !_hasTakeLevel)
			{
				ResetTradeLevels();
			}
		}
	}

	private void ResetTradeLevels()
	{
		_entryPrice = 0m;
		_stopPrice = 0m;
		_targetPrice = 0m;
		_hasStopLevel = false;
		_hasTakeLevel = false;
	}
}

