import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class laptrend1_strategy(Strategy):
    """
    Laptrend: EMA trend + RSI momentum with ATR-based stops.
    Simplified from multi-timeframe custom indicator version.
    """

    def __init__(self):
        super(laptrend1_strategy, self).__init__()
        self._ema_length = self.Param("EmaLength", 20).SetDisplay("EMA Length", "Trend EMA", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14).SetDisplay("RSI Length", "RSI period", "Indicators")
        self._atr_length = self.Param("AtrLength", 14).SetDisplay("ATR Length", "ATR period", "Indicators")
        self._sl_mult = self.Param("SlMultiplier", 2.0).SetDisplay("SL Mult", "ATR multiplier for SL", "Risk")
        self._tp_mult = self.Param("TpMultiplier", 3.0).SetDisplay("TP Mult", "ATR multiplier for TP", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

        self._prev_ema = 0.0
        self._entry_price = 0.0
        self._stop = 0.0
        self._tp = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(laptrend1_strategy, self).OnReseted()
        self._prev_ema = 0.0
        self._entry_price = 0.0
        self._stop = 0.0
        self._tp = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(laptrend1_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, rsi, atr, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_val, rsi_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        ema = float(ema_val)
        rsi = float(rsi_val)
        atr = float(atr_val)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        if self._cooldown > 0:
            self._cooldown -= 1
        if atr <= 0:
            self._prev_ema = ema
            return
        if self.Position > 0:
            if low <= self._stop or high >= self._tp:
                self.SellMarket()
                self._entry_price = 0
                self._cooldown = 20
        elif self.Position < 0:
            if high >= self._stop or low <= self._tp:
                self.BuyMarket()
                self._entry_price = 0
                self._cooldown = 20
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_ema = ema
            return
        if self._cooldown > 0:
            self._prev_ema = ema
            return
        if self._prev_ema == 0:
            self._prev_ema = ema
            return
        trend_up = close > ema and self._prev_ema < ema
        trend_down = close < ema and self._prev_ema > ema
        if trend_up and rsi > 50 and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = close
            self._stop = close - atr * self._sl_mult.Value
            self._tp = close + atr * self._tp_mult.Value
            self._cooldown = 20
        elif trend_down and rsi < 50 and self.Position >= 0:
            self.SellMarket()
            self._entry_price = close
            self._stop = close + atr * self._sl_mult.Value
            self._tp = close - atr * self._tp_mult.Value
            self._cooldown = 20
        self._prev_ema = ema

    def CreateClone(self):
        return laptrend1_strategy()
