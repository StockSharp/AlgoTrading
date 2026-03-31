import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class parabolic_sar_limit_strategy(Strategy):
    def __init__(self):
        super(parabolic_sar_limit_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")
        self._ema_length = self.Param("EmaLength", 14).SetDisplay("EMA Length", "EMA period acting as dynamic SAR proxy", "Indicators")
        self._atr_length = self.Param("AtrLength", 14).SetDisplay("ATR Length", "ATR for volatility", "Indicators")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(parabolic_sar_limit_strategy, self).OnReseted()
        self._prev_ema = 0
        self._prev_close = 0
        self._entry_price = 0
        self._was_bullish = False
        self._has_prev = False

    def OnStarted2(self, time):
        super(parabolic_sar_limit_strategy, self).OnStarted2(time)
        self._prev_ema = 0
        self._prev_close = 0
        self._entry_price = 0
        self._was_bullish = False
        self._has_prev = False

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(ema, atr, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ema_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice

        if not self._has_prev:
            self._prev_ema = ema_val
            self._prev_close = close
            self._was_bullish = close > ema_val
            self._has_prev = True
            return

        is_bullish = close > ema_val
        flip = is_bullish != self._was_bullish

        if self.Position > 0 and flip and not is_bullish:
            self.SellMarket()
            self._entry_price = 0
        elif self.Position < 0 and flip and is_bullish:
            self.BuyMarket()
            self._entry_price = 0

        if self.Position == 0 and flip:
            if is_bullish:
                self._entry_price = close
                self.BuyMarket()
            else:
                self._entry_price = close
                self.SellMarket()

        self._prev_ema = ema_val
        self._prev_close = close
        self._was_bullish = is_bullish

    def CreateClone(self):
        return parabolic_sar_limit_strategy()
