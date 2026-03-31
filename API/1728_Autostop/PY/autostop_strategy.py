import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class autostop_strategy(Strategy):
    def __init__(self):
        super(autostop_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 10) \
            .SetDisplay("Fast EMA", "Fast EMA period", "General")
        self._slow_length = self.Param("SlowLength", 20) \
            .SetDisplay("Slow EMA", "Slow EMA period", "General")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period for TP/SL", "Risk")
        self._tp_multiplier = self.Param("TpMultiplier", 2.0) \
            .SetDisplay("TP Multiplier", "ATR multiplier for take profit", "Risk")
        self._sl_multiplier = self.Param("SlMultiplier", 1.5) \
            .SetDisplay("SL Multiplier", "ATR multiplier for stop loss", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._entry_price = 0.0
        self._take_price = 0.0
        self._stop_price = 0.0

    @property
    def fast_length(self):
        return self._fast_length.Value

    @property
    def slow_length(self):
        return self._slow_length.Value

    @property
    def atr_length(self):
        return self._atr_length.Value

    @property
    def tp_multiplier(self):
        return self._tp_multiplier.Value

    @property
    def sl_multiplier(self):
        return self._sl_multiplier.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(autostop_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._take_price = 0.0
        self._stop_price = 0.0

    def OnStarted2(self, time):
        super(autostop_strategy, self).OnStarted2(time)
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.fast_length
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.slow_length
        atr = StandardDeviation()
        atr.Length = self.atr_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_ema, slow_ema, atr, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def on_process(self, candle, fast, slow, atr_value):
        if candle.State != CandleStates.Finished:
            return
        # Check TP/SL for existing positions
        if self.Position > 0:
            if candle.ClosePrice >= self._take_price or candle.ClosePrice <= self._stop_price:
                self.SellMarket()
                return
        elif self.Position < 0:
            if candle.ClosePrice <= self._take_price or candle.ClosePrice >= self._stop_price:
                self.BuyMarket()
                return
        # Entry signals
        if fast > slow and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = candle.ClosePrice
            self._take_price = self._entry_price + atr_value * self.tp_multiplier
            self._stop_price = self._entry_price - atr_value * self.sl_multiplier
        elif fast < slow and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = candle.ClosePrice
            self._take_price = self._entry_price - atr_value * self.tp_multiplier
            self._stop_price = self._entry_price + atr_value * self.sl_multiplier

    def CreateClone(self):
        return autostop_strategy()
