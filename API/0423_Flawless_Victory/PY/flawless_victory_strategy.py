import clr
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Messages")

from System import TimeSpan, Math
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import RelativeStrengthIndex, MoneyFlowIndex, BollingerBands
from StockSharp.Messages import CandleStates
from datatype_extensions import *

class flawless_victory_strategy(Strategy):
    """Flawless Victory strategy.

    Three configurations combine RSI, MFI and Bollinger Bands. Version 1
    trades RSI signals against Bollinger Bands. Version 2 adds take-profit
    and stop-loss percentages. Version 3 requires both RSI and MFI to agree
    for an entry.
    """

    def __init__(self):
        super(flawless_victory_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._version = self.Param("Version", 1) \
            .SetDisplay("Version", "1=RSI, 2=RSI+TP/SL, 3=RSI+MFI", "General")
        self._rsi_len = self.Param("RsiLength", 14)
        self._mfi_len = self.Param("MfiLength", 14)
        self._bb_len = self.Param("BBLength", 20)
        self._bb_mult = self.Param("BBMultiplier", 2.0)
        self._tp = self.Param("TakeProfitPct", 1.5)
        self._sl = self.Param("StopLossPct", 1.0)

        self._rsi = RelativeStrengthIndex()
        self._mfi = MoneyFlowIndex()
        self._boll = BollingerBands()
        self._entry = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(flawless_victory_strategy, self).OnReseted()
        self._rsi = RelativeStrengthIndex()
        self._mfi = MoneyFlowIndex()
        self._boll = BollingerBands()
        self._entry = 0.0

    def OnStarted(self, time):
        super(flawless_victory_strategy, self).OnStarted(time)
        self._rsi.Length = self._rsi_len.Value
        self._mfi.Length = self._mfi_len.Value
        self._boll.Length = self._bb_len.Value
        self._boll.Width = self._bb_mult.Value
        sub = self.SubscribeCandles(self.candle_type)
        sub.BindEx(self._rsi, self._mfi, self._boll, self._on_process).Start()

    def _on_process(self, candle, rsi_val, mfi_val, boll_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        rsi = rsi_val
        mfi = mfi_val
        up = boll_val.UpBand
        low = boll_val.LowBand

        entry_long = False
        entry_short = False
        exit_long = False
        exit_short = False

        if self._version.Value == 1:
            entry_long = rsi < 30 and candle.ClosePrice > low
            entry_short = rsi > 70 and candle.ClosePrice < up
        elif self._version.Value == 2:
            entry_long = rsi < 30 and candle.ClosePrice > low
            entry_short = rsi > 70 and candle.ClosePrice < up
            if self.Position > 0:
                exit_long = candle.ClosePrice >= self._entry * (1 + self._tp.Value/100) or \
                             candle.ClosePrice <= self._entry * (1 - self._sl.Value/100)
            elif self.Position < 0:
                exit_short = candle.ClosePrice <= self._entry * (1 - self._tp.Value/100) or \
                              candle.ClosePrice >= self._entry * (1 + self._sl.Value/100)
        else:  # version 3
            entry_long = rsi < 30 and mfi < 20 and candle.ClosePrice > low
            entry_short = rsi > 70 and mfi > 80 and candle.ClosePrice < up

        if entry_long and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
            self._entry = candle.ClosePrice
        elif entry_short and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
            self._entry = candle.ClosePrice
        elif exit_long and self.Position > 0:
            self.SellMarket(Math.Abs(self.Position))
            self._entry = 0.0
        elif exit_short and self.Position < 0:
            self.BuyMarket(Math.Abs(self.Position))
            self._entry = 0.0

    def CreateClone(self):
        return flawless_victory_strategy()
