namespace StockSharp.Samples.Strategies;

using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Commodity Channel Index strategy converted from the MetaTrader "CCI_MA v1.5" expert advisor.
/// Uses a primary CCI with a manually computed SMA of CCI values as a signal line.
/// A secondary CCI provides overbought/oversold exit confirmation.
/// </summary>
public class CciMaV15Strategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _signalCciPeriod;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<DataType> _candleType;

	private CommodityChannelIndex _cci;
	private CommodityChannelIndex _signalCci;

	private readonly List<decimal> _cciHistory = new();
	private decimal? _prevCciMa;
	private decimal? _prevCci;
	private decimal? _prevSignalCci;

	/// <summary>
	/// Primary CCI period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Secondary CCI period for exit signals.
	/// </summary>
	public int SignalCciPeriod
	{
		get => _signalCciPeriod.Value;
		set => _signalCciPeriod.Value = value;
	}

	/// <summary>
	/// SMA period applied to the primary CCI values.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss distance in absolute points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance in absolute points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
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
	/// Initializes strategy parameters.
	/// </summary>
	public CciMaV15Strategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Length of the primary CCI", "CCI")
			.SetOptimize(7, 35, 7);

		_signalCciPeriod = Param(nameof(SignalCciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Exit CCI Period", "Length of the secondary CCI", "CCI")
			.SetOptimize(7, 35, 7);

		_maPeriod = Param(nameof(MaPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("CCI MA Period", "SMA length applied to the CCI", "CCI")
			.SetOptimize(3, 21, 3);

		_stopLossPoints = Param(nameof(StopLossPoints), 500m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Protective stop distance in absolute points", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 500m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Profit target distance in absolute points", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Market data series", "General");

		Volume = 1;
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
		_cci = null;
		_signalCci = null;
		_cciHistory.Clear();
		_prevCciMa = null;
		_prevCci = null;
		_prevSignalCci = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		_cci = new CommodityChannelIndex { Length = CciPeriod };
		_signalCci = new CommodityChannelIndex { Length = SignalCciPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_cci, _signalCci, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var indArea = CreateChartArea();
			if (indArea != null)
			{
				DrawIndicator(indArea, _cci);
				DrawIndicator(indArea, _signalCci);
			}
		}

		var tp = TakeProfitPoints > 0 ? new Unit(TakeProfitPoints, UnitTypes.Absolute) : null;
		var sl = StopLossPoints > 0 ? new Unit(StopLossPoints, UnitTypes.Absolute) : null;
		if (tp != null || sl != null)
			StartProtection(tp, sl);

		base.OnStarted2(time);
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue, decimal signalCciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Maintain CCI history for manual SMA calculation
		_cciHistory.Add(cciValue);
		if (_cciHistory.Count > MaPeriod)
			_cciHistory.RemoveAt(0);

		// Compute SMA of CCI
		decimal? cciMa = null;
		if (_cciHistory.Count >= MaPeriod)
		{
			decimal sum = 0;
			for (int i = 0; i < _cciHistory.Count; i++)
				sum += _cciHistory[i];
			cciMa = sum / _cciHistory.Count;
		}

		if (cciMa == null || _prevCci == null || _prevCciMa == null || _prevSignalCci == null)
		{
			_prevCci = cciValue;
			_prevCciMa = cciMa;
			_prevSignalCci = signalCciValue;
			return;
		}

		// Exit logic: secondary CCI overbought/oversold reversal
		if (Position > 0 && _prevSignalCci > 100 && signalCciValue <= 100)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && _prevSignalCci < -100 && signalCciValue >= -100)
		{
			BuyMarket(Math.Abs(Position));
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevCci = cciValue;
			_prevCciMa = cciMa;
			_prevSignalCci = signalCciValue;
			return;
		}

		// Entry: CCI crosses above its MA (buy) or below (sell)
		if (_prevCci < _prevCciMa && cciValue > cciMa.Value && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
		}
		else if (_prevCci > _prevCciMa && cciValue < cciMa.Value && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Position);
			SellMarket(Volume);
		}

		_prevCci = cciValue;
		_prevCciMa = cciMa;
		_prevSignalCci = signalCciValue;
	}
}
