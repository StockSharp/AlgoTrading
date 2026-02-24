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
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lastOpenBuyPrice;
	private decimal _lastOpenSellPrice;
	private decimal _entryPrice;
	private int _tradeCount;
	private bool _longTrade;
	private bool _shortTrade;
	private decimal _rsiValue;
	private decimal? _prevClose;

	public int StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public int TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public int DefaultPips { get => _defaultPips.Value; set => _defaultPips.Value = value; }
	public int Depth { get => _depth.Value; set => _depth.Value = value; }
	public decimal LotExponent { get => _lotExponent.Value; set => _lotExponent.Value = value; }
	public int MaxTrades { get => _maxTrades.Value; set => _maxTrades.Value = value; }
	public decimal RsiMin { get => _rsiMin.Value; set => _rsiMin.Value = value; }
	public decimal RsiMax { get => _rsiMax.Value; set => _rsiMax.Value = value; }
	public decimal CciDrop { get => _cciDrop.Value; set => _cciDrop.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AngryBirdScalpingStrategy()
	{
		_stopLoss = Param(nameof(StopLoss), 500)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 20)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in points", "Risk");

		_defaultPips = Param(nameof(DefaultPips), 12)
			.SetGreaterThanZero()
			.SetDisplay("Default Pips", "Minimal grid step in pips", "Grid");

		_depth = Param(nameof(Depth), 24)
			.SetGreaterThanZero()
			.SetDisplay("Depth", "Bars for high/low calculation", "Grid");

		_lotExponent = Param(nameof(LotExponent), 1.62m)
			.SetGreaterThanZero()
			.SetDisplay("Lot Exponent", "Volume multiplier for averaging", "Grid");

		_maxTrades = Param(nameof(MaxTrades), 5)
			.SetGreaterThanZero()
			.SetDisplay("Max Trades", "Maximum number of averaging orders", "Grid");

		_rsiMin = Param(nameof(RsiMin), 30m)
			.SetDisplay("RSI Min", "RSI threshold to sell", "Signals");

		_rsiMax = Param(nameof(RsiMax), 70m)
			.SetDisplay("RSI Max", "RSI threshold to buy", "Signals");

		_cciDrop = Param(nameof(CciDrop), 500m)
			.SetGreaterThanZero()
			.SetDisplay("CCI Drop", "CCI value to close positions", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Working candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_tradeCount = 0;
		_longTrade = false;
		_shortTrade = false;
		_entryPrice = 0;

		var cci = new CommodityChannelIndex { Length = 55 };
		var rsi = new RelativeStrengthIndex { Length = 14 };
		var highest = new Highest { Length = Depth };
		var lowest = new Lowest { Length = Depth };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(cci, rsi, highest, lowest, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, cci);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cci, decimal rsi, decimal highest, decimal lowest)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var stepPrice = Security.PriceStep ?? 1m;
		var pipDistance = Math.Max((highest - lowest) / Math.Max(stepPrice, 1m), DefaultPips) * stepPrice;

		_rsiValue = rsi;

		// Close all positions on strong CCI movement
		if ((cci > CciDrop && _shortTrade) || (cci < -CciDrop && _longTrade))
		{
			CloseAll();
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
			if (_longTrade && _lastOpenBuyPrice - close >= pipDistance)
				tradeNow = true;

			if (_shortTrade && close - _lastOpenSellPrice >= pipDistance)
				tradeNow = true;
		}

		if (tradeNow)
		{
			var volume = Volume * (decimal)Math.Pow((double)LotExponent, _tradeCount);

			if (_longTrade)
			{
				BuyMarket(volume);
				_lastOpenBuyPrice = close;
				_tradeCount++;
			}
			else if (_shortTrade)
			{
				SellMarket(volume);
				_lastOpenSellPrice = close;
				_tradeCount++;
			}
			else if (_prevClose is decimal prev && prev > close)
			{
				if (_rsiValue > RsiMin)
				{
					SellMarket(volume);
					_shortTrade = true;
					_lastOpenSellPrice = close;
					_entryPrice = close;
					_tradeCount = 1;
				}
				else if (_rsiValue < RsiMax)
				{
					BuyMarket(volume);
					_longTrade = true;
					_lastOpenBuyPrice = close;
					_entryPrice = close;
					_tradeCount = 1;
				}
			}
		}

		if (Position != 0m)
		{
			if (_longTrade)
			{
				var tp = _entryPrice + TakeProfit * stepPrice;
				var sl = _entryPrice - StopLoss * stepPrice;

				if (close >= tp || close <= sl)
					CloseAll();
			}
			else if (_shortTrade)
			{
				var tp = _entryPrice - TakeProfit * stepPrice;
				var sl = _entryPrice + StopLoss * stepPrice;

				if (close <= tp || close >= sl)
					CloseAll();
			}
		}

		_prevClose = close;
	}

	private void CloseAll()
	{
		if (Position > 0)
			SellMarket(Math.Abs(Position));
		else if (Position < 0)
			BuyMarket(Math.Abs(Position));

		_tradeCount = 0;
		_longTrade = false;
		_shortTrade = false;
		_entryPrice = 0;
	}
}
