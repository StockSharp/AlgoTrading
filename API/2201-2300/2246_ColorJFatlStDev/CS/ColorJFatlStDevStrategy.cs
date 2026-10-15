using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Jurik moving average and standard deviation bands.
/// Opens and closes positions according to signal modes.
/// </summary>
public class ColorJFatlStDevStrategy : Strategy
{
	public enum SignalModes
	{
		Point,
		Direct,
		Without
	}

	private readonly StrategyParam<int> _jmaLength;
	private readonly StrategyParam<int> _jmaPhase;
	private readonly StrategyParam<int> _stdPeriod;
	private readonly StrategyParam<decimal> _k1;
	private readonly StrategyParam<decimal> _k2;
	private readonly StrategyParam<SignalModes> _buyOpenMode;
	private readonly StrategyParam<SignalModes> _sellOpenMode;
	private readonly StrategyParam<SignalModes> _buyCloseMode;
	private readonly StrategyParam<SignalModes> _sellCloseMode;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevJma;
	private decimal? _prevPrevJma;

	public int JmaLength { get => _jmaLength.Value; set => _jmaLength.Value = value; }
	public int JmaPhase { get => _jmaPhase.Value; set => _jmaPhase.Value = value; }
	public int StdPeriod { get => _stdPeriod.Value; set => _stdPeriod.Value = value; }
	public decimal K1 { get => _k1.Value; set => _k1.Value = value; }
	public decimal K2 { get => _k2.Value; set => _k2.Value = value; }
	public SignalModes BuyOpenMode { get => _buyOpenMode.Value; set => _buyOpenMode.Value = value; }
	public SignalModes SellOpenMode { get => _sellOpenMode.Value; set => _sellOpenMode.Value = value; }
	public SignalModes BuyCloseMode { get => _buyCloseMode.Value; set => _buyCloseMode.Value = value; }
	public SignalModes SellCloseMode { get => _sellCloseMode.Value; set => _sellCloseMode.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ColorJFatlStDevStrategy()
	{
		_jmaLength = Param(nameof(JmaLength), 5)
			.SetDisplay("JMA Length", "JMA period", "Indicators");

		_jmaPhase = Param(nameof(JmaPhase), -100)
			.SetDisplay("JMA Phase", "JMA phase", "Indicators");

		_stdPeriod = Param(nameof(StdPeriod), 9)
			.SetDisplay("Std Period", "Standard deviation period", "Indicators");

		_k1 = Param(nameof(K1), 0.5m)
			.SetDisplay("K1", "First deviation multiplier", "Parameters");

		_k2 = Param(nameof(K2), 1.0m)
			.SetDisplay("K2", "Second deviation multiplier", "Parameters");

		_buyOpenMode = Param(nameof(BuyOpenMode), SignalModes.Point)
			.SetDisplay("Buy Open", "Mode for opening long", "Signals");

		_sellOpenMode = Param(nameof(SellOpenMode), SignalModes.Point)
			.SetDisplay("Sell Open", "Mode for opening short", "Signals");

		_buyCloseMode = Param(nameof(BuyCloseMode), SignalModes.Point)
			.SetDisplay("Buy Close", "Mode for closing long", "Signals");

		_sellCloseMode = Param(nameof(SellCloseMode), SignalModes.Point)
			.SetDisplay("Sell Close", "Mode for closing short", "Signals");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevJma = null;
		_prevPrevJma = null;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevJma = null;
		_prevPrevJma = null;

		var jma = new JurikMovingAverage { Length = JmaLength, Phase = JmaPhase };
		var std = new StandardDeviation { Length = StdPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(jma, std, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, jma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal jmaValue, decimal stdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevJma is null || _prevPrevJma is null)
		{
			_prevPrevJma = _prevJma;
			_prevJma = jmaValue;
			return;
		}

		if (stdValue == 0)
		{
			_prevPrevJma = _prevJma;
			_prevJma = jmaValue;
			return;
		}

		var upper1 = jmaValue + K1 * stdValue;
		var upper2 = jmaValue + K2 * stdValue;
		var lower1 = jmaValue - K1 * stdValue;
		var lower2 = jmaValue - K2 * stdValue;

		var buyOpen = false;
		var sellOpen = false;
		var buyClose = false;
		var sellClose = false;

		switch (BuyOpenMode)
		{
			case SignalModes.Point:
				buyOpen = candle.ClosePrice > upper1 || candle.ClosePrice > upper2;
				break;
			case SignalModes.Direct:
				buyOpen = jmaValue > _prevJma && _prevJma < _prevPrevJma;
				break;
		}

		switch (SellOpenMode)
		{
			case SignalModes.Point:
				sellOpen = candle.ClosePrice < lower1 || candle.ClosePrice < lower2;
				break;
			case SignalModes.Direct:
				sellOpen = jmaValue < _prevJma && _prevJma > _prevPrevJma;
				break;
		}

		switch (BuyCloseMode)
		{
			case SignalModes.Point:
				buyClose = candle.ClosePrice < lower1 || candle.ClosePrice < lower2;
				break;
			case SignalModes.Direct:
				buyClose = jmaValue > _prevJma;
				break;
		}

		switch (SellCloseMode)
		{
			case SignalModes.Point:
				sellClose = candle.ClosePrice > upper1 || candle.ClosePrice > upper2;
				break;
			case SignalModes.Direct:
				sellClose = jmaValue < _prevJma;
				break;
		}

		if (buyClose && Position > 0)
			SellMarket();
		else if (sellClose && Position < 0)
			BuyMarket();
		else if (buyOpen && Position <= 0)
			BuyMarket();
		else if (sellOpen && Position >= 0)
			SellMarket();

		_prevPrevJma = _prevJma;
		_prevJma = jmaValue;
	}
}
