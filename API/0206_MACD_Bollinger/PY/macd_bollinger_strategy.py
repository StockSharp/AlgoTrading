import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, BollingerBands, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class macd_bollinger_strategy(Strategy):
    """
    Strategy based on MACD and Bollinger Bands indicators.
    """

    def __init__(self):
        super(macd_bollinger_strategy, self).__init__()
        self._macd_fast = self.Param("MacdFast", 12).SetDisplay("MACD Fast", "MACD fast EMA period", "MACD")
        self._macd_slow = self.Param("MacdSlow", 26).SetDisplay("MACD Slow", "MACD slow EMA period", "MACD")
        self._macd_signal = self.Param("MacdSignal", 9).SetDisplay("MACD Signal", "MACD signal line period", "MACD")
        self._bollinger_period = self.Param("BollingerPeriod", 20).SetDisplay("Bollinger Period", "Bollinger Bands period", "Bollinger")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0).SetDisplay("Bollinger Deviation", "Bollinger Bands stddev multiplier", "Bollinger")
        self._cooldown_bars = self.Param("CooldownBars", 100).SetDisplay("Cooldown Bars", "Bars between entries", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_bollinger_strategy, self).OnReseted()
        self._cooldown = 0

    def OnStarted(self, time):
        super(macd_bollinger_strategy, self).OnStarted(time)
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.LongMa.Length = self._macd_slow.Value
        macd.Macd.ShortMa.Length = self._macd_fast.Value
        macd.SignalMa.Length = self._macd_signal.Value
        bollinger = BollingerBands()
        bollinger.Length = self._bollinger_period.Value
        bollinger.Width = self._bollinger_deviation.Value
        atr = AverageTrueRange()
        atr.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, macd, atr, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, bollinger_value, macd_value, atr_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if bollinger_value.MovingAverage is None:
            return
        middle_band = float(bollinger_value.MovingAverage)
        macd_typed = macd_value
        macd_line = macd_typed.Macd
        signal_line = macd_typed.Signal
        if macd_line is None or signal_line is None:
            return
        price = float(candle.ClosePrice)
        macd_cross_over = float(macd_line) > float(signal_line)
        if self._cooldown > 0:
            self._cooldown -= 1
        if self._cooldown == 0 and macd_cross_over and price < middle_band * 0.999 and self.Position <= 0:
            self.BuyMarket()
            self._cooldown = self._cooldown_bars.Value
        elif self._cooldown == 0 and not macd_cross_over and price > middle_band * 1.001 and self.Position >= 0:
            self.SellMarket()
            self._cooldown = self._cooldown_bars.Value
        elif self.Position > 0 and not macd_cross_over:
            self.SellMarket()
            self._cooldown = self._cooldown_bars.Value
        elif self.Position < 0 and macd_cross_over:
            self.BuyMarket()
            self._cooldown = self._cooldown_bars.Value

    def CreateClone(self):
        return macd_bollinger_strategy()
