namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Directional Movement Index crossover strategy.
/// </summary>
public class AdxDmiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _dmiPeriod;
	private readonly StrategyParam<bool> _allowLong;
	private readonly StrategyParam<bool> _allowShort;
	private readonly StrategyParam<bool> _closeLong;
	private readonly StrategyParam<bool> _closeShort;

	private DirectionalIndex _dmi = null!;
	private decimal? _prevPlus;
	private decimal? _prevMinus;

	public AdxDmiStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromHours(8).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for strategy calculation", "General");

		_dmiPeriod = Param(nameof(DmiPeriod), 14)
			.SetDisplay("DMI Period", "Directional Movement Index period", "Indicators")
			.SetGreaterThanZero();

		_allowLong = Param(nameof(AllowLong), true)
			.SetDisplay("Allow Long", "Enable long entries", "Trading");

		_allowShort = Param(nameof(AllowShort), true)
			.SetDisplay("Allow Short", "Enable short entries", "Trading");

		_closeLong = Param(nameof(CloseLong), true)
			.SetDisplay("Close Long", "Close long positions on opposite signal", "Trading");

		_closeShort = Param(nameof(CloseShort), true)
			.SetDisplay("Close Short", "Close short positions on opposite signal", "Trading");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int DmiPeriod
	{
		get => _dmiPeriod.Value;
		set => _dmiPeriod.Value = value;
	}

	public bool AllowLong
	{
		get => _allowLong.Value;
		set => _allowLong.Value = value;
	}

	public bool AllowShort
	{
		get => _allowShort.Value;
		set => _allowShort.Value = value;
	}

	public bool CloseLong
	{
		get => _closeLong.Value;
		set => _closeLong.Value = value;
	}

	public bool CloseShort
	{
		get => _closeShort.Value;
		set => _closeShort.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_dmi = new DirectionalIndex
		{
			Length = DmiPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_dmi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _dmi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue dmiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var dmi = (DirectionalIndexValue)dmiValue;

		if (dmi.Plus is not decimal currentPlus ||
			dmi.Minus is not decimal currentMinus)
			return;

		if (_prevPlus is null || _prevMinus is null)
		{
			_prevPlus = currentPlus;
			_prevMinus = currentMinus;
			return;
		}

		var buySignal = _prevMinus > _prevPlus && currentMinus <= currentPlus;
		var sellSignal = _prevPlus > _prevMinus && currentPlus <= currentMinus;

		if (buySignal)
		{
			if (CloseShort && Position < 0)
				BuyMarket(Math.Abs(Position));

			if (AllowLong && Position <= 0)
				BuyMarket(Volume);
		}

		if (sellSignal)
		{
			if (CloseLong && Position > 0)
				SellMarket(Position);

			if (AllowShort && Position >= 0)
				SellMarket(Volume);
		}

		_prevPlus = currentPlus;
		_prevMinus = currentMinus;
	}
}
