import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class super_forex_v2_strategy(Strategy):
    """RSI threshold reversal with ATR trailing stop."""
    def __init__(self):
        super(super_forex_v2_strategy, self).__init__()
        self._rsi_length = self.Param("RsiLength", 4).SetDisplay("RSI Length", "RSI period", "Indicators")
        self._atr_length = self.Param("AtrLength", 14).SetDisplay("ATR Length", "ATR period for trailing", "Indicators")
        self._upper_level = self.Param("UpperLevel", 62.0).SetDisplay("RSI Upper", "Overbought for shorts", "Signals")
        self._lower_level = self.Param("LowerLevel", 42.0).SetDisplay("RSI Lower", "Oversold for longs", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(super_forex_v2_strategy, self).OnReseted()
        self._entry_price = 0
        self._trail_stop = 0

    def OnStarted2(self, time):
        super(super_forex_v2_strategy, self).OnStarted2(time)
        self._entry_price = 0
        self._trail_stop = 0

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(rsi, atr, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, rsi_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        if atr_val <= 0:
            return

        close = float(candle.ClosePrice)
        rsi_val = float(rsi_val)
        atr_val = float(atr_val)
        upper = self._upper_level.Value
        lower = self._lower_level.Value

        # Trailing stop + opposite RSI exit
        if self.Position > 0:
            new_trail = close - atr_val * 1.5
            if new_trail > self._trail_stop:
                self._trail_stop = new_trail
            if close <= self._trail_stop or rsi_val > upper:
                self.SellMarket()
                self._entry_price = 0
                self._trail_stop = 0

        elif self.Position < 0:
            new_trail = close + atr_val * 1.5
            if self._trail_stop == 0 or new_trail < self._trail_stop:
                self._trail_stop = new_trail
            if close >= self._trail_stop or rsi_val < lower:
                self.BuyMarket()
                self._entry_price = 0
                self._trail_stop = 0

        # Entry on RSI levels
        if self.Position == 0:
            if rsi_val < lower:
                self._entry_price = close
                self._trail_stop = close - atr_val * 2
                self.BuyMarket()
            elif rsi_val > upper:
                self._entry_price = close
                self._trail_stop = close + atr_val * 2
                self.SellMarket()

    def CreateClone(self):
        return super_forex_v2_strategy()
