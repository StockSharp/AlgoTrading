using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trades when VIX spikes above its mean by a multiple of standard deviation.
/// Enters long on the main security and exits after a fixed number of bars.
/// </summary>
public class VixSpikeStrategy : Strategy
{
	private readonly StrategyParam<int> _stdDevLength;
	private readonly StrategyParam<decimal> _stdDevMultiplier;
	private readonly StrategyParam<int> _exitPeriods;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<Security> _vixSecurity;

	private int _barsSinceEntry;

	/// <summary>
	/// Standard deviation length.
	/// </summary>
	public int StdDevLength
	{
		get => _stdDevLength.Value;
		set => _stdDevLength.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier.
	/// </summary>
	public decimal StdDevMultiplier
	{
		get => _stdDevMultiplier.Value;
		set => _stdDevMultiplier.Value = value;
	}

	/// <summary>
	/// Number of bars to hold after entry.
	/// </summary>
	public int ExitPeriods
	{
		get => _exitPeriods.Value;
		set => _exitPeriods.Value = value;
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
	/// VIX security.
	/// </summary>
	public Security VixSecurity
	{
		get => _vixSecurity.Value;
		set => _vixSecurity.Value = value;
	}

	public VixSpikeStrategy()
	{
		_stdDevLength = Param(nameof(StdDevLength), 15)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Length", "Length for VIX calculations", "Parameters");

		_stdDevMultiplier = Param(nameof(StdDevMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("StdDev Mult", "StdDev multiplier", "Parameters");

		_exitPeriods = Param(nameof(ExitPeriods), 10)
			.SetGreaterThanZero()
			.SetDisplay("Exit Bars", "Bars to hold position", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "Parameters");

		_vixSecurity = Param(nameof(VixSecurity), new Security { Id = "CBOE:VIX" })
			.SetDisplay("VIX Security", "Security representing VIX", "Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (VixSecurity, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_barsSinceEntry = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection(new Unit(3, UnitTypes.Percent), new Unit(2, UnitTypes.Percent));

		var ma = new SimpleMovingAverage { Length = StdDevLength };
		var std = new StandardDeviation { Length = StdDevLength };

		var mainSub = SubscribeCandles(CandleType);
		mainSub.Bind(ProcessMain).Start();

		var vixSub = SubscribeCandles(CandleType, security: VixSecurity);
		vixSub.Bind(ma, std, ProcessVix).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, mainSub);
			DrawOwnTrades(area);
		}
	}

	private void ProcessVix(ICandleMessage candle, decimal maValue, decimal stdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var threshold = maValue + StdDevMultiplier * stdValue;
		if (candle.ClosePrice > threshold && Position <= 0)
		{
			BuyMarket();
			_barsSinceEntry = 0;
		}
	}

	private void ProcessMain(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (Position > 0)
		{
			_barsSinceEntry++;
			if (_barsSinceEntry >= ExitPeriods)
				SellMarket();
		}
	}
}
