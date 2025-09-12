using System;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Liquidity Grab Strategy (Volume Trap).
/// Waits for a bearish liquidity grab on flat volume,
/// then enters on a pullback to the fair value gap bottom
/// with symmetrical stop loss and take profit.
/// </summary>
public class LiquidityGrabVolumeTrapStrategy : Strategy
{
	private readonly StrategyParam<int> _volumeMaLength;
	private readonly StrategyParam<decimal> _maxVolumeDeviation;
	private readonly StrategyParam<DataType> _candleType;
	
	private SimpleMovingAverage _volumeSma = null!;
	private ICandleMessage _prev1;
	private ICandleMessage _prev2;
	private decimal _entryPrice;
	private decimal _stopLoss;
	private decimal _takeProfit;
	private bool _entryOrderActive;
	private bool _protectionPlaced;
	
	/// <summary>
	/// Volume moving average length.
	/// </summary>
	public int VolumeMaLength
	{
		get => _volumeMaLength.Value;
		set => _volumeMaLength.Value = value;
	}
	
	/// <summary>
	/// Maximum volume deviation from its moving average.
	/// </summary>
	public decimal MaxVolumeDeviation
	{
		get => _maxVolumeDeviation.Value;
		set => _maxVolumeDeviation.Value = value;
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
	/// Initializes a new instance of the <see cref="LiquidityGrabVolumeTrapStrategy"/>.
	/// </summary>
	public LiquidityGrabVolumeTrapStrategy()
	{
		_volumeMaLength = Param(nameof(VolumeMaLength), 20)
		.SetDisplay("Volume MA Length", "Length of volume moving average", "General")
		.SetCanOptimize(true);
		
		_maxVolumeDeviation = Param(nameof(MaxVolumeDeviation), 0.05m)
		.SetDisplay("Max Volume Deviation (%)", "Maximum allowed deviation from volume MA", "General")
		.SetCanOptimize(true);
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Candles for calculations", "General");
	}
	
	/// <inheritdoc />
	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	=> [(Security, CandleType)];
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prev1 = null;
		_prev2 = null;
		_entryOrderActive = false;
		_protectionPlaced = false;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		_volumeSma = new SimpleMovingAverage { Length = VolumeMaLength };
		
		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		var volMa = _volumeSma.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();
		if (volMa == 0m)
		{
			_prev2 = _prev1;
			_prev1 = candle;
			return;
		}
		
		var volFlat = Math.Abs(candle.TotalVolume - volMa) / volMa < MaxVolumeDeviation;
		
		if (_prev1 is not null && _prev2 is not null)
		{
			var isBearishBreak = _prev1.ClosePrice > _prev2.ClosePrice &&
			candle.ClosePrice < _prev1.ClosePrice &&
			candle.ClosePrice < _prev1.LowPrice;
			
			var liquidityGrab = isBearishBreak && volFlat;
			
			var fvgTop = _prev1.HighPrice;
			var fvgBottom = _prev2.LowPrice;
			var fvgExists = _prev2.ClosePrice < _prev1.OpenPrice &&
			candle.ClosePrice > fvgTop &&
			liquidityGrab;
			
			if (fvgExists && Position <= 0 && !_entryOrderActive)
			{
				var volume = Volume + Math.Abs(Position);
				_entryPrice = fvgBottom;
				var range = fvgTop - fvgBottom;
				_stopLoss = fvgBottom - range;
				_takeProfit = fvgTop;
				_entryOrderActive = true;
				BuyLimit(volume, _entryPrice);
			}
		}
		
		if (_entryOrderActive && !_protectionPlaced && Position > 0)
		{
			var volume = Math.Abs(Position);
			SellStop(volume, _stopLoss);
			SellLimit(volume, _takeProfit);
			_protectionPlaced = true;
		}
		
		if (Position == 0)
		{
			_entryOrderActive = false;
			_protectionPlaced = false;
		}
		
		_prev2 = _prev1;
		_prev1 = candle;
	}
}
