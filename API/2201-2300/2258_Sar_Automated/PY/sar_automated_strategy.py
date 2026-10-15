import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy


class sar_automated_strategy(Strategy):
    def __init__(self):
        super(sar_automated_strategy, self).__init__()
        self._sar_step = self.Param("SarStep", 0.002) \
            .SetDisplay("SAR Step", "Acceleration factor for SAR", "Indicators")
        self._sar_max = self.Param("SarMax", 0.02) \
            .SetDisplay("SAR Max", "Maximum acceleration for SAR", "Indicators")
        self._stop_loss = self.Param("StopLoss", 3500.0) \
            .SetDisplay("Stop Loss", "Stop loss in price units", "Risk Management")
        self._take_profit = self.Param("TakeProfit", 6500.0) \
            .SetDisplay("Take Profit", "Take profit in price units", "Risk Management")
        self._trailing_stop = self.Param("TrailingStop", 800.0) \
            .SetDisplay("Trailing Stop", "Trailing stop in price units", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._entry_price = 0.0
        self._highest_price = 0.0
        self._lowest_price = 0.0

    @property
    def sar_step(self):
        return self._sar_step.Value

    @property
    def sar_max(self):
        return self._sar_max.Value

    @property
    def stop_loss(self):
        return self._stop_loss.Value

    @property
    def take_profit(self):
        return self._take_profit.Value

    @property
    def trailing_stop(self):
        return self._trailing_stop.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(sar_automated_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._highest_price = 0.0
        self._lowest_price = 0.0

    def OnStarted2(self, time):
        super(sar_automated_strategy, self).OnStarted2(time)
        sar = ParabolicSar()
        sar.Acceleration = self.sar_step
        sar.AccelerationMax = self.sar_max
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sar, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sar)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, sar_value):
        if candle.State != CandleStates.Finished:
            return
        sar_value = float(sar_value)
        close_price = float(candle.ClosePrice)
        high_price = float(candle.HighPrice)
        low_price = float(candle.LowPrice)
        tp = float(self.take_profit)
        sl = float(self.stop_loss)
        ts = float(self.trailing_stop)
        if self.Position == 0:
            if sar_value < close_price:
                self.BuyMarket()
                self._entry_price = close_price
                self._highest_price = close_price
                self._lowest_price = close_price
            elif sar_value > close_price:
                self.SellMarket()
                self._entry_price = close_price
                self._highest_price = close_price
                self._lowest_price = close_price
        elif self.Position > 0:
            self._highest_price = max(self._highest_price, high_price)
            if high_price - self._entry_price >= tp:
                self.SellMarket()
                return
            if self._entry_price - low_price >= sl:
                self.SellMarket()
                return
            if ts > 0 and self._highest_price - close_price >= ts:
                self.SellMarket()
                return
            if sar_value > close_price:
                self.SellMarket()
        elif self.Position < 0:
            self._lowest_price = min(self._lowest_price, low_price)
            if self._entry_price - low_price >= tp:
                self.BuyMarket()
                return
            if high_price - self._entry_price >= sl:
                self.BuyMarket()
                return
            if ts > 0 and close_price - self._lowest_price >= ts:
                self.BuyMarket()
                return
            if sar_value < close_price:
                self.BuyMarket()

    def CreateClone(self):
        return sar_automated_strategy()
