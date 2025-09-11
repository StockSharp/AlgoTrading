using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA crossover strategy with multiple take-profit levels.
/// Enters long when EMA20 crosses above EMA50.
/// Enters short when EMA20 crosses below EMA50.
/// Exits when price reaches any take-profit level or stop-loss.
/// </summary>
public class EMACrossoverTakeProfitStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<decimal> _tp1Multiplier;
	private readonly StrategyParam<decimal> _tp2Multiplier;
	private readonly StrategyParam<decimal> _tp3Multiplier;
	private readonly StrategyParam<decimal> _tp4Multiplier;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	
	private decimal _entryPrice;
	private bool _isLongPosition;
	private decimal _tp1;
	private decimal _tp2;
	private decimal _tp3;
	private decimal _tp4;
	
	/// <summary>
	/// Fast EMA period length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}
	
	/// <summary>
	/// Slow EMA period length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}
	
	/// <summary>
	/// Multiplier for take-profit level 1.
	/// </summary>
	public decimal Tp1Multiplier
	{
		get => _tp1Multiplier.Value;
		set => _tp1Multiplier.Value = value;
	}
	
	/// <summary>
	/// Multiplier for take-profit level 2.
	/// </summary>
	public decimal Tp2Multiplier
	{
		get => _tp2Multiplier.Value;
		set => _tp2Multiplier.Value = value;
	}
	
	/// <summary>
	/// Multiplier for take-profit level 3.
	/// </summary>
	public decimal Tp3Multiplier
	{
		get => _tp3Multiplier.Value;
		set => _tp3Multiplier.Value = value;
	}
	
	/// <summary>
	/// Multiplier for take-profit level 4.
	/// </summary>
	public decimal Tp4Multiplier
	{
		get => _tp4Multiplier.Value;
		set => _tp4Multiplier.Value = value;
	}
	
	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}
	
	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}
	
	/// <summary>
	/// Constructor.
	/// </summary>
	public EMACrossoverTakeProfitStrategy()
	{
		_fastLength = Param(nameof(FastLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("Fast EMA Length", "Period for fast EMA", "EMA")
		.SetCanOptimize(true)
		.SetOptimize(10, 50, 5);
		
		_slowLength = Param(nameof(SlowLength), 50)
		.SetGreaterThanZero()
		.SetDisplay("Slow EMA Length", "Period for slow EMA", "EMA")
		.SetCanOptimize(true)
		.SetOptimize(30, 100, 5);
		
		_tp1Multiplier = Param(nameof(Tp1Multiplier), 0.5m)
		.SetGreaterThanZero()
		.SetDisplay("TP1 Multiplier", "Take-profit level 1 multiplier", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 2m, 0.5m);
		
		_tp2Multiplier = Param(nameof(Tp2Multiplier), 1.0m)
		.SetGreaterThanZero()
		.SetDisplay("TP2 Multiplier", "Take-profit level 2 multiplier", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 2m, 0.5m);
		
		_tp3Multiplier = Param(nameof(Tp3Multiplier), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("TP3 Multiplier", "Take-profit level 3 multiplier", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 2m, 0.5m);
		
		_tp4Multiplier = Param(nameof(Tp4Multiplier), 2.0m)
		.SetGreaterThanZero()
		.SetDisplay("TP4 Multiplier", "Take-profit level 4 multiplier", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 2m, 0.5m);
		
		_stopLossPercent = Param(nameof(StopLossPercent), 3.0m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss %", "Stop-loss percentage from entry price", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(1.0m, 5.0m, 1.0m);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles for calculations", "General");
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
		_tp1 = _tp2 = _tp3 = _tp4 = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var fastEma = new EMA { Length = FastLength };
		var slowEma = new EMA { Length = SlowLength };
		var trendEma = new EMA { Length = 200 };
		
		var subscription = SubscribeCandles(CandleType);
		
		var wasFastLessThanSlow = false;
		var isInitialized = false;
		
		subscription
		.Bind(fastEma, slowEma, trendEma, (candle, fast, slow, trend) =>
		{
			if (candle.State != CandleStates.Finished)
			return;
			
			if (!IsFormedAndOnlineAndAllowTrading())
			return;
			
			if (!isInitialized && fastEma.IsFormed && slowEma.IsFormed)
			{
				wasFastLessThanSlow = fast < slow;
				isInitialized = true;
				return;
			}
			
			if (!isInitialized)
			return;
			
			var isFastLessThanSlow = fast < slow;
			
			if (wasFastLessThanSlow != isFastLessThanSlow)
			{
				if (!isFastLessThanSlow)
				{
					if (Position <= 0)
					{
						_entryPrice = candle.ClosePrice;
						_isLongPosition = true;
						BuyMarket(Volume + Math.Abs(Position));
						var range = candle.HighPrice - candle.LowPrice;
						_tp1 = candle.HighPrice + range * Tp1Multiplier;
						_tp2 = candle.HighPrice + range * Tp2Multiplier;
						_tp3 = candle.HighPrice + range * Tp3Multiplier;
						_tp4 = candle.HighPrice + range * Tp4Multiplier;
					}
				}
				else
				{
					if (Position >= 0)
					{
						_entryPrice = candle.ClosePrice;
						_isLongPosition = false;
						SellMarket(Volume + Math.Abs(Position));
						var range = candle.HighPrice - candle.LowPrice;
						_tp1 = candle.LowPrice - range * Tp1Multiplier;
						_tp2 = candle.LowPrice - range * Tp2Multiplier;
						_tp3 = candle.LowPrice - range * Tp3Multiplier;
						_tp4 = candle.LowPrice - range * Tp4Multiplier;
					}
				}
				
				wasFastLessThanSlow = isFastLessThanSlow;
			}
			
			CheckTakeProfit(candle);
			CheckStopLoss(candle);
			})
			.Start();
			
			var area = CreateChartArea();
			if (area != null)
			{
				DrawCandles(area, subscription);
				DrawIndicator(area, fastEma);
				DrawIndicator(area, slowEma);
				DrawIndicator(area, trendEma);
				DrawOwnTrades(area);
			}
		}
		
		private void CheckTakeProfit(ICandleMessage candle)
		{
			if (Position == 0)
			return;
			
			var targets = new[] { _tp1, _tp2, _tp3, _tp4 };
			foreach (var tp in targets)
			{
				if (tp == 0m)
				continue;
				
				if (_isLongPosition)
				{
					if (candle.HighPrice >= tp)
					{
						SellMarket(Math.Abs(Position));
						ResetTakeProfits();
						return;
					}
				}
				else
				{
					if (candle.LowPrice <= tp)
					{
						BuyMarket(Math.Abs(Position));
						ResetTakeProfits();
						return;
					}
				}
			}
		}
		
		private void CheckStopLoss(ICandleMessage candle)
		{
			if (Position == 0 || _entryPrice == 0m)
			return;
			
			var threshold = StopLossPercent / 100m;
			if (_isLongPosition && Position > 0)
			{
				var stopPrice = _entryPrice * (1m - threshold);
				if (candle.LowPrice <= stopPrice)
				{
					SellMarket(Math.Abs(Position));
					ResetTakeProfits();
				}
			}
			else if (!_isLongPosition && Position < 0)
			{
				var stopPrice = _entryPrice * (1m + threshold);
				if (candle.HighPrice >= stopPrice)
				{
					BuyMarket(Math.Abs(Position));
					ResetTakeProfits();
				}
			}
		}
		
		private void ResetTakeProfits()
		{
			_tp1 = _tp2 = _tp3 = _tp4 = 0m;
			_entryPrice = 0m;
			_isLongPosition = false;
		}
	}
