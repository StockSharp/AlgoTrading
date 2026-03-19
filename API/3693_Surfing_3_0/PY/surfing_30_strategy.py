import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class surfing_30_strategy(Strategy):
    """SMA crossover with RSI filter and SL/TP."""
    def __init__(self):
        super(surfing_30_strategy, self).__init__()
        self._tp_points = self.Param("TakeProfitPoints", 80).SetNotNegative().SetDisplay("Take Profit Points", "Distance to TP in points", "Risk Management")
        self._sl_points = self.Param("StopLossPoints", 50).SetNotNegative().SetDisplay("Stop Loss Points", "Distance to SL in points", "Risk Management")
        self._ma_period = self.Param("MaPeriod", 50).SetDisplay("EMA Period", "SMA period for trend", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 10).SetDisplay("RSI Period", "RSI filter period", "Indicators")
        self._long_rsi = self.Param("LongRsiThreshold", 30.0).SetDisplay("Long RSI Threshold", "Min RSI for longs", "Filters")
        self._short_rsi = self.Param("ShortRsiThreshold", 70.0).SetDisplay("Short RSI Threshold", "Max RSI for shorts", "Filters")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(15).TimeFrame()).SetDisplay("Candle Type", "Aggregation for calculations", "Data")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(surfing_30_strategy, self).OnReseted()
        self._prev_close = 0
        self._prev_sma = 0
        self._sl_price = 0
        self._tp_price = 0

    def OnStarted(self, time):
        super(surfing_30_strategy, self).OnStarted(time)
        self._prev_close = 0
        self._prev_sma = 0
        self._sl_price = 0
        self._tp_price = 0

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(sma, rsi, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, sma_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        sma_val = float(sma_val)
        rsi_val = float(rsi_val)
        sl_pts = self._sl_points.Value
        tp_pts = self._tp_points.Value

        # Manage SL/TP
        if self.Position > 0:
            if (self._sl_price > 0 and low <= self._sl_price) or (self._tp_price > 0 and high >= self._tp_price):
                self.SellMarket()
                self._sl_price = 0
                self._tp_price = 0
                self._prev_close = close
                self._prev_sma = sma_val
                return

        elif self.Position < 0:
            if (self._sl_price > 0 and high >= self._sl_price) or (self._tp_price > 0 and low <= self._tp_price):
                self.BuyMarket()
                self._sl_price = 0
                self._tp_price = 0
                self._prev_close = close
                self._prev_sma = sma_val
                return

        if self._prev_close > 0 and self._prev_sma > 0:
            buy_signal = self._prev_close <= self._prev_sma and close > sma_val and rsi_val > self._long_rsi.Value
            sell_signal = self._prev_close >= self._prev_sma and close < sma_val and rsi_val < self._short_rsi.Value

            if buy_signal and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
                self._sl_price = close - sl_pts if sl_pts > 0 else 0
                self._tp_price = close + tp_pts if tp_pts > 0 else 0
            elif sell_signal and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
                self._sl_price = close + sl_pts if sl_pts > 0 else 0
                self._tp_price = close - tp_pts if tp_pts > 0 else 0

        self._prev_close = close
        self._prev_sma = sma_val

    def CreateClone(self):
        return surfing_30_strategy()
