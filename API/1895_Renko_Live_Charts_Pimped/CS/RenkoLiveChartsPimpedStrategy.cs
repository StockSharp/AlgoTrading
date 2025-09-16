using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Renko strategy with optional ATR-based brick size.
/// Builds renko candles and trades on brick direction changes.
/// </summary>
public class RenkoLiveChartsPimpedStrategy : Strategy
{
	private readonly StrategyParam<decimal> _boxSize;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<bool> _calculateBestBoxSize;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _atrCandleType;
	private readonly StrategyParam<bool> _useAtrMa;
	private readonly StrategyParam<int> _atrMaPeriod;
	
	private DataType _renkoType;
	private AverageTrueRange _atr;
	private SimpleMovingAverage _atrMa;
	private bool _renkoStarted;
	private decimal _prevOpen;
	private decimal _prevClose;
	private bool _hasPrev;
	
	/// <summary>
	/// Renko brick size in price units.
	/// </summary>
	public decimal BoxSize { get => _boxSize.Value; set => _boxSize.Value = value; }
	
	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }
	
	/// <summary>
	/// Calculate brick size from ATR.
	/// </summary>
	public bool CalculateBestBoxSize { get => _calculateBestBoxSize.Value; set => _calculateBestBoxSize.Value = value; }
	
	/// <summary>
	/// ATR calculation period.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	
	/// <summary>
	/// Candle type used for ATR calculation.
	/// </summary>
	public DataType AtrCandleType { get => _atrCandleType.Value; set => _atrCandleType.Value = value; }
	
	/// <summary>
	/// Apply moving average to ATR.
	/// </summary>
	public bool UseAtrMa { get => _useAtrMa.Value; set => _useAtrMa.Value = value; }
	
	/// <summary>
	/// Moving average length for ATR.
	/// </summary>
	public int AtrMaPeriod { get => _atrMaPeriod.Value; set => _atrMaPeriod.Value = value; }
	
	/// <summary>
	/// Initializes <see cref="RenkoLiveChartsPimpedStrategy"/>.
	/// </summary>
	public RenkoLiveChartsPimpedStrategy()
	{
		_boxSize = Param(nameof(BoxSize), 10m)
		.SetGreaterThanZero()
		.SetDisplay("Box Size", "Renko brick size", "Renko")
		.SetCanOptimize(true)
		.SetOptimize(5m, 20m, 1m);
		
		_volume = Param(nameof(Volume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Volume", "Order volume", "Trading");
		
		_calculateBestBoxSize = Param(nameof(CalculateBestBoxSize), false)
		.SetDisplay("Use ATR Box", "Calculate brick size from ATR", "Renko");
		
		_atrPeriod = Param(nameof(AtrPeriod), 24)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "ATR calculation period", "Renko")
		.SetCanOptimize(true);
		
		_atrCandleType = Param(nameof(AtrCandleType), TimeSpan.FromMinutes(60).TimeFrame())
		.SetDisplay("ATR TimeFrame", "Candles for ATR calculation", "Renko");
		
		_useAtrMa = Param(nameof(UseAtrMa), true)
		.SetDisplay("Smooth ATR", "Apply moving average on ATR", "Renko");
		
		_atrMaPeriod = Param(nameof(AtrMaPeriod), 120)
		.SetGreaterThanZero()
		.SetDisplay("ATR MA Period", "Moving average length for ATR", "Renko")
		.SetCanOptimize(true);
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (CalculateBestBoxSize)
		{
			yield return (Security, AtrCandleType);
		}
		else
		{
			_renkoType ??= DataType.Create(typeof(RenkoCandleMessage), new RenkoCandleArg
			{
				BuildFrom = RenkoBuildFrom.Points,
				BoxSize = BoxSize
			});
			
			yield return (Security, _renkoType);
		}
	}
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		
		_renkoType = default;
		_atr = default;
		_atrMa = default;
		_renkoStarted = false;
		_hasPrev = false;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		if (CalculateBestBoxSize)
		{
			_atr = new AverageTrueRange { Length = AtrPeriod };
			
			var atrSub = SubscribeCandles(AtrCandleType);
			
			if (UseAtrMa)
			{
				_atrMa = new SimpleMovingAverage { Length = AtrMaPeriod };
				atrSub.Bind(_atr, _atrMa, ProcessAtr).Start();
			}
			else
			{
				atrSub.Bind(_atr, ProcessAtr).Start();
			}
		}
		else
		{
			StartRenko();
		}
	}
	
	private void ProcessAtr(ICandleMessage candle, decimal atr, decimal ma)
	{
		if (candle.State != CandleStates.Finished || _renkoStarted)
			return;
		
		BoxSize = UseAtrMa ? ma : atr;
		StartRenko();
	}
	
	private void ProcessAtr(ICandleMessage candle, decimal atr)
	{
		if (candle.State != CandleStates.Finished || _renkoStarted)
			return;
		
		BoxSize = atr;
		StartRenko();
	}
	
	private void StartRenko()
	{
		if (_renkoStarted)
			return;
		
		_renkoType = DataType.Create(typeof(RenkoCandleMessage), new RenkoCandleArg
		{
			BuildFrom = RenkoBuildFrom.Points,
			BoxSize = BoxSize
		});
		
		var subscription = SubscribeCandles(_renkoType);
		subscription.Bind(ProcessRenko).Start();
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
		
		StartProtection();
		
		_renkoStarted = true;
	}
	
	private void ProcessRenko(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;
		
		if (_hasPrev)
		{
			var prevBull = _prevClose > _prevOpen;
			var currBull = candle.ClosePrice > candle.OpenPrice;
			
			if (!prevBull && currBull && Position <= 0)
				BuyMarket(Volume);
			else if (prevBull && !currBull && Position >= 0)
				SellMarket(Volume);
		}
		
		_prevOpen = candle.OpenPrice;
		_prevClose = candle.ClosePrice;
		_hasPrev = true;
	}
}

