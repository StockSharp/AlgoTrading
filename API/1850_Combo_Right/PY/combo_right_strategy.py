import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class combo_right_strategy(Strategy):
    """
    CCI + three perceptrons strategy. Uses close price differences
    at various shifts as perceptron inputs to filter CCI signals.
    """

    def __init__(self):
        super(combo_right_strategy, self).__init__()
        self._tp1 = self.Param("TakeProfit1", 500.0) \
            .SetDisplay("TP1", "Take profit for basic signal", "General")
        self._sl1 = self.Param("StopLoss1", 500.0) \
            .SetDisplay("SL1", "Stop loss for basic signal", "General")
        self._cci_period = self.Param("CciPeriod", 10) \
            .SetDisplay("CCI Period", "Period of CCI", "General")
        self._x12 = self.Param("X12", 100) \
            .SetDisplay("X12", "Sale perceptron weight", "Perceptron")
        self._x22 = self.Param("X22", 100) \
            .SetDisplay("X22", "Sale perceptron weight", "Perceptron")
        self._x32 = self.Param("X32", 100) \
            .SetDisplay("X32", "Sale perceptron weight", "Perceptron")
        self._x42 = self.Param("X42", 100) \
            .SetDisplay("X42", "Sale perceptron weight", "Perceptron")
        self._tp2 = self.Param("TakeProfit2", 500.0) \
            .SetDisplay("TP2", "Take profit for sale perceptron", "Perceptron")
        self._sl2 = self.Param("StopLoss2", 500.0) \
            .SetDisplay("SL2", "Stop loss for sale perceptron", "Perceptron")
        self._p2 = self.Param("P2", 20) \
            .SetDisplay("P2", "Sale perceptron period", "Perceptron")
        self._x13 = self.Param("X13", 100) \
            .SetDisplay("X13", "Buy perceptron weight", "Perceptron")
        self._x23 = self.Param("X23", 100) \
            .SetDisplay("X23", "Buy perceptron weight", "Perceptron")
        self._x33 = self.Param("X33", 100) \
            .SetDisplay("X33", "Buy perceptron weight", "Perceptron")
        self._x43 = self.Param("X43", 100) \
            .SetDisplay("X43", "Buy perceptron weight", "Perceptron")
        self._tp3 = self.Param("TakeProfit3", 500.0) \
            .SetDisplay("TP3", "Take profit for buy perceptron", "Perceptron")
        self._sl3 = self.Param("StopLoss3", 500.0) \
            .SetDisplay("SL3", "Stop loss for buy perceptron", "Perceptron")
        self._p3 = self.Param("P3", 20) \
            .SetDisplay("P3", "Buy perceptron period", "Perceptron")
        self._x14 = self.Param("X14", 100) \
            .SetDisplay("X14", "General perceptron weight", "Perceptron")
        self._x24 = self.Param("X24", 100) \
            .SetDisplay("X24", "General perceptron weight", "Perceptron")
        self._x34 = self.Param("X34", 100) \
            .SetDisplay("X34", "General perceptron weight", "Perceptron")
        self._x44 = self.Param("X44", 100) \
            .SetDisplay("X44", "General perceptron weight", "Perceptron")
        self._p4 = self.Param("P4", 20) \
            .SetDisplay("P4", "General perceptron period", "Perceptron")
        self._pass_mode = self.Param("Pass", 1) \
            .SetDisplay("Pass", "Mode of operation", "General")
        self._shift = self.Param("Shift", 1) \
            .SetDisplay("Shift", "Bar shift", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._min_cci_signal = self.Param("MinCciSignal", 50.0) \
            .SetDisplay("Minimum CCI", "Minimum absolute CCI value for entries", "Filters")
        self._cooldown_bars = self.Param("CooldownBars", 4) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading")

        self._close_buffer = []
        self._bar_index = 0
        self._cooldown_remaining = 0
        self._previous_signal = 0

        self._w11 = 0.0
        self._w12 = 0.0
        self._w13 = 0.0
        self._w14 = 0.0
        self._w21 = 0.0
        self._w22 = 0.0
        self._w23 = 0.0
        self._w24 = 0.0
        self._w31 = 0.0
        self._w32 = 0.0
        self._w33 = 0.0
        self._w34 = 0.0

        self._sh11 = 0
        self._sh12 = 0
        self._sh13 = 0
        self._sh14 = 0
        self._sh15 = 0
        self._sh21 = 0
        self._sh22 = 0
        self._sh23 = 0
        self._sh24 = 0
        self._sh25 = 0
        self._sh31 = 0
        self._sh32 = 0
        self._sh33 = 0
        self._sh34 = 0
        self._sh35 = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(combo_right_strategy, self).OnReseted()
        self._close_buffer = []
        self._bar_index = 0
        self._cooldown_remaining = 0
        self._previous_signal = 0

    def OnStarted(self, time):
        super(combo_right_strategy, self).OnStarted(time)

        shift = self._shift.Value
        p2 = self._p2.Value
        p3 = self._p3.Value
        p4 = self._p4.Value

        self._w11 = float(self._x12.Value - 100)
        self._w12 = float(self._x22.Value - 100)
        self._w13 = float(self._x32.Value - 100)
        self._w14 = float(self._x42.Value - 100)

        self._w21 = float(self._x13.Value - 100)
        self._w22 = float(self._x23.Value - 100)
        self._w23 = float(self._x33.Value - 100)
        self._w24 = float(self._x43.Value - 100)

        self._w31 = float(self._x14.Value - 100)
        self._w32 = float(self._x24.Value - 100)
        self._w33 = float(self._x34.Value - 100)
        self._w34 = float(self._x44.Value - 100)

        self._sh11 = shift
        self._sh12 = shift + p2
        self._sh13 = shift + p2 * 2
        self._sh14 = shift + p2 * 3
        self._sh15 = shift + p2 * 4

        self._sh21 = shift
        self._sh22 = shift + p3
        self._sh23 = shift + p3 * 2
        self._sh24 = shift + p3 * 3
        self._sh25 = shift + p3 * 4

        self._sh31 = shift
        self._sh32 = shift + p4
        self._sh33 = shift + p4 * 2
        self._sh34 = shift + p4 * 3
        self._sh35 = shift + p4 * 4

        max_shift = max(self._sh15, self._sh25, self._sh35) + 1
        self._close_buffer = [0.0] * max_shift
        self._bar_index = 0

        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(cci, self.on_process).Start()

        self.StartProtection(
            Unit(float(self._tp1.Value), UnitTypes.Absolute),
            Unit(float(self._sl1.Value), UnitTypes.Absolute)
        )

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, cci)
            self.DrawOwnTrades(area)

    def on_process(self, candle, cci_val):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        buf_len = len(self._close_buffer)
        self._close_buffer[self._bar_index % buf_len] = float(candle.ClosePrice)
        self._bar_index += 1

        if self._bar_index <= buf_len:
            return

        raw_signal = self._supervisor(float(cci_val))
        signal = 0
        min_cci = float(self._min_cci_signal.Value)
        if raw_signal > 0 and float(cci_val) >= min_cci:
            signal = 1
        elif raw_signal < 0 and float(cci_val) <= -min_cci:
            signal = -1

        if self._cooldown_remaining == 0:
            if signal > 0 and self._previous_signal <= 0 and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
                self._cooldown_remaining = self._cooldown_bars.Value
            elif signal < 0 and self._previous_signal >= 0 and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
                self._cooldown_remaining = self._cooldown_bars.Value

        if signal != 0:
            self._previous_signal = signal

    def _supervisor(self, basic_signal):
        pass_mode = self._pass_mode.Value

        if pass_mode == 4:
            ok1, out1 = self._perceptron(self._sh11, self._sh12, self._sh13, self._sh14, self._sh15,
                                          self._w11, self._w12, self._w13, self._w14)
            ok2, out2 = self._perceptron(self._sh21, self._sh22, self._sh23, self._sh24, self._sh25,
                                          self._w21, self._w22, self._w23, self._w24)
            ok3, out3 = self._perceptron(self._sh31, self._sh32, self._sh33, self._sh34, self._sh35,
                                          self._w31, self._w32, self._w33, self._w34)
            if not ok1 or not ok2 or not ok3:
                return 0.0
            if out3 > 0:
                return 1.0 if out2 > 0 else basic_signal
            return -1.0 if out1 < 0 else basic_signal

        if pass_mode == 3:
            ok2, out2 = self._perceptron(self._sh21, self._sh22, self._sh23, self._sh24, self._sh25,
                                          self._w21, self._w22, self._w23, self._w24)
            if not ok2:
                return 0.0
            return 1.0 if out2 > 0 else basic_signal

        if pass_mode == 2:
            ok1, out1 = self._perceptron(self._sh11, self._sh12, self._sh13, self._sh14, self._sh15,
                                          self._w11, self._w12, self._w13, self._w14)
            if not ok1:
                return 0.0
            return -1.0 if out1 < 0 else basic_signal

        return basic_signal

    def _perceptron(self, sh1, sh2, sh3, sh4, sh5, w1, w2, w3, w4):
        if self._bar_index <= sh5:
            return False, 0.0

        csh1 = self._get_close(sh1)
        osh2 = self._get_close(sh2)
        osh3 = self._get_close(sh3)
        osh4 = self._get_close(sh4)
        osh5 = self._get_close(sh5)

        a1 = csh1 - osh2
        a2 = osh2 - osh3
        a3 = osh3 - osh4
        a4 = osh4 - osh5

        output = w1 * a1 + w2 * a2 + w3 * a3 + w4 * a4
        return True, output

    def _get_close(self, shift):
        buf_len = len(self._close_buffer)
        index = (self._bar_index - 1 - shift) % buf_len
        if index < 0:
            index += buf_len
        return self._close_buffer[index]

    def CreateClone(self):
        return combo_right_strategy()
