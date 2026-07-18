import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class get_rich_gbp_session_reversal_strategy(Strategy):
    def __init__(self):
        super(get_rich_gbp_session_reversal_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14).SetDisplay("RSI Period", "RSI lookback", "Indicators")
        self._ema_period = self.Param("EmaPeriod", 50).SetDisplay("EMA Period", "EMA trend filter", "Indicators")
        self._overbought = self.Param("Overbought", 70.0).SetDisplay("Overbought", "RSI overbought level", "Levels")
        self._oversold = self.Param("Oversold", 30.0).SetDisplay("Oversold", "RSI oversold level", "Levels")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_rsi = 0.0
        self._has_prev = False
    @property
    def rsi_period(self): return self._rsi_period.Value
    @property
    def ema_period(self): return self._ema_period.Value
    @property
    def overbought(self): return self._overbought.Value
    @property
    def oversold(self): return self._oversold.Value
    @property
    def candle_type(self): return self._candle_type.Value
    def OnReseted(self):
        super(get_rich_gbp_session_reversal_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._has_prev = False
    def OnStarted2(self, time):
        super(get_rich_gbp_session_reversal_strategy, self).OnStarted2(time)
        self._has_prev = False
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, ema, self.process_candle).Start()
    def process_candle(self, candle, rsi, ema):
        if candle.State != CandleStates.Finished:
            return
        rsi_val = float(rsi)
        if not self._has_prev:
            self._prev_rsi = rsi_val
            self._has_prev = True
            return
        if self._prev_rsi >= self.oversold and rsi_val < self.oversold and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self._prev_rsi <= self.overbought and rsi_val > self.overbought and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_rsi = rsi_val
    def CreateClone(self):
        return get_rich_gbp_session_reversal_strategy()
