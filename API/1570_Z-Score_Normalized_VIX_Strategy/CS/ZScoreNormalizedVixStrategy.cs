using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy averaging z-scores of multiple volatility indices.
/// </summary>
public class ZScoreNormalizedVixStrategy : Strategy
{
	private readonly StrategyParam<int> _zScoreLength;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _useVix;
	private readonly StrategyParam<bool> _useVix3m;
	private readonly StrategyParam<bool> _useVix9d;
	private readonly StrategyParam<bool> _useVvix;
	private readonly StrategyParam<Security> _vixSecurity;
	private readonly StrategyParam<Security> _vix3mSecurity;
	private readonly StrategyParam<Security> _vix9dSecurity;
	private readonly StrategyParam<Security> _vvixSecurity;
	
	private decimal? _zVix;
	private decimal? _zVix3m;
	private decimal? _zVix9d;
	private decimal? _zVvix;
	
	/// <summary>
	/// Lookback period for z-score calculation.
	/// </summary>
	public int ZScoreLength
	{
		get => _zScoreLength.Value;
		set => _zScoreLength.Value = value;
	}
	
	/// <summary>
	/// Z-score threshold for entry and exit.
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
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
	/// Include VIX index.
	/// </summary>
	public bool UseVix
	{
		get => _useVix.Value;
		set => _useVix.Value = value;
	}
	
	/// <summary>
	/// Include VIX3M index.
	/// </summary>
	public bool UseVix3m
	{
		get => _useVix3m.Value;
		set => _useVix3m.Value = value;
	}
	
	/// <summary>
	/// Include VIX9D index.
	/// </summary>
	public bool UseVix9d
	{
		get => _useVix9d.Value;
		set => _useVix9d.Value = value;
	}
	
	/// <summary>
	/// Include VVIX index.
	/// </summary>
	public bool UseVvix
	{
		get => _useVvix.Value;
		set => _useVvix.Value = value;
	}
	
	/// <summary>
	/// Security representing VIX.
	/// </summary>
	public Security VixSecurity
	{
		get => _vixSecurity.Value;
		set => _vixSecurity.Value = value;
	}
	
	/// <summary>
	/// Security representing VIX3M.
	/// </summary>
	public Security Vix3mSecurity
	{
		get => _vix3mSecurity.Value;
		set => _vix3mSecurity.Value = value;
	}
	
	/// <summary>
	/// Security representing VIX9D.
	/// </summary>
	public Security Vix9dSecurity
	{
		get => _vix9dSecurity.Value;
		set => _vix9dSecurity.Value = value;
	}
	
	/// <summary>
	/// Security representing VVIX.
	/// </summary>
	public Security VvixSecurity
	{
		get => _vvixSecurity.Value;
		set => _vvixSecurity.Value = value;
	}
	
	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public ZScoreNormalizedVixStrategy()
	{
		_zScoreLength = Param(nameof(ZScoreLength), 6)
		.SetDisplay("Z-Score Length", "Lookback period for z-score", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(3, 20, 1);
		
		_threshold = Param(nameof(Threshold), 1m)
		.SetDisplay("Z-Score Threshold", "Entry and exit threshold", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(0.5m, 2m, 0.1m);
		
		_useVix = Param(nameof(UseVix), true)
		.SetDisplay("Use VIX", "Include VIX index", "Indices");
		_useVix3m = Param(nameof(UseVix3m), true)
		.SetDisplay("Use VIX3M", "Include VIX3M index", "Indices");
		_useVix9d = Param(nameof(UseVix9d), true)
		.SetDisplay("Use VIX9D", "Include VIX9D index", "Indices");
		_useVvix = Param(nameof(UseVvix), true)
		.SetDisplay("Use VVIX", "Include VVIX index", "Indices");
		
		_vixSecurity = Param(nameof(VixSecurity), new Security { Id = "CBOE:VIX" })
		.SetDisplay("VIX Security", "Security representing VIX", "Indices");
		_vix3mSecurity = Param(nameof(Vix3mSecurity), new Security { Id = "CBOE:VIX3M" })
		.SetDisplay("VIX3M Security", "Security representing VIX3M", "Indices");
		_vix9dSecurity = Param(nameof(Vix9dSecurity), new Security { Id = "CBOE:VIX9D" })
		.SetDisplay("VIX9D Security", "Security representing VIX9D", "Indices");
		_vvixSecurity = Param(nameof(VvixSecurity), new Security { Id = "CBOE:VVIX" })
		.SetDisplay("VVIX Security", "Security representing VVIX", "Indices");
		
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "Data");
	}
	
	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (UseVix)
		yield return (VixSecurity, CandleType);
		if (UseVix3m)
		yield return (Vix3mSecurity, CandleType);
		if (UseVix9d)
		yield return (Vix9dSecurity, CandleType);
		if (UseVvix)
		yield return (VvixSecurity, CandleType);
		
		yield return (Security, CandleType);
	}
	
	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		
		_zVix = _zVix3m = _zVix9d = _zVvix = null;
	}
	
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		
		var mainSub = SubscribeCandles(CandleType);
		mainSub.Bind(ProcessMainCandle).Start();
		
		if (UseVix)
		{
			var sma = new SimpleMovingAverage { Length = ZScoreLength };
			var std = new StandardDeviation { Length = ZScoreLength };
			SubscribeCandles(CandleType, security: VixSecurity)
			.Bind(sma, std, ProcessVixCandle)
			.Start();
		}
		
		if (UseVix3m)
		{
			var sma = new SimpleMovingAverage { Length = ZScoreLength };
			var std = new StandardDeviation { Length = ZScoreLength };
			SubscribeCandles(CandleType, security: Vix3mSecurity)
			.Bind(sma, std, ProcessVix3mCandle)
			.Start();
		}
		
		if (UseVix9d)
		{
			var sma = new SimpleMovingAverage { Length = ZScoreLength };
			var std = new StandardDeviation { Length = ZScoreLength };
			SubscribeCandles(CandleType, security: Vix9dSecurity)
			.Bind(sma, std, ProcessVix9dCandle)
			.Start();
		}
		
		if (UseVvix)
		{
			var sma = new SimpleMovingAverage { Length = ZScoreLength };
			var std = new StandardDeviation { Length = ZScoreLength };
			SubscribeCandles(CandleType, security: VvixSecurity)
			.Bind(sma, std, ProcessVvixCandle)
			.Start();
		}
		
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSub);
			DrawOwnTrades(area);
		}
	}
	
	private void ProcessVixCandle(ICandleMessage candle, decimal sma, decimal std)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (std == 0)
		return;
		
		_zVix = (candle.ClosePrice - sma) / std;
	}
	
	private void ProcessVix3mCandle(ICandleMessage candle, decimal sma, decimal std)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (std == 0)
		return;
		
		_zVix3m = (candle.ClosePrice - sma) / std;
	}
	
	private void ProcessVix9dCandle(ICandleMessage candle, decimal sma, decimal std)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (std == 0)
		return;
		
		_zVix9d = (candle.ClosePrice - sma) / std;
	}
	
	private void ProcessVvixCandle(ICandleMessage candle, decimal sma, decimal std)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (std == 0)
		return;
		
		_zVvix = (candle.ClosePrice - sma) / std;
	}
	
	private void ProcessMainCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;
		
		if (!IsFormedAndOnlineAndAllowTrading())
		return;
		
		decimal sum = 0m;
		int count = 0;
		
		if (UseVix && _zVix is decimal z1)
		{
			sum += z1;
			count++;
		}
		
		if (UseVix3m && _zVix3m is decimal z2)
		{
			sum += z2;
			count++;
		}
		
		if (UseVix9d && _zVix9d is decimal z3)
		{
			sum += z3;
			count++;
		}
		
		if (UseVvix && _zVvix is decimal z4)
		{
			sum += z4;
			count++;
		}
		
		if (count == 0)
		return;
		
		var combined = sum / count;
		
		if (Position <= 0 && combined < -Threshold)
		{
			BuyMarket(Volume);
		}
		else if (Position > 0 && combined > -Threshold)
		{
			SellMarket(Position);
		}
	}
}
