using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Commitment of Trader %R strategy with optional SMA filter.
/// </summary>
public class CommitmentOfTraderRStrategy : Strategy
{
	private readonly StrategyParam<int> _williamsPeriod;
	private readonly StrategyParam<decimal> _upperThreshold;
	private readonly StrategyParam<decimal> _lowerThreshold;
	private readonly StrategyParam<bool> _smaEnabled;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<DataType> _candleType;

	/// <summary>
	/// Williams %R period.
	/// </summary>
	public int WilliamsPeriod
	{
		get => _williamsPeriod.Value;
		set => _williamsPeriod.Value = value;
	}

	/// <summary>
	/// Upper threshold.
	/// </summary>
	public decimal UpperThreshold
	{
		get => _upperThreshold.Value;
		set => _upperThreshold.Value = value;
	}

	/// <summary>
	/// Lower threshold.
	/// </summary>
	public decimal LowerThreshold
	{
		get => _lowerThreshold.Value;
		set => _lowerThreshold.Value = value;
	}

	/// <summary>
	/// Enable SMA filter.
	/// </summary>
	public bool SmaEnabled
	{
		get => _smaEnabled.Value;
		set => _smaEnabled.Value = value;
	}

	/// <summary>
	/// SMA period.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public CommitmentOfTraderRStrategy()
	{
		_williamsPeriod = Param(nameof(WilliamsPeriod), 252)
			.SetGreaterThanZero()
			.SetDisplay("Williams %R Period", "Lookback period for Williams %R", "Williams %R")
			.SetCanOptimize(true)
			.SetOptimize(50, 300, 10);

		_upperThreshold = Param(nameof(UpperThreshold), -10m)
			.SetDisplay("Upper Threshold", "Upper threshold for Williams %R", "Williams %R")
			.SetCanOptimize(true)
			.SetOptimize(-20m, -5m, 5m);

		_lowerThreshold = Param(nameof(LowerThreshold), -90m)
			.SetDisplay("Lower Threshold", "Lower threshold for Williams %R", "Williams %R")
			.SetCanOptimize(true)
			.SetOptimize(-95m, -70m, 5m);

		_smaEnabled = Param(nameof(SmaEnabled), true)
			.SetDisplay("Enable SMA Filter", "Use SMA trend filter", "SMA");

		_smaLength = Param(nameof(SmaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "SMA period for trend filter", "SMA")
			.SetCanOptimize(true)
			.SetOptimize(50, 300, 10);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var williams = new WilliamsR { Length = WilliamsPeriod };
		var sma = new SMA { Length = SmaLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(williams, sma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, williams);
			if (SmaEnabled)
				DrawIndicator(area, sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal wr, decimal smaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (wr > UpperThreshold)
		{
			if (!SmaEnabled || candle.ClosePrice > smaValue)
			{
				if (Position <= 0)
					BuyMarket(Volume + Math.Abs(Position));
			}
		}
		else if (Position > 0 && wr < UpperThreshold)
		{
			SellMarket(Position);
		}

		if (wr < LowerThreshold)
		{
			if (!SmaEnabled || candle.ClosePrice < smaValue)
			{
				if (Position >= 0)
					SellMarket(Volume + Math.Abs(Position));
			}
		}
		else if (Position < 0 && wr > LowerThreshold)
		{
			BuyMarket(-Position);
		}
	}
}
