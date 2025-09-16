using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Angry Bird scalping strategy.
/// Uses RSI and CCI indicators with a dynamic grid for averaging positions.
/// </summary>
public class AngryBirdScalpingStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<int> _defaultPips;
	private readonly StrategyParam<int> _depth;
	private readonly StrategyParam<decimal> _lotExponent;
	private readonly StrategyParam<int> _maxTrades;
	private readonly StrategyParam<decimal> _rsiMin;
	private readonly StrategyParam<decimal> _rsiMax;
	private readonly StrategyParam<decimal> _cciDrop;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lastOpenBuyPrice;
	private decimal _lastOpenSellPrice;
	private int _tradeCount;
	private bool _longTrade;
	private bool _shortTrade;
	private decimal _rsiValue;
	private decimal? _prevClose;

	/// <summary>
	/// Stop-loss in points.
	/// </summary>
	public int StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Take-profit in points.
	/// </summary>
	public int TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Minimal grid step in pips.
	/// </summary>
	public int DefaultPips { get => _defaultPips.Value; set => _defaultPips.Value = value; }

	/// <summary>
	/// Number of candles for high/low calculation.
	/// </summary>
	public int Depth { get => _depth.Value; set => _depth.Value = value; }

	/// <summary>
	/// Volume multiplier for subsequent orders.
	/// </summary>
	public decimal LotExponent { get => _lotExponent.Value; set => _lotExponent.Value = value; }

	/// <summary>
	/// Maximum number of averaging trades.
	/// </summary>
	public int MaxTrades { get => _maxTrades.Value; set => _maxTrades.Value = value; }

	/// <summary>
	/// RSI threshold to open short.
	/// </summary>
	public decimal RsiMin { get => _rsiMin.Value; set => _rsiMin.Value = value; }

	/// <summary>
	/// RSI threshold to open long.
	/// </summary>
	public decimal RsiMax { get => _rsiMax.Value; set => _rsiMax.Value = value; }

	/// <summary>
	/// CCI absolute value to force close.
	/// </summary>
	public decimal CciDrop { get => _cciDrop.Value; set => _cciDrop.Value = value; }

	/// <summary>
	/// Initial order volume.
	/// </summary>
	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }

	/// <summary>
	/// Working candle timeframe.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public AngryBirdScalpingStrategy()
	{
		_stopLoss = Param(nameof(StopLoss), 500)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(100, 1000, 100);

		_takeProfit = Param(nameof(TakeProfit), 20)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 10);

		_defaultPips = Param(nameof(DefaultPips), 12)
			.SetGreaterThanZero()
			.SetDisplay("Default Pips", "Minimal grid step in pips", "Grid")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_depth = Param(nameof(Depth), 24)
			.SetGreaterThanZero()
			.SetDisplay("Depth", "Bars for high/low calculation", "Grid");

		_lotExponent = Param(nameof(LotExponent), 1.62m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Exponent", "Volume multiplier for averaging", "Grid");

		_maxTrades = Param(nameof(MaxTrades), 10)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Maximum number of averaging orders", "Grid");

		_rsiMin = Param(nameof(RsiMin), 30m)
			.SetDisplay("RSI Min", "RSI threshold to sell", "Signals");

		_rsiMax = Param(nameof(RsiMax), 70m)
			.SetDisplay("RSI Max", "RSI threshold to buy", "Signals");

		_cciDrop = Param(nameof(CciDrop), 500m)
			.SetGreaterThanZero()
			.SetDisplay("CCI Drop", "CCI value to close positions", "Signals");

		_volume = Param(nameof(Volume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Initial order volume", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, TimeSpan.FromHours(1).TimeFrame())];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var cci = new CommodityChannelIndex { Length = 55 };
		var rsi = new RelativeStrengthIndex { Length = 14 };
		var highest = new Highest { Length = Depth };
		var lowest = new Lowest { Length = Depth };

		var mainSub = SubscribeCandles(CandleType);
		mainSub.Bind(cci, highest, lowest, ProcessMain).Start();

		var rsiSub = SubscribeCandles(TimeSpan.FromHours(1).TimeFrame());
		rsiSub.Bind(rsi, ProcessRsi).Start();

		StartProtection();
	}

	private void ProcessRsi(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_rsiValue = rsi;
	}

	private void ProcessMain(ICandleMessage candle, decimal cci, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var stepPrice = Security.PriceStep ?? 1m;
		var pipDistance = Math.Max((highest - lowest) / stepPrice, DefaultPips) * stepPrice;

		// Close all positions on strong CCI movement
		if ((cci > CciDrop && _shortTrade) || (cci < -CciDrop && _longTrade))
		{
			ClosePosition();
			return;
		}

		var tradeNow = false;

		if (Position == 0m)
		{
			_tradeCount = 0;
			_longTrade = false;
			_shortTrade = false;
			tradeNow = true;
		}
		else if (_tradeCount <= MaxTrades)
		{
			if (_longTrade && _lastOpenBuyPrice - candle.ClosePrice >= pipDistance)
				tradeNow = true;

			if (_shortTrade && candle.ClosePrice - _lastOpenSellPrice >= pipDistance)
				tradeNow = true;
		}

		if (tradeNow)
		{
			var volume = Volume * (decimal)Math.Pow((double)LotExponent, _tradeCount);

			if (_longTrade)
			{
				BuyMarket(volume);
				_lastOpenBuyPrice = candle.ClosePrice;
				_tradeCount++;
			}
			else if (_shortTrade)
			{
				SellMarket(volume);
				_lastOpenSellPrice = candle.ClosePrice;
				_tradeCount++;
			}
			else if (_prevClose is decimal prev && prev > candle.ClosePrice)
			{
				if (_rsiValue > RsiMin)
				{
					SellMarket(volume);
					_shortTrade = true;
					_lastOpenSellPrice = candle.ClosePrice;
					_tradeCount = 1;
				}
				else if (_rsiValue < RsiMax)
				{
					BuyMarket(volume);
					_longTrade = true;
					_lastOpenBuyPrice = candle.ClosePrice;
					_tradeCount = 1;
				}
			}
		}

		if (Position != 0m)
		{
			var avg = PositionPrice;

			if (_longTrade)
			{
				var tp = avg + TakeProfit * stepPrice;
				var sl = avg - StopLoss * stepPrice;

				if (candle.ClosePrice >= tp || candle.ClosePrice <= sl)
					ClosePosition();
			}
			else if (_shortTrade)
			{
				var tp = avg - TakeProfit * stepPrice;
				var sl = avg + StopLoss * stepPrice;

				if (candle.ClosePrice <= tp || candle.ClosePrice >= sl)
					ClosePosition();
			}
		}

		_prevClose = candle.ClosePrice;
	}
}
