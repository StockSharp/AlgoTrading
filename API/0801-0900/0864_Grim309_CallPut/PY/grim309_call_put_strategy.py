import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class grim309_call_put_strategy(Strategy):
    """
    GRIM309 CallPut strategy.
    Goes long when EMA10 is above EMA20, price is above EMA50 and a rising EMA5 is above EMA10; goes short on the mirrored
    conditions. Entries need a flat position and at least CooldownBars candles since the last exit. A position closes when price
    crosses EMA15 against it or when the warning fires: the EMA5-EMA10 spread in the trade's direction shrinks to less than half
    of its previous value.
    """

    def __init__(self):
        super(grim309_call_put_strategy, self).__init__()
        self._ema5_length = self.Param("Ema5Length", 5).SetGreaterThanZero().SetDisplay("EMA5 Length", "Length of the fastest EMA", "Indicators")
        self._ema10_length = self.Param("Ema10Length", 10).SetGreaterThanZero().SetDisplay("EMA10 Length", "Length of the EMA paired with EMA5", "Indicators")
        self._ema15_length = self.Param("Ema15Length", 15).SetGreaterThanZero().SetDisplay("EMA15 Length", "Length of the exit EMA", "Indicators")
        self._ema20_length = self.Param("Ema20Length", 20).SetGreaterThanZero().SetDisplay("EMA20 Length", "Length of the EMA compared with EMA10", "Indicators")
        self._ema50_length = self.Param("Ema50Length", 50).SetGreaterThanZero().SetDisplay("EMA50 Length", "Length of the trend EMA", "Indicators")
        self._ema200_length = self.Param("Ema200Length", 200).SetGreaterThanZero().SetDisplay("EMA200 Length", "Length of the long-term EMA", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 2).SetNotNegative().SetDisplay("Cooldown Bars", "Candles to wait after an exit before a new entry", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_ema5 = None
        self._prev_spread = None
        self._bars_since_exit = None

    def OnReseted(self):
        super(grim309_call_put_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(grim309_call_put_strategy, self).OnStarted2(time)

        self._reset_state()

        def ema(length):
            indicator = ExponentialMovingAverage()
            indicator.Length = length
            return indicator

        ema5 = ema(self._ema5_length.Value)
        ema10 = ema(self._ema10_length.Value)
        ema15 = ema(self._ema15_length.Value)
        ema20 = ema(self._ema20_length.Value)
        ema50 = ema(self._ema50_length.Value)
        ema200 = ema(self._ema200_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema5, ema10, ema15, ema20, ema50, ema200, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            for indicator in (ema5, ema10, ema15, ema20, ema50, ema200):
                self.DrawIndicator(area, indicator)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema5_value, ema10_value, ema15_value, ema20_value, ema50_value, ema200_value):
        if candle.State != CandleStates.Finished:
            return

        if self._bars_since_exit is not None:
            self._bars_since_exit += 1

        ema5 = float(ema5_value)
        ema10 = float(ema10_value)
        ema15 = float(ema15_value)
        ema20 = float(ema20_value)
        ema50 = float(ema50_value)

        prev_ema5 = self._prev_ema5
        prev_spread = self._prev_spread
        spread = ema5 - ema10

        self._prev_ema5 = ema5
        self._prev_spread = spread

        if prev_ema5 is None or prev_spread is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)

        if self.Position > 0:
            warning = prev_spread > 0 and spread < prev_spread / 2.0
            if close < ema15 or warning:
                self.SellMarket(self.Position)
                self._bars_since_exit = 0
            return

        if self.Position < 0:
            warning = prev_spread < 0 and spread > prev_spread / 2.0
            if close > ema15 or warning:
                self.BuyMarket(-self.Position)
                self._bars_since_exit = 0
            return

        if self._bars_since_exit is not None and self._bars_since_exit < self._cooldown_bars.Value:
            return

        if ema10 > ema20 and close > ema50 and ema5 > ema10 and ema5 > prev_ema5:
            self.BuyMarket(self.Volume)
        elif ema10 < ema20 and close < ema50 and ema5 < ema10 and ema5 < prev_ema5:
            self.SellMarket(self.Volume)

    def CreateClone(self):
        return grim309_call_put_strategy()
