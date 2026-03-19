import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class crossing_of_two_i_ma_v2_strategy(Strategy):
    """
    Crossing of two MA strategy with optional third MA filter and trailing stop.
    Uses SMA crossover with protection-based stop loss and take profit.
    """

    def __init__(self):
        super(crossing_of_two_i_ma_v2_strategy, self).__init__()
        self._first_period = self.Param("FirstPeriod", 5) \
            .SetDisplay("First MA Period", "Period of the first moving average", "First MA")
        self._second_period = self.Param("SecondPeriod", 8) \
            .SetDisplay("Second MA Period", "Period of the second moving average", "Second MA")
        self._use_filter = self.Param("UseFilter", False) \
            .SetDisplay("Enable Filter", "Use the third moving average as directional filter", "Filter")
        self._third_period = self.Param("ThirdPeriod", 13) \
            .SetDisplay("Third MA Period", "Period of the third moving average filter", "Filter")
        self._stop_loss_pips = self.Param("StopLossPips", 50) \
            .SetDisplay("Stop Loss", "Stop loss distance in pips", "Protection")
        self._take_profit_pips = self.Param("TakeProfitPips", 50) \
            .SetDisplay("Take Profit", "Take profit distance in pips", "Protection")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Candle type used for analysis", "General")

        self._prev_first = 0.0
        self._prev_second = 0.0
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(crossing_of_two_i_ma_v2_strategy, self).OnReseted()
        self._prev_first = 0.0
        self._prev_second = 0.0
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(crossing_of_two_i_ma_v2_strategy, self).OnStarted(time)

        first_ma = SimpleMovingAverage()
        first_ma.Length = self._first_period.Value
        second_ma = SimpleMovingAverage()
        second_ma.Length = self._second_period.Value

        if self._use_filter.Value:
            third_ma = SimpleMovingAverage()
            third_ma.Length = self._third_period.Value
            subscription = self.SubscribeCandles(self.candle_type)
            subscription.Bind(first_ma, second_ma, third_ma, self.on_process_filtered).Start()
        else:
            subscription = self.SubscribeCandles(self.candle_type)
            subscription.Bind(first_ma, second_ma, self.on_process).Start()

        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        if step <= 0:
            step = 1.0

        sl_pips = self._stop_loss_pips.Value
        tp_pips = self._take_profit_pips.Value
        if sl_pips > 0 or tp_pips > 0:
            tp_unit = Unit(float(tp_pips * step), UnitTypes.Absolute) if tp_pips > 0 else None
            sl_unit = Unit(float(sl_pips * step), UnitTypes.Absolute) if sl_pips > 0 else None
            if tp_unit is not None or sl_unit is not None:
                self.StartProtection(tp_unit, sl_unit)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, first_val, second_val):
        if candle.State != CandleStates.Finished:
            return

        first_val = float(first_val)
        second_val = float(second_val)

        if self._prev_first == 0.0 or self._prev_second == 0.0:
            self._prev_first = first_val
            self._prev_second = second_val
            return

        buy_signal = first_val > second_val and self._prev_first < self._prev_second
        sell_signal = first_val < second_val and self._prev_first > self._prev_second

        if buy_signal and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif sell_signal and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_first = first_val
        self._prev_second = second_val

    def on_process_filtered(self, candle, first_val, second_val, third_val):
        if candle.State != CandleStates.Finished:
            return

        first_val = float(first_val)
        second_val = float(second_val)
        third_val = float(third_val)

        if self._prev_first == 0.0 or self._prev_second == 0.0:
            self._prev_first = first_val
            self._prev_second = second_val
            return

        buy_signal = first_val > second_val and self._prev_first < self._prev_second
        sell_signal = first_val < second_val and self._prev_first > self._prev_second

        if buy_signal and third_val < first_val:
            buy_signal = False
        if sell_signal and third_val > first_val:
            sell_signal = False

        if buy_signal and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif sell_signal and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_first = first_val
        self._prev_second = second_val

    def CreateClone(self):
        return crossing_of_two_i_ma_v2_strategy()
