using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Rank Correlation Index strategy that trades on RCI/MA crossovers.
/// </summary>
public class RciStrategy : Strategy
{
	private readonly StrategyParam<int> _rciLength;
	private readonly StrategyParam<string> _maType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<string> _direction;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRci;
	private decimal _prevMa;
	private bool _isInitialized;

	/// <summary>
	/// Rank Correlation Index period.
	/// </summary>
	public int RciLength
	{
		get => _rciLength.Value;
		set => _rciLength.Value = value;
	}

	/// <summary>
	/// Moving average type.
	/// </summary>
	public string MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Allowed trade direction.
	/// </summary>
	public string Direction
	{
		get => _direction.Value;
		set => _direction.Value = value;
	}

	/// <summary>
	/// Type of candles for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public RciStrategy()
	{
		_rciLength = Param(nameof(RciLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("RCI Length", "Rank Correlation Index period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_maType = Param(nameof(MaType), "SMA")
			.SetDisplay("MA Type", "Moving average type", "Parameters");

		_maLength = Param(nameof(MaLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average period", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 5);

		_direction = Param(nameof(Direction), "Long & Short")
			.SetDisplay("Trade Direction", "Allowed trade direction", "Parameters");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy", "General");
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
		_prevRci = 0m;
		_prevMa = 0m;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var rci = new RankCorrelationIndex { Length = RciLength };

		IIndicator? rciMa = MaType switch
		{
			"SMA" => new SMA { Length = MaLength },
			"EMA" => new EMA { Length = MaLength },
			"SMMA (RMA)" => new SMMA { Length = MaLength },
			"WMA" => new WMA { Length = MaLength },
			"VWMA" => new VWMA { Length = MaLength },
			_ => null
		};

		var subscription = SubscribeCandles(CandleType);

		if (rciMa != null)
		{
			subscription.Bind(rci, rciMa, Process).Start();
		}
		else
		{
			subscription.Bind(rci, (candle, value) => { }).Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rci);
			if (rciMa != null)
				DrawIndicator(area, rciMa);
			DrawOwnTrades(area);
		}
	}

	private void Process(ICandleMessage candle, decimal rciValue, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_isInitialized)
		{
			_prevRci = rciValue;
			_prevMa = maValue;
			_isInitialized = true;
			return;
		}

		var longCond = _prevRci <= _prevMa && rciValue > maValue;
		var shortCond = _prevRci >= _prevMa && rciValue < maValue;

		var canLong = Direction != "Short Only";
		var canShort = Direction != "Long Only";

		if (longCond)
		{
			if (canLong && Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
			else if (!canLong && Position < 0)
				BuyMarket(Math.Abs(Position));
		}
		else if (shortCond)
		{
			if (canShort && Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
			else if (!canShort && Position > 0)
				SellMarket(Math.Abs(Position));
		}

		_prevRci = rciValue;
		_prevMa = maValue;
	}
}
