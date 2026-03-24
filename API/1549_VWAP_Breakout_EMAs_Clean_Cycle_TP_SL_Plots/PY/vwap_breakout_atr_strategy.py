import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class vwap_breakout_atr_strategy(Strategy):
    def __init__(self):
        super(vwap_breakout_atr_strategy, self).__init__()
        self._ma_length = self.Param("MaLength", 20) \
            .SetDisplay("MA Length", "Moving average period", "Parameters")
        self._std_length = self.Param("StdLength", 14) \
            .SetDisplay("StdDev Length", "Volatility period", "Parameters")
        self._stop_mult = self.Param("StopMult", 1.5) \
            .SetDisplay("Stop Mult", "StdDev multiplier for stop", "Parameters")
        self._take_mult = self.Param("TakeMult", 2.0) \
            .SetDisplay("Take Mult", "StdDev multiplier for TP", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_close = 0.0
        self._prev_ma = 0.0
        self._has_prev = False
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0

    @property
    def ma_length(self):
        return self._ma_length.Value

    @property
    def std_length(self):
        return self._std_length.Value

    @property
    def stop_mult(self):
        return self._stop_mult.Value

    @property
    def take_mult(self):
        return self._take_mult.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vwap_breakout_atr_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_ma = 0.0
        self._has_prev = False
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0

    def OnStarted(self, time):
        super(vwap_breakout_atr_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = self.ma_length
        std_dev = StandardDeviation()
        std_dev.Length = self.std_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, std_dev, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def on_process(self, candle, sma_val, std_val):
        if candle.State != CandleStates.Finished:
            return
        # TP/SL management
        if self.Position > 0:
            if candle.LowPrice <= self._stop_price or candle.HighPrice >= self._take_price:
                self.SellMarket()
                self._entry_price = 0
        elif self.Position < 0:
            if candle.HighPrice >= self._stop_price or candle.LowPrice <= self._take_price:
                self.BuyMarket()
                self._entry_price = 0
        if self._has_prev and std_val > 0:
            cross_over = self._prev_close <= self._prev_ma and candle.ClosePrice > sma_val
            cross_under = self._prev_close >= self._prev_ma and candle.ClosePrice < sma_val
            if cross_over and self.Position <= 0:
                self.BuyMarket()
                self._entry_price = candle.ClosePrice
                self._stop_price = self._entry_price - std_val * self.stop_mult
                self._take_price = self._entry_price + std_val * self.take_mult
            elif cross_under and self.Position >= 0:
                self.SellMarket()
                self._entry_price = candle.ClosePrice
                self._stop_price = self._entry_price + std_val * self.stop_mult
                self._take_price = self._entry_price - std_val * self.take_mult
        self._prev_close = candle.ClosePrice
        self._prev_ma = sma_val
        self._has_prev = True

    def CreateClone(self):
        return vwap_breakout_atr_strategy()
