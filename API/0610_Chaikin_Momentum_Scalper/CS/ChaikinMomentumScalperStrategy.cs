using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Scalping strategy using Chaikin oscillator with ATR-based exits.
/// </summary>
public class ChaikinMomentumScalperStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplierSl;
	private readonly StrategyParam<decimal> _atrMultiplierTp;
	private readonly StrategyParam<DataType> _candleType;
	
	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private decimal _prevChaikin;
	private bool _isFirst;
	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	
	/// <summary>
	/// Fast EMA length for Chaikin oscillator.
	/// </summary>
	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	
	/// <summary>
	/// Slow EMA length for Chaikin oscillator.
	/// </summary>
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	
	/// <summary>
	/// SMA length for trend filter.
	/// </summary>
	public int SmaLength { get => _smaLength.Value; set => _smaLength.Value = value; }
	
	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	
	/// <summary>
	/// ATR multiplier for stop-loss.
	/// </summary>
	public decimal AtrMultiplierSL { get => _atrMultiplierSl.Value; set => _atrMultiplierSl.Value = value; }
	
	/// <summary>
	/// ATR multiplier for take-profit.
	/// </summary>
	public decimal AtrMultiplierTP { get => _atrMultiplierTp.Value; set => _atrMultiplierTp.Value = value; }
	
	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	
	/// <summary>
	/// Initializes a new instance of the <see cref="ChaikinMomentumScalperStrategy"/> class.
	/// </summary>
	public ChaikinMomentumScalperStrategy()
	{
		_fastLength = Param(nameof(FastLength), 3)
		.SetGreaterThanZero()
		.SetDisplay("Chaikin Fast Length", "Fast EMA length", "Chaikin");
		
		_slowLength = Param(nameof(SlowLength), 10)
		.SetGreaterThanZero()
		.SetDisplay("Chaikin Slow Length", "Slow EMA length", "Chaikin");
		
		_smaLength = Param(nameof(SmaLength), 200)
		.SetGreaterThanZero()
		.SetDisplay("SMA Length", "Simple moving average", "Trend");
		
		_atrLength = Param(nameof(AtrLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Length", "ATR period", "ATR");
		
		_atrMultiplierSl = Param(nameof(AtrMultiplierSL), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Multiplier SL", "ATR multiplier for stop-loss", "Risk");
		
		_atrMultiplierTp = Param(nameof(AtrMultiplierTP), 2m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Multiplier TP", "ATR multiplier for take-profit", "Risk");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for candles", "Data");
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
		_prevChaikin = 0m;
		_isFirst = true;
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_fastEma = new ExponentialMovingAverage { Length = FastLength };
		_slowEma = new ExponentialMovingAverage { Length = SlowLength };
		var ad = new AccumulationDistributionLine();
		var sma = new SimpleMovingAverage { Length = SmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ad, sma, atr, ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawIndicator(area, ad);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle, decimal adValue, decimal smaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var fastVal = _fastEma.Process(new DecimalIndicatorValue(_fastEma, adValue, candle.Time));
		var slowVal = _slowEma.Process(new DecimalIndicatorValue(_slowEma, adValue, candle.Time));
		
		if (!fastVal.IsFinal || !slowVal.IsFinal)
		{
			_prevChaikin = fastVal.ToDecimal() - slowVal.ToDecimal();
			return;
		}
		
		var chaikinValue = fastVal.ToDecimal() - slowVal.ToDecimal();
		
		if (_isFirst)
		{
			_prevChaikin = chaikinValue;
			_isFirst = false;
			return;
		}
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takeProfitPrice)
			{
				SellMarket(Math.Abs(Position));
				_stopPrice = 0m;
				_takeProfitPrice = 0m;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takeProfitPrice)
			{
				BuyMarket(Math.Abs(Position));
				_stopPrice = 0m;
				_takeProfitPrice = 0m;
			}
		}
		else
		{
			CancelActiveOrders();
			
			var crossUp = _prevChaikin <= 0 && chaikinValue > 0 && candle.ClosePrice > smaValue;
			var crossDown = _prevChaikin >= 0 && chaikinValue < 0 && candle.ClosePrice < smaValue;
			
			if (crossUp)
			{
				BuyMarket(Volume);
				_stopPrice = candle.ClosePrice - atrValue * AtrMultiplierSL;
				_takeProfitPrice = candle.ClosePrice + atrValue * AtrMultiplierTP;
			}
			else if (crossDown)
			{
				SellMarket(Volume);
				_stopPrice = candle.ClosePrice + atrValue * AtrMultiplierSL;
				_takeProfitPrice = candle.ClosePrice - atrValue * AtrMultiplierTP;
			}
		}
		
		_prevChaikin = chaikinValue;
	}
}
