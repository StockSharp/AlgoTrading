import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ParabolicSar, DecimalIndicatorValue, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class rsi_bollinger_fractal_breakout_strategy(Strategy):
    def __init__(self):
        super(rsi_bollinger_fractal_breakout_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 8).SetGreaterThanZero().SetDisplay("RSI Period", "RSI lookback", "RSI")
        self._rsi_upper = self.Param("RsiUpper", 75.0).SetDisplay("RSI Upper", "Overbought threshold", "RSI")
        self._rsi_lower = self.Param("RsiLower", 25.0).SetDisplay("RSI Lower", "Oversold threshold", "RSI")
        self._sl_pips = self.Param("StopLossPips", 135.0).SetDisplay("Stop Loss (pips)", "SL distance", "Risk")
        self._tp_pips = self.Param("TakeProfitPips", 50.0).SetDisplay("Take Profit (pips)", "TP distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(rsi_bollinger_fractal_breakout_strategy, self).OnReseted()
        self._long_stop = None
        self._long_tp = None
        self._short_stop = None
        self._short_tp = None

    def OnStarted(self, time):
        super(rsi_bollinger_fractal_breakout_strategy, self).OnStarted(time)
        self._long_stop = None
        self._long_tp = None
        self._short_stop = None
        self._short_tp = None
        self._pip_size = 1.0
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            self._pip_size = float(self.Security.PriceStep)

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(rsi, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return

        close = candle.ClosePrice
        sl_dist = self._sl_pips.Value * self._pip_size
        tp_dist = self._tp_pips.Value * self._pip_size

        # Manage existing position
        if self.Position > 0:
            if self._long_tp is not None and candle.HighPrice >= self._long_tp:
                self.SellMarket()
                self._long_stop = None
                self._long_tp = None
                return
            if self._long_stop is not None and candle.LowPrice <= self._long_stop:
                self.SellMarket()
                self._long_stop = None
                self._long_tp = None
                return
        elif self.Position < 0:
            if self._short_tp is not None and candle.LowPrice <= self._short_tp:
                self.BuyMarket()
                self._short_stop = None
                self._short_tp = None
                return
            if self._short_stop is not None and candle.HighPrice >= self._short_stop:
                self.BuyMarket()
                self._short_stop = None
                self._short_tp = None
                return

        # Entry signals
        if rsi_val > self._rsi_upper.Value and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._long_stop = close - sl_dist if self._sl_pips.Value > 0 else None
            self._long_tp = close + tp_dist if self._tp_pips.Value > 0 else None
        elif rsi_val < self._rsi_lower.Value and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._short_stop = close + sl_dist if self._sl_pips.Value > 0 else None
            self._short_tp = close - tp_dist if self._tp_pips.Value > 0 else None

    def CreateClone(self):
        return rsi_bollinger_fractal_breakout_strategy()
