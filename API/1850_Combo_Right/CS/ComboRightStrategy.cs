using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Implementation of strategy converted from MQL "Combo_Right".
/// Uses CCI indicator and three simple perceptrons to generate trade signals.
/// </summary>
public class ComboRightStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tp1;
	private readonly StrategyParam<decimal> _sl1;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _x12;
	private readonly StrategyParam<int> _x22;
	private readonly StrategyParam<int> _x32;
	private readonly StrategyParam<int> _x42;
	private readonly StrategyParam<decimal> _tp2;
	private readonly StrategyParam<decimal> _sl2;
	private readonly StrategyParam<int> _p2;
	private readonly StrategyParam<int> _x13;
	private readonly StrategyParam<int> _x23;
	private readonly StrategyParam<int> _x33;
	private readonly StrategyParam<int> _x43;
	private readonly StrategyParam<decimal> _tp3;
	private readonly StrategyParam<decimal> _sl3;
	private readonly StrategyParam<int> _p3;
	private readonly StrategyParam<int> _x14;
	private readonly StrategyParam<int> _x24;
	private readonly StrategyParam<int> _x34;
	private readonly StrategyParam<int> _x44;
	private readonly StrategyParam<int> _p4;
	private readonly StrategyParam<int> _pass;
	private readonly StrategyParam<int> _shift;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal[] _closeBuffer = Array.Empty<decimal>();
	private int _barIndex;

	private decimal _w11, _w12, _w13, _w14;
	private decimal _w21, _w22, _w23, _w24;
	private decimal _w31, _w32, _w33, _w34;

	private int _sh11, _sh12, _sh13, _sh14, _sh15;
	private int _sh21, _sh22, _sh23, _sh24, _sh25;
	private int _sh31, _sh32, _sh33, _sh34, _sh35;

	public decimal TakeProfit1 { get => _tp1.Value; set => _tp1.Value = value; }
	public decimal StopLoss1 { get => _sl1.Value; set => _sl1.Value = value; }
	public int CciPeriod { get => _cciPeriod.Value; set => _cciPeriod.Value = value; }
	public int X12 { get => _x12.Value; set => _x12.Value = value; }
	public int X22 { get => _x22.Value; set => _x22.Value = value; }
	public int X32 { get => _x32.Value; set => _x32.Value = value; }
	public int X42 { get => _x42.Value; set => _x42.Value = value; }
	public decimal TakeProfit2 { get => _tp2.Value; set => _tp2.Value = value; }
	public decimal StopLoss2 { get => _sl2.Value; set => _sl2.Value = value; }
	public int P2 { get => _p2.Value; set => _p2.Value = value; }
	public int X13 { get => _x13.Value; set => _x13.Value = value; }
	public int X23 { get => _x23.Value; set => _x23.Value = value; }
	public int X33 { get => _x33.Value; set => _x33.Value = value; }
	public int X43 { get => _x43.Value; set => _x43.Value = value; }
	public decimal TakeProfit3 { get => _tp3.Value; set => _tp3.Value = value; }
	public decimal StopLoss3 { get => _sl3.Value; set => _sl3.Value = value; }
	public int P3 { get => _p3.Value; set => _p3.Value = value; }
	public int X14 { get => _x14.Value; set => _x14.Value = value; }
	public int X24 { get => _x24.Value; set => _x24.Value = value; }
	public int X34 { get => _x34.Value; set => _x34.Value = value; }
	public int X44 { get => _x44.Value; set => _x44.Value = value; }
	public int P4 { get => _p4.Value; set => _p4.Value = value; }
	public int Pass { get => _pass.Value; set => _pass.Value = value; }
	public int Shift { get => _shift.Value; set => _shift.Value = value; }
	public new decimal Volume { get => _volume.Value; set => _volume.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ComboRightStrategy()
	{
		_tp1 = Param(nameof(TakeProfit1), 500m).SetDisplay("TP1", "Take profit for basic signal", "General");
		_sl1 = Param(nameof(StopLoss1), 500m).SetDisplay("SL1", "Stop loss for basic signal", "General");
		_cciPeriod = Param(nameof(CciPeriod), 10).SetDisplay("CCI Period", "Period of CCI", "General");
		_x12 = Param(nameof(X12), 100).SetDisplay("X12", "Sale perceptron weight", "Perceptron");
		_x22 = Param(nameof(X22), 100).SetDisplay("X22", "Sale perceptron weight", "Perceptron");
		_x32 = Param(nameof(X32), 100).SetDisplay("X32", "Sale perceptron weight", "Perceptron");
		_x42 = Param(nameof(X42), 100).SetDisplay("X42", "Sale perceptron weight", "Perceptron");
		_tp2 = Param(nameof(TakeProfit2), 500m).SetDisplay("TP2", "Take profit for sale perceptron", "Perceptron");
		_sl2 = Param(nameof(StopLoss2), 500m).SetDisplay("SL2", "Stop loss for sale perceptron", "Perceptron");
		_p2 = Param(nameof(P2), 20).SetDisplay("P2", "Sale perceptron period", "Perceptron");
		_x13 = Param(nameof(X13), 100).SetDisplay("X13", "Buy perceptron weight", "Perceptron");
		_x23 = Param(nameof(X23), 100).SetDisplay("X23", "Buy perceptron weight", "Perceptron");
		_x33 = Param(nameof(X33), 100).SetDisplay("X33", "Buy perceptron weight", "Perceptron");
		_x43 = Param(nameof(X43), 100).SetDisplay("X43", "Buy perceptron weight", "Perceptron");
		_tp3 = Param(nameof(TakeProfit3), 500m).SetDisplay("TP3", "Take profit for buy perceptron", "Perceptron");
		_sl3 = Param(nameof(StopLoss3), 500m).SetDisplay("SL3", "Stop loss for buy perceptron", "Perceptron");
		_p3 = Param(nameof(P3), 20).SetDisplay("P3", "Buy perceptron period", "Perceptron");
		_x14 = Param(nameof(X14), 100).SetDisplay("X14", "General perceptron weight", "Perceptron");
		_x24 = Param(nameof(X24), 100).SetDisplay("X24", "General perceptron weight", "Perceptron");
		_x34 = Param(nameof(X34), 100).SetDisplay("X34", "General perceptron weight", "Perceptron");
		_x44 = Param(nameof(X44), 100).SetDisplay("X44", "General perceptron weight", "Perceptron");
		_p4 = Param(nameof(P4), 20).SetDisplay("P4", "General perceptron period", "Perceptron");
		_pass = Param(nameof(Pass), 1).SetDisplay("Pass", "Mode of operation", "General");
		_shift = Param(nameof(Shift), 1).SetDisplay("Shift", "Bar shift", "General");
		_volume = Param(nameof(Volume), 0.1m).SetDisplay("Volume", "Trading volume", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame()).SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_closeBuffer = Array.Empty<decimal>();
		_barIndex = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_w11 = X12 - 100;
		_w12 = X22 - 100;
		_w13 = X32 - 100;
		_w14 = X42 - 100;

		_w21 = X13 - 100;
		_w22 = X23 - 100;
		_w23 = X33 - 100;
		_w24 = X43 - 100;

		_w31 = X14 - 100;
		_w32 = X24 - 100;
		_w33 = X34 - 100;
		_w34 = X44 - 100;

		_sh11 = Shift;
		_sh12 = Shift + P2;
		_sh13 = Shift + P2 * 2;
		_sh14 = Shift + P2 * 3;
		_sh15 = Shift + P2 * 4;

		_sh21 = Shift;
		_sh22 = Shift + P3;
		_sh23 = Shift + P3 * 2;
		_sh24 = Shift + P3 * 3;
		_sh25 = Shift + P3 * 4;

		_sh31 = Shift;
		_sh32 = Shift + P4;
		_sh33 = Shift + P4 * 2;
		_sh34 = Shift + P4 * 3;
		_sh35 = Shift + P4 * 4;

		var maxShift = Math.Max(Math.Max(_sh15, _sh25), _sh35) + 1;
		_closeBuffer = new decimal[maxShift];
		_barIndex = 0;

		var cci = new CCI { Length = CciPeriod };
		var sub = SubscribeCandles(CandleType);
		sub.Bind(cci, ProcessCandle).Start();

		StartProtection(new Unit(TakeProfit1, UnitTypes.Step), new Unit(StopLoss1, UnitTypes.Step));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawIndicator(area, cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var len = _closeBuffer.Length;
		_closeBuffer[_barIndex % len] = candle.ClosePrice;
		_barIndex++;

		if (_barIndex <= len)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var signal = Supervisor(cciValue);

		if (signal > 0 && Position <= 0)
		{
		BuyMarket(Volume + Math.Abs(Position));
		}
		else if (signal < 0 && Position >= 0)
		{
		SellMarket(Volume + Math.Abs(Position));
		}
	}

	private decimal Supervisor(decimal basicSig)
	{
		decimal signal = 0;

		if (Pass == 4)
		{
		if (!Perceptron(out var output1, _sh11, _sh12, _sh13, _sh14, _sh15, _w11, _w12, _w13, _w14) ||
		!Perceptron(out var output2, _sh21, _sh22, _sh23, _sh24, _sh25, _w21, _w22, _w23, _w24) ||
		!Perceptron(out var output3, _sh31, _sh32, _sh33, _sh34, _sh35, _w31, _w32, _w33, _w34))
		return 0;

		if (output3 > 0)
		{
		if (output2 > 0)
		signal = 1;
		}
		else
		{
		if (output1 < 0)
		signal = -1;
		}

		if (signal == 0)
		signal = basicSig;

		return signal;
		}

		if (Pass == 3)
		{
		if (!Perceptron(out var output2, _sh21, _sh22, _sh23, _sh24, _sh25, _w21, _w22, _w23, _w24))
		return 0;
		return output2 > 0 ? 1 : basicSig;
		}

		if (Pass == 2)
		{
		if (!Perceptron(out var output1, _sh11, _sh12, _sh13, _sh14, _sh15, _w11, _w12, _w13, _w14))
		return 0;
		return output1 < 0 ? -1 : basicSig;
		}

		return basicSig;
	}

	private bool Perceptron(out decimal output, int sh1, int sh2, int sh3, int sh4, int sh5,
		decimal w1, decimal w2, decimal w3, decimal w4)
	{
		output = 0m;
		var len = _closeBuffer.Length;
		if (_barIndex <= sh5)
		return false;

		var csh1 = GetClose(sh1, len);
		var osh2 = GetClose(sh2, len);
		var osh3 = GetClose(sh3, len);
		var osh4 = GetClose(sh4, len);
		var osh5 = GetClose(sh5, len);

		var a1 = csh1 - osh2;
		var a2 = osh2 - osh3;
		var a3 = osh3 - osh4;
		var a4 = osh4 - osh5;

		output = w1 * a1 + w2 * a2 + w3 * a3 + w4 * a4;
		return true;
	}

	private decimal GetClose(int shift, int len)
	{
		var index = (_barIndex - 1 - shift) % len;
		if (index < 0)
		index += len;
		return _closeBuffer[index];
	}
}
