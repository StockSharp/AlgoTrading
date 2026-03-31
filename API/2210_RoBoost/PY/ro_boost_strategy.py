import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class ro_boost_strategy(Strategy):
    def __init__(self):
        super(ro_boost_strategy, self).__init__()
        self._tp = self.Param("TakeProfit", 500.0).SetGreaterThanZero().SetDisplay("Take Profit", "TP distance in points", "Risk")
        self._sl = self.Param("StopLoss", 1000.0).SetGreaterThanZero().SetDisplay("Stop Loss", "SL distance in points", "Risk")
        self._rsi_period = self.Param("RsiPeriod", 7).SetGreaterThanZero().SetDisplay("RSI Period", "RSI length", "Indicator")
        self._rsi_up = self.Param("RsiUp", 50).SetDisplay("RSI Up", "RSI threshold for longs", "Indicator")
        self._rsi_down = self.Param("RsiDown", 50).SetDisplay("RSI Down", "RSI threshold for shorts", "Indicator")
        self._use_trailing = self.Param("UseTrailing", False).SetDisplay("Use Trailing", "Enable trailing stop", "Risk")
        self._trail_start = self.Param("TrailStart", 5.0).SetGreaterThanZero().SetDisplay("Trail Start", "Profit to activate trailing", "Risk")
        self._trail_step = self.Param("TrailStep", 2.0).SetGreaterThanZero().SetDisplay("Trail Step", "Trailing distance from price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(ro_boost_strategy, self).OnReseted()
        self._entry_price = 0
        self._is_long = False
        self._trailing_stop = 0
        self._prev_close = 0
        self._is_first = True

    def OnStarted2(self, time):
        super(ro_boost_strategy, self).OnStarted2(time)
        self._entry_price = 0
        self._is_long = False
        self._trailing_stop = 0
        self._prev_close = 0
        self._is_first = True

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(rsi, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        if self._is_first:
            self._prev_close = close
            self._is_first = False
            return

        if self.Position == 0:
            if self._prev_close > close and rsi_val < self._rsi_down.Value:
                self.SellMarket()
                self._entry_price = close
                self._is_long = False
                self._trailing_stop = 0
            elif self._prev_close <= close and rsi_val >= self._rsi_up.Value:
                self.BuyMarket()
                self._entry_price = close
                self._is_long = True
                self._trailing_stop = 0
        else:
            self._manage_position(close)

        self._prev_close = close

    def _manage_position(self, price):
        if self._entry_price == 0:
            return

        if self._is_long:
            profit = price - self._entry_price
            if profit >= self._tp.Value or -profit >= self._sl.Value:
                self.SellMarket()
                return
            if self._use_trailing.Value:
                if profit >= self._trail_start.Value:
                    new_stop = price - self._trail_step.Value
                    if self._trailing_stop < new_stop:
                        self._trailing_stop = new_stop
                if self._trailing_stop != 0 and price <= self._trailing_stop:
                    self.SellMarket()
        else:
            profit = self._entry_price - price
            if profit >= self._tp.Value or -profit >= self._sl.Value:
                self.BuyMarket()
                return
            if self._use_trailing.Value:
                if profit >= self._trail_start.Value:
                    new_stop = price + self._trail_step.Value
                    if self._trailing_stop == 0 or self._trailing_stop > new_stop:
                        self._trailing_stop = new_stop
                if self._trailing_stop != 0 and price >= self._trailing_stop:
                    self.BuyMarket()

    def CreateClone(self):
        return ro_boost_strategy()
