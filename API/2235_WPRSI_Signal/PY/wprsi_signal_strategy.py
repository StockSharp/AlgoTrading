import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import WilliamsR, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class wprsi_signal_strategy(Strategy):
    def __init__(self):
        super(wprsi_signal_strategy, self).__init__()
        self._period = self.Param("Period", 27) \
            .SetDisplay("Period", "Period for WPR and RSI", "Parameters")
        self._filter_up = self.Param("FilterUp", 10) \
            .SetDisplay("Filter Up", "Bars to confirm buy", "Parameters")
        self._filter_down = self.Param("FilterDown", 10) \
            .SetDisplay("Filter Down", "Bars to confirm sell", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for candles", "Parameters")
        self._prev_wpr = 0.0
        self._is_prev_init = False
        self._pending_buy = False
        self._pending_sell = False
        self._up_counter = 0
        self._down_counter = 0

    @property
    def period(self):
        return self._period.Value

    @property
    def filter_up(self):
        return self._filter_up.Value

    @property
    def filter_down(self):
        return self._filter_down.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(wprsi_signal_strategy, self).OnReseted()
        self._prev_wpr = 0.0
        self._is_prev_init = False
        self._pending_buy = False
        self._pending_sell = False
        self._up_counter = 0
        self._down_counter = 0

    def OnStarted2(self, time):
        super(wprsi_signal_strategy, self).OnStarted2(time)
        wpr = WilliamsR()
        wpr.Length = self.period
        rsi = RelativeStrengthIndex()
        rsi.Length = self.period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(wpr, rsi, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, wpr)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, wpr_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        wpr_value = float(wpr_value)
        rsi_value = float(rsi_value)
        if not self._is_prev_init:
            self._prev_wpr = wpr_value
            self._is_prev_init = True
            return
        if self._pending_buy:
            if wpr_value <= -20:
                self._pending_buy = False
            else:
                self._up_counter -= 1
                if self._up_counter <= 0:
                    if rsi_value > 50 and self.Position <= 0:
                        self.BuyMarket()
                    self._pending_buy = False
        elif self._prev_wpr < -20 and wpr_value > -20 and rsi_value > 50:
            self._pending_buy = True
            self._up_counter = self.filter_up
        if self._pending_sell:
            if wpr_value >= -80:
                self._pending_sell = False
            else:
                self._down_counter -= 1
                if self._down_counter <= 0:
                    if rsi_value < 50 and self.Position >= 0:
                        self.SellMarket()
                    self._pending_sell = False
        elif self._prev_wpr > -80 and wpr_value < -80 and rsi_value < 50:
            self._pending_sell = True
            self._down_counter = self.filter_down
        self._prev_wpr = wpr_value

    def CreateClone(self):
        return wprsi_signal_strategy()
