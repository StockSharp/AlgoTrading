import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class dlmv1_grid_strategy(Strategy):
    def __init__(self):
        super(dlmv1_grid_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 14).SetDisplay("EMA Period", "EMA lookback", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14).SetDisplay("RSI Period", "RSI lookback", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_ema = 0.0
        self._has_prev = False
    @property
    def ema_period(self): return self._ema_period.Value
    @property
    def rsi_period(self): return self._rsi_period.Value
    @property
    def candle_type(self): return self._candle_type.Value
    def OnReseted(self):
        super(dlmv1_grid_strategy, self).OnReseted()
        self._prev_ema = 0.0; self._has_prev = False
    def OnStarted2(self, time):
        super(dlmv1_grid_strategy, self).OnStarted2(time)
        self._has_prev = False
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, rsi, self.process_candle).Start()
    def process_candle(self, candle, ema, rsi):
        if candle.State != CandleStates.Finished: return
        ema_val = float(ema); rsi_val = float(rsi)
        if not self._has_prev:
            self._prev_ema = ema_val; self._has_prev = True; return
        if ema_val > self._prev_ema and rsi_val > 50 and self.Position <= 0:
            if self.Position < 0: self.BuyMarket()
            self.BuyMarket()
        elif ema_val < self._prev_ema and rsi_val < 50 and self.Position >= 0:
            if self.Position > 0: self.SellMarket()
            self.SellMarket()
        self._prev_ema = ema_val
    def CreateClone(self): return dlmv1_grid_strategy()
