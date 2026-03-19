import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class twenty_pips_price_channel_strategy(Strategy):
    """Donchian channel breakout with SMA filter and StartProtection SL/TP."""
    def __init__(self):
        super(twenty_pips_price_channel_strategy, self).__init__()
        self._channel_period = self.Param("ChannelPeriod", 20).SetGreaterThanZero().SetDisplay("Channel Period", "Donchian channel lookback", "Parameters")
        self._slow_ma_period = self.Param("SlowMaPeriod", 20).SetGreaterThanZero().SetDisplay("Slow MA Period", "Slow MA length", "Parameters")
        self._sl = self.Param("StopLoss", 500).SetNotNegative().SetDisplay("Stop Loss", "SL distance", "Risk")
        self._tp = self.Param("TakeProfit", 500).SetNotNegative().SetDisplay("Take Profit", "TP distance", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5).TimeFrame()).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(twenty_pips_price_channel_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._prev_upper = None
        self._prev_lower = None

    def OnStarted(self, time):
        super(twenty_pips_price_channel_strategy, self).OnStarted(time)
        self._highs = []
        self._lows = []
        self._prev_upper = None
        self._prev_lower = None

        slow_ma = SimpleMovingAverage()
        slow_ma.Length = self._slow_ma_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(slow_ma, self.OnProcess).Start()

        sl = self._sl.Value
        tp = self._tp.Value
        if sl > 0 or tp > 0:
            self.StartProtection(self.CreateProtection(sl if sl > 0 else 0, tp if tp > 0 else 0))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, slow_ma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, slow_ma_val):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        ma_val = float(slow_ma_val)
        period = self._channel_period.Value

        self._highs.append(high)
        self._lows.append(low)

        while len(self._highs) > period:
            self._highs.pop(0)
        while len(self._lows) > period:
            self._lows.pop(0)

        if len(self._highs) < period:
            self._prev_upper = None
            self._prev_lower = None
            return

        ch_upper = max(self._highs)
        ch_lower = min(self._lows)

        if self._prev_upper is not None and self._prev_lower is not None:
            if close > self._prev_upper and close > ma_val and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif close < self._prev_lower and close < ma_val and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev_upper = ch_upper
        self._prev_lower = ch_lower

    def CreateClone(self):
        return twenty_pips_price_channel_strategy()
