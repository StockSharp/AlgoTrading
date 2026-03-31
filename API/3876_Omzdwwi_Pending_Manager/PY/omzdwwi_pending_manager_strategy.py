import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes
class omzdwwi_pending_manager_strategy(Strategy):
    def __init__(self):
        super(omzdwwi_pending_manager_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14).SetDisplay("RSI Period", "RSI lookback", "Indicators")
        self._sma_period = self.Param("SmaPeriod", 20).SetDisplay("SMA Period", "SMA lookback", "Indicators")
        self._oversold = self.Param("Oversold", 40.0).SetDisplay("Oversold", "RSI oversold level", "Indicators")
        self._overbought = self.Param("Overbought", 60.0).SetDisplay("Overbought", "RSI overbought level", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_rsi = 0.0; self._has_prev = False
    @property
    def rsi_period(self): return self._rsi_period.Value
    @property
    def sma_period(self): return self._sma_period.Value
    @property
    def oversold(self): return self._oversold.Value
    @property
    def overbought(self): return self._overbought.Value
    @property
    def candle_type(self): return self._candle_type.Value
    def OnReseted(self):
        super(omzdwwi_pending_manager_strategy, self).OnReseted()
        self._prev_rsi = 0.0; self._has_prev = False
    def OnStarted2(self, time):
        super(omzdwwi_pending_manager_strategy, self).OnStarted2(time)
        self._prev_rsi = 0.0; self._has_prev = False
        rsi = RelativeStrengthIndex(); rsi.Length = self.rsi_period
        sma = SimpleMovingAverage(); sma.Length = self.sma_period
        sub = self.SubscribeCandles(self.candle_type)
        sub.Bind(rsi, sma, self.process_candle).Start()
        self.StartProtection(takeProfit=Unit(2, UnitTypes.Percent), stopLoss=Unit(1, UnitTypes.Percent))
    def process_candle(self, candle, rsi, sma):
        if candle.State != CandleStates.Finished: return
        close = float(candle.ClosePrice); rsi_val = float(rsi); sma_val = float(sma)
        if not self._has_prev: self._prev_rsi = rsi_val; self._has_prev = True; return
        if self._prev_rsi < self.oversold and rsi_val >= self.oversold and close > sma_val and self.Position == 0:
            self.BuyMarket()
        elif self._prev_rsi > self.overbought and rsi_val <= self.overbought and close < sma_val and self.Position == 0:
            self.SellMarket()
        self._prev_rsi = rsi_val
    def CreateClone(self): return omzdwwi_pending_manager_strategy()
