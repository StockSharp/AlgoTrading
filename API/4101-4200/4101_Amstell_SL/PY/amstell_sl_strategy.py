import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import AverageTrueRange, ExponentialMovingAverage

class amstell_sl_strategy(Strategy):
    def __init__(self):
        super(amstell_sl_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8))) \
            .SetDisplay("Candle Type", "Timeframe.", "General")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period.", "Indicators")
        self._ema_length = self.Param("EmaLength", 50) \
            .SetDisplay("EMA Length", "EMA trend filter.", "Indicators")

        self._entry_price = 0.0
        self._prev_ema = 0.0
        self._grid_count = 0
        self._cooldown = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def AtrLength(self):
        return self._atr_length.Value

    @property
    def EmaLength(self):
        return self._ema_length.Value

    def OnStarted2(self, time):
        super(amstell_sl_strategy, self).OnStarted2(time)

        self._entry_price = 0.0
        self._prev_ema = 0.0
        self._grid_count = 0
        self._cooldown = 0

        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrLength
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.EmaLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._atr, self._ema, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, atr_val, ema_val):
        if candle.State != CandleStates.Finished:
            return

        av = float(atr_val)
        ev = float(ema_val)

        if av <= 0 or self._prev_ema == 0:
            self._prev_ema = ev
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_ema = ev
            if self.Position == 0:
                return

        close = float(candle.ClosePrice)

        # Position management with grid and stops
        if self.Position > 0:
            if close >= self._entry_price + av * 2.5:
                self.SellMarket()
                self._entry_price = 0.0
                self._grid_count = 0
                self._cooldown = 10
            elif close <= self._entry_price - av * 4.0:
                self.SellMarket()
                self._entry_price = 0.0
                self._grid_count = 0
                self._cooldown = 10
            elif self._grid_count < 1 and close <= self._entry_price - av * 2.0:
                self._entry_price = (self._entry_price + close) / 2.0
                self._grid_count += 1
                self.BuyMarket()
        elif self.Position < 0:
            if close <= self._entry_price - av * 2.5:
                self.BuyMarket()
                self._entry_price = 0.0
                self._grid_count = 0
                self._cooldown = 10
            elif close >= self._entry_price + av * 4.0:
                self.BuyMarket()
                self._entry_price = 0.0
                self._grid_count = 0
                self._cooldown = 10
            elif self._grid_count < 1 and close >= self._entry_price + av * 2.0:
                self._entry_price = (self._entry_price + close) / 2.0
                self._grid_count += 1
                self.SellMarket()

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_ema = ev
            return

        # Entry on EMA trend
        if self.Position == 0 and self._cooldown == 0:
            if close > ev and ev > self._prev_ema:
                self._entry_price = close
                self._grid_count = 0
                self.BuyMarket()
            elif close < ev and ev < self._prev_ema:
                self._entry_price = close
                self._grid_count = 0
                self.SellMarket()

        self._prev_ema = ev

    def OnReseted(self):
        super(amstell_sl_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._prev_ema = 0.0
        self._grid_count = 0
        self._cooldown = 0

    def CreateClone(self):
        return amstell_sl_strategy()
