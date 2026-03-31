import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, RelativeStrengthIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class asc_trend_nd_strategy(Strategy):
    def __init__(self):
        super(asc_trend_nd_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of source candles", "General")
        self._sma_period = self.Param("SmaPeriod", 50) \
            .SetDisplay("SMA Period", "Length of simple moving average", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Length of relative strength index", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "Length of average true range", "Risk")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0) \
            .SetDisplay("ATR Multiplier", "ATR multiplier for stop trailing", "Risk")
        self._stop_price = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def sma_period(self):
        return self._sma_period.Value

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def atr_multiplier(self):
        return self._atr_multiplier.Value

    def OnReseted(self):
        super(asc_trend_nd_strategy, self).OnReseted()
        self._stop_price = None

    def OnStarted2(self, time):
        super(asc_trend_nd_strategy, self).OnStarted2(time)
        self._stop_price = None
        sma = SimpleMovingAverage()
        sma.Length = self.sma_period
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        atr = AverageTrueRange()
        atr.Length = self.atr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, rsi, atr, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, sma_value, rsi_value, atr_value):
        if candle.State != CandleStates.Finished:
            return
        price = float(candle.ClosePrice)
        sma_value = float(sma_value)
        rsi_value = float(rsi_value)
        atr_value = float(atr_value)
        atr_mult = float(self.atr_multiplier)
        if self.Position == 0:
            if price > sma_value and rsi_value > 50.0:
                self._stop_price = price - atr_value * atr_mult
                self.BuyMarket()
            elif price < sma_value and rsi_value < 50.0:
                self._stop_price = price + atr_value * atr_mult
                self.SellMarket()
            return
        if self._stop_price is None:
            return
        if self.Position > 0:
            new_stop = price - atr_value * atr_mult
            if new_stop > self._stop_price:
                self._stop_price = new_stop
            if price <= self._stop_price:
                self.SellMarket()
        else:
            new_stop = price + atr_value * atr_mult
            if new_stop < self._stop_price:
                self._stop_price = new_stop
            if price >= self._stop_price:
                self.BuyMarket()

    def CreateClone(self):
        return asc_trend_nd_strategy()
