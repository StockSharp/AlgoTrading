import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class macd_rsi_ema_bb_atr_day_trading_strategy(Strategy):
    """
    Day trading: EMA crossover with RSI confirmation and ATR-based stop loss.
    """

    def __init__(self):
        super(macd_rsi_ema_bb_atr_day_trading_strategy, self).__init__()
        self._ema_fast_len = self.Param("EmaFastLen", 9).SetDisplay("Fast EMA", "Fast EMA", "Indicators")
        self._ema_slow_len = self.Param("EmaSlowLen", 21).SetDisplay("Slow EMA", "Slow EMA", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14).SetDisplay("RSI", "RSI period", "Indicators")
        self._atr_length = self.Param("AtrLength", 14).SetDisplay("ATR", "ATR period", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 3.0).SetDisplay("ATR Mult", "ATR stop mult", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(25))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._stop_price = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_rsi_ema_bb_atr_day_trading_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False
        self._stop_price = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(macd_rsi_ema_bb_atr_day_trading_strategy, self).OnStarted(time)
        self._ema_fast = ExponentialMovingAverage()
        self._ema_fast.Length = self._ema_fast_len.Value
        self._ema_slow = ExponentialMovingAverage()
        self._ema_slow.Length = self._ema_slow_len.Value
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self._rsi_length.Value
        self._atr = AverageTrueRange()
        self._atr.Length = self._atr_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema_fast, self._ema_slow, self._rsi, self._atr, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema_fast)
            self.DrawIndicator(area, self._ema_slow)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_val, slow_val, rsi_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._ema_fast.IsFormed or not self._ema_slow.IsFormed or not self._rsi.IsFormed or not self._atr.IsFormed:
            return
        fast = float(fast_val)
        slow = float(slow_val)
        rsi = float(rsi_val)
        atr = float(atr_val)
        if not self._initialized:
            self._prev_fast = fast
            self._prev_slow = slow
            self._initialized = True
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_fast = fast
            self._prev_slow = slow
            return
        close = float(candle.ClosePrice)
        cross_up = self._prev_fast <= self._prev_slow and fast > slow
        cross_down = self._prev_fast >= self._prev_slow and fast < slow
        if cross_up and rsi > 25 and rsi < 80 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._stop_price = close - atr * float(self._atr_multiplier.Value)
            self._cooldown = 8
        elif cross_down and rsi > 20 and rsi < 75 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._stop_price = close + atr * float(self._atr_multiplier.Value)
            self._cooldown = 8
        if self.Position > 0 and self._stop_price > 0 and close <= self._stop_price:
            self.SellMarket()
            self._stop_price = 0.0
            self._cooldown = 10
        elif self.Position < 0 and self._stop_price > 0 and close >= self._stop_price:
            self.BuyMarket()
            self._stop_price = 0.0
            self._cooldown = 10
        self._prev_fast = fast
        self._prev_slow = slow

    def CreateClone(self):
        return macd_rsi_ema_bb_atr_day_trading_strategy()
