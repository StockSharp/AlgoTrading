import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy

class keltner_macd_strategy(Strategy):
    """
    Keltner MACD strategy.
    The Keltner Channel is the EmaPeriod EMA plus and minus Multiplier times the AtrPeriod ATR. A close above the upper band with MACD above
    its signal line goes long and a close below the lower band with MACD below the signal line goes short, reversing an opposite position.
    A long closes when MACD crosses below the signal line and a short when it crosses above it. The stop lies AtrMultiplier ATR from the entry
    close and is checked on candle closes.
    """

    def __init__(self):
        super(keltner_macd_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20).SetGreaterThanZero().SetDisplay("EMA Period", "Period of the channel EMA", "Keltner")
        self._multiplier = self.Param("Multiplier", 2.0).SetGreaterThanZero().SetDisplay("Multiplier", "ATR multiplier of the channel width", "Keltner")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Period of the channel and stop ATR", "Keltner")
        self._macd_fast_period = self.Param("MacdFastPeriod", 12).SetGreaterThanZero().SetDisplay("MACD Fast", "Fast EMA period of MACD", "MACD")
        self._macd_slow_period = self.Param("MacdSlowPeriod", 26).SetGreaterThanZero().SetDisplay("MACD Slow", "Slow EMA period of MACD", "MACD")
        self._macd_signal_period = self.Param("MacdSignalPeriod", 9).SetGreaterThanZero().SetDisplay("MACD Signal", "Signal line period of MACD", "MACD")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetNotNegative().SetDisplay("ATR Multiplier", "Stop distance from the entry in ATRs", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._stop_price = Decimal(0)

    def OnReseted(self):
        super(keltner_macd_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(keltner_macd_strategy, self).OnStarted2(time)

        self._reset_state()

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast_period.Value
        macd.Macd.LongMa.Length = self._macd_slow_period.Value
        macd.SignalMa.Length = self._macd_signal_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ema, atr, macd, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, macd)

    def _process_candle(self, candle, ema_value, atr_value, macd_value):
        if candle.State != CandleStates.Finished:
            return

        if not ema_value.IsFormed or not atr_value.IsFormed or not macd_value.IsFormed:
            return
        if macd_value.Macd is None or macd_value.Signal is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        macd = macd_value.Macd
        signal = macd_value.Signal
        middle = ema_value.GetValue[Decimal](None)
        atr = atr_value.GetValue[Decimal](None)
        multiplier = Decimal(self._multiplier.Value)
        upper = middle + multiplier * atr
        lower = middle - multiplier * atr
        close = candle.ClosePrice

        stop_atr = Decimal(self._atr_multiplier.Value)
        if close > upper and macd > signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - stop_atr * atr
        elif close < lower and macd < signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + stop_atr * atr
        elif self.Position > 0 and (macd < signal or (stop_atr > 0 and close <= self._stop_price)):
            self.SellMarket(self.Position)
        elif self.Position < 0 and (macd > signal or (stop_atr > 0 and close >= self._stop_price)):
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return keltner_macd_strategy()
