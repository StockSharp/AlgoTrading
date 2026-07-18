import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class xauusd10_minute_strategy(Strategy):
    def __init__(self):
        super(xauusd10_minute_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 65) \
            .SetDisplay("RSI Overbought", "Overbought level", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 35) \
            .SetDisplay("RSI Oversold", "Oversold level", "Indicators")
        self._fast_ema = self.Param("FastEma", 12) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_ema = self.Param("SlowEma", 26) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._stop_mult = self.Param("StopMult", 3) \
            .SetDisplay("Stop Mult", "StdDev mult for stop", "Risk")
        self._tp_mult = self.Param("TpMult", 5.0) \
            .SetDisplay("TP Mult", "StdDev mult for TP", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_fast_ema = 0.0
        self._prev_slow_ema = 0.0
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0

    @property
    def rsi_length(self):
        return self._rsi_length.Value

    @property
    def rsi_overbought(self):
        return self._rsi_overbought.Value

    @property
    def rsi_oversold(self):
        return self._rsi_oversold.Value

    @property
    def fast_ema(self):
        return self._fast_ema.Value

    @property
    def slow_ema(self):
        return self._slow_ema.Value

    @property
    def stop_mult(self):
        return self._stop_mult.Value

    @property
    def tp_mult(self):
        return self._tp_mult.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(xauusd10_minute_strategy, self).OnReseted()
        self._prev_fast_ema = 0.0
        self._prev_slow_ema = 0.0
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0

    def OnStarted2(self, time):
        super(xauusd10_minute_strategy, self).OnStarted2(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_length
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.fast_ema
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.slow_ema
        std_dev = StandardDeviation()
        std_dev.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, fast_ema, slow_ema, std_dev, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle, rsi_val, fast_val, slow_val, std_val):
        if candle.State != CandleStates.Finished:
            return
        # TP/SL management
        if self.Position > 0 and self._entry_price > 0:
            if candle.ClosePrice <= self._stop_price or candle.ClosePrice >= self._take_price:
                self.SellMarket()
                self._entry_price = 0
        elif self.Position < 0 and self._entry_price > 0:
            if candle.ClosePrice >= self._stop_price or candle.ClosePrice <= self._take_price:
                self.BuyMarket()
                self._entry_price = 0
        if self._prev_fast_ema == 0 or self._prev_slow_ema == 0 or std_val <= 0:
            self._prev_fast_ema = fast_val
            self._prev_slow_ema = slow_val
            return
        ema_cross_up = self._prev_fast_ema <= self._prev_slow_ema and fast_val > slow_val
        ema_cross_down = self._prev_fast_ema >= self._prev_slow_ema and fast_val < slow_val
        buy_signal = ema_cross_up
        sell_signal = ema_cross_down
        if buy_signal and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = candle.ClosePrice
            self._stop_price = self._entry_price - self.stop_mult * std_val
            self._take_price = self._entry_price + self.tp_mult * std_val
        elif sell_signal and self.Position >= 0:
            self.SellMarket()
            self._entry_price = candle.ClosePrice
            self._stop_price = self._entry_price + self.stop_mult * std_val
            self._take_price = self._entry_price - self.tp_mult * std_val
        self._prev_fast_ema = fast_val
        self._prev_slow_ema = slow_val

    def CreateClone(self):
        return xauusd10_minute_strategy()
