using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EURUSD V2.0 strategy using SMA and ATR filters with risk management.
/// </summary>
public class EurusdV20Strategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _buffer;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _filter;
	private readonly StrategyParam<decimal> _maxSpread;
	private readonly StrategyParam<decimal> _riskFactor;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrThreshold;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _entryPrice;
	private decimal _stopPrice;
	private decimal _takePrice;
	private bool _priceAbove;
	private bool _tradeOk = true;
	
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public decimal Buffer { get => _buffer.Value; set => _buffer.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal Filter { get => _filter.Value; set => _filter.Value = value; }
	public decimal MaxSpread { get => _maxSpread.Value; set => _maxSpread.Value = value; }
	public decimal RiskFactor { get => _riskFactor.Value; set => _riskFactor.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal AtrThreshold { get => _atrThreshold.Value; set => _atrThreshold.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	public EurusdV20Strategy()
	{
		_maLength = Param(nameof(MaLength), 218)
		.SetGreaterThanZero()
		.SetDisplay("MA Length", "Simple moving average period", "General");
		_buffer = Param(nameof(Buffer), 0m)
		.SetDisplay("Buffer (pips)", "Max distance from SMA to enter", "Trading");
		_stopLoss = Param(nameof(StopLoss), 20m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss (pips)", "Stop loss distance", "Risk");
		_takeProfit = Param(nameof(TakeProfit), 350m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit (pips)", "Take profit distance", "Risk");
		_filter = Param(nameof(Filter), 50m)
		.SetGreaterThanZero()
		.SetDisplay("Noise Filter (pips)", "Price noise filter", "Trading");
		_maxSpread = Param(nameof(MaxSpread), 4m)
		.SetGreaterThanZero()
		.SetDisplay("Max Spread (pips)", "Maximum allowed spread", "Risk");
		_riskFactor = Param(nameof(RiskFactor), 2m)
		.SetGreaterThanZero()
		.SetDisplay("Risk Factor Z", "Money management factor", "Risk");
		_atrLength = Param(nameof(AtrLength), 200)
		.SetGreaterThanZero()
		.SetDisplay("ATR Length", "ATR calculation period", "Indicators");
		_atrThreshold = Param(nameof(AtrThreshold), 40m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Threshold (pips)", "Max ATR to allow trades", "Indicators");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles to process", "General");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
		_priceAbove = false;
		_tradeOk = true;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		StartProtection();
		
		var sma = new SMA { Length = MaLength };
		var atr = new Atr { Length = AtrLength };
		
		var sub = SubscribeCandles(CandleType);
		sub.Bind(sma, atr, ProcessCandle).Start();
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal sma, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var pip = (Security?.MinPriceStep ?? 1m) * 10m; // approximate pip size
		var spread = Security.BestAsk - Security.BestBid;
		var close = candle.ClosePrice;
		
		_priceAbove = close > sma;
		
		if (_tradeOk)
		{
			if (spread <= MaxSpread * pip && atr < AtrThreshold * pip)
			{
				if (close - sma < Buffer * pip && _priceAbove)
				{
					var volume = CalcVolume();
					_entryPrice = close;
					_stopPrice = close + StopLoss * pip;
					_takePrice = close - TakeProfit * pip;
					SellMarket(volume);
					_tradeOk = false;
					return;
				}
				
				if (sma - close < Buffer * pip && !_priceAbove)
				{
					var volume = CalcVolume();
					_entryPrice = close;
					_stopPrice = close - StopLoss * pip;
					_takePrice = close + TakeProfit * pip;
					BuyMarket(volume);
					_tradeOk = false;
				}
			}
			
			return;
		}
		
		if (Position < 0)
		{
			if (close <= _takePrice || close >= _stopPrice)
			BuyMarket(-Position);
		}
		else if (Position > 0)
		{
			if (close >= _takePrice || close <= _stopPrice)
			SellMarket(Position);
		}
		
		if (_priceAbove)
		{
			if (close < _entryPrice - Filter * pip)
			{
				_priceAbove = false;
				_tradeOk = true;
			}
			else if (close > _entryPrice + Filter * pip)
			{
				_tradeOk = true;
			}
		}
		else
		{
			if (close > _entryPrice + Filter * pip)
			{
				_priceAbove = true;
				_tradeOk = true;
			}
			else if (close < _entryPrice - Filter * pip)
			{
				_tradeOk = true;
			}
		}
	}
	
	private decimal CalcVolume()
	{
		var balance = Portfolio?.Cash ?? 0m;
		var m = (balance * 0.005m * RiskFactor) / (0.1m * StopLoss);
		if (m < 1m)
		m = 1m;
		
		return m * 0.01m;
	}
}

