import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, BollingerBands, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class macd_bollinger_strategy(Strategy):
    """
    MACD Bollinger strategy.
    MACD above its signal line with a close below the lower Bollinger band goes long and MACD below the signal line with a close above
    the upper band goes short, reversing an opposite position. A position closes once price returns to the middle band. The stop lies
    AtrMultiplier ATR from the entry close and is checked on candle closes.
    """

    def __init__(self):
        super(macd_bollinger_strategy, self).__init__()
        self._macd_fast = self.Param("MacdFast", 12).SetGreaterThanZero().SetDisplay("MACD Fast", "Fast EMA period of MACD", "MACD")
        self._macd_slow = self.Param("MacdSlow", 26).SetGreaterThanZero().SetDisplay("MACD Slow", "Slow EMA period of MACD", "MACD")
        self._macd_signal = self.Param("MacdSignal", 9).SetGreaterThanZero().SetDisplay("MACD Signal", "Signal line period of MACD", "MACD")
        self._bollinger_period = self.Param("BollingerPeriod", 20).SetGreaterThanZero().SetDisplay("BB Period", "Period of the Bollinger Bands", "Bollinger")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0).SetGreaterThanZero().SetDisplay("BB Deviation", "Standard deviation multiplier of the bands", "Bollinger")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Period of the stop ATR", "Risk")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetNotNegative().SetDisplay("ATR Multiplier", "Stop distance from the entry in ATRs", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._stop_price = Decimal(0)

    def OnReseted(self):
        super(macd_bollinger_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(macd_bollinger_strategy, self).OnStarted2(time)

        self._reset_state()

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast.Value
        macd.Macd.LongMa.Length = self._macd_slow.Value
        macd.SignalMa.Length = self._macd_signal.Value
        bollinger = BollingerBands()
        bollinger.Length = self._bollinger_period.Value
        bollinger.Width = Decimal(self._bollinger_deviation.Value)
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, bollinger, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, macd)

    def _process_candle(self, candle, macd_value, bollinger_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not macd_value.IsFormed or not bollinger_value.IsFormed or not atr_value.IsFormed:
            return
        if macd_value.Macd is None or macd_value.Signal is None:
            return
        if bollinger_value.UpBand is None or bollinger_value.LowBand is None or bollinger_value.MovingAverage is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        macd = macd_value.Macd
        signal = macd_value.Signal
        upper = bollinger_value.UpBand
        lower = bollinger_value.LowBand
        middle = bollinger_value.MovingAverage
        atr = atr_value.GetValue[Decimal](None)
        close = candle.ClosePrice

        stop_atr = Decimal(self._atr_multiplier.Value)
        if macd > signal and close < lower and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - stop_atr * atr
        elif macd < signal and close > upper and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + stop_atr * atr
        elif self.Position > 0 and (close >= middle or (stop_atr > 0 and close <= self._stop_price)):
            self.SellMarket(self.Position)
        elif self.Position < 0 and (close <= middle or (stop_atr > 0 and close >= self._stop_price)):
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return macd_bollinger_strategy()
