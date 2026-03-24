import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class vinicius_setup_atr_strategy(Strategy):
    """EMA trend direction with RSI momentum crossing 50: buy in uptrend, sell in downtrend."""
    def __init__(self):
        super(vinicius_setup_atr_strategy, self).__init__()
        self._ema_length = self.Param("EmaLength", 50).SetGreaterThanZero().SetDisplay("EMA Length", "EMA trend filter length", "General")
        self._rsi_period = self.Param("RsiPeriod", 14).SetDisplay("RSI Period", "RSI length", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(vinicius_setup_atr_strategy, self).OnReseted()
        self._rsi_val = 0
        self._prev_rsi = 0
        self._cooldown = 0

    def OnStarted(self, time):
        super(vinicius_setup_atr_strategy, self).OnStarted(time)
        self._rsi_val = 0
        self._prev_rsi = 0
        self._cooldown = 0

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub \
            .Bind(rsi, self._on_rsi) \
            .Bind(ema, self.OnProcess) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _on_rsi(self, candle, r):
        self._prev_rsi = self._rsi_val
        self._rsi_val = float(r)

    def OnProcess(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        if self._rsi_val == 0 or self._prev_rsi == 0:
            return

        ev = float(ema_val)
        close = float(candle.ClosePrice)
        is_up = close > ev
        is_down = close < ev

        buy_signal = is_up and self._prev_rsi <= 50 and self._rsi_val > 50
        sell_signal = is_down and self._prev_rsi >= 50 and self._rsi_val < 50

        if buy_signal and self.Position <= 0:
            self.BuyMarket()
            self._cooldown = 100
        elif sell_signal and self.Position >= 0:
            self.SellMarket()
            self._cooldown = 100

    def CreateClone(self):
        return vinicius_setup_atr_strategy()
