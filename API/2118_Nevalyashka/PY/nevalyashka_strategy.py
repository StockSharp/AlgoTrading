import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class nevalyashka_strategy(Strategy):
    def __init__(self):
        super(nevalyashka_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Parameters")
        self._overbought = self.Param("Overbought", 65.0) \
            .SetDisplay("Overbought", "Overbought level", "Parameters")
        self._oversold = self.Param("Oversold", 35.0) \
            .SetDisplay("Oversold", "Oversold level", "Parameters")
        self._entry_price = 0.0
        self._prev_rsi = 50.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def overbought(self):
        return self._overbought.Value

    @property
    def oversold(self):
        return self._oversold.Value

    def OnReseted(self):
        super(nevalyashka_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._prev_rsi = 50.0
        self._has_prev = False

    def OnStarted(self, time):
        super(nevalyashka_strategy, self).OnStarted(time)
        self._entry_price = 0.0
        self._prev_rsi = 50.0
        self._has_prev = False
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        rsi_value = float(rsi_value)
        price = float(candle.ClosePrice)
        if not self._has_prev:
            self._prev_rsi = rsi_value
            self._has_prev = True
            return
        ob = float(self.overbought)
        os_level = float(self.oversold)
        # Exit on TP/SL
        if self.Position > 0 and self._entry_price > 0:
            pnl_pct = (price - self._entry_price) / self._entry_price * 100.0
            if pnl_pct >= 2.0 or pnl_pct <= -1.0 or rsi_value > ob:
                self.SellMarket()
                self._entry_price = 0.0
        elif self.Position < 0 and self._entry_price > 0:
            pnl_pct = (self._entry_price - price) / self._entry_price * 100.0
            if pnl_pct >= 2.0 or pnl_pct <= -1.0 or rsi_value < os_level:
                self.BuyMarket()
                self._entry_price = 0.0
        # Entry signals
        if self.Position == 0:
            if self._prev_rsi <= os_level and rsi_value > os_level:
                self.BuyMarket()
                self._entry_price = price
            elif self._prev_rsi >= ob and rsi_value < ob:
                self.SellMarket()
                self._entry_price = price
        self._prev_rsi = rsi_value

    def CreateClone(self):
        return nevalyashka_strategy()
