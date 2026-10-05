import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class bollinger_band_squeeze_strategy(Strategy):
    """
    Bollinger Band Squeeze strategy.
    A squeeze is a band width below the average width of the last LookbackPeriod candles, the current one included. During a squeeze a close
    above the upper band goes long and a close below the lower band goes short, reversing an opposite position. A long closes once price
    closes back inside the bands and a short likewise. The stop lies AtrMultiplier ATR from the entry close and is checked on candle closes.
    """

    def __init__(self):
        super(bollinger_band_squeeze_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20).SetGreaterThanZero().SetDisplay("Bollinger Period", "Period of the Bollinger Bands", "Indicators")
        self._bollinger_multiplier = self.Param("BollingerMultiplier", 2.0).SetGreaterThanZero().SetDisplay("Bollinger Multiplier", "Standard deviation multiplier of the bands", "Indicators")
        self._lookback_period = self.Param("LookbackPeriod", 20).SetGreaterThanZero().SetDisplay("Lookback Period", "Candles the average band width spans", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Period of the stop ATR", "Risk")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetNotNegative().SetDisplay("ATR Multiplier", "Stop distance from the entry in ATRs", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._widths = []
        self._stop_price = Decimal(0)

    def OnReseted(self):
        super(bollinger_band_squeeze_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(bollinger_band_squeeze_strategy, self).OnStarted2(time)

        self._reset_state()

        bollinger = BollingerBands()
        bollinger.Length = self._bollinger_period.Value
        bollinger.Width = Decimal(self._bollinger_multiplier.Value)
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, atr)

    def _process_candle(self, candle, bollinger_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not bollinger_value.IsFormed:
            return
        if bollinger_value.UpBand is None or bollinger_value.LowBand is None:
            return

        upper = bollinger_value.UpBand
        lower = bollinger_value.LowBand
        width = upper - lower
        period = self._lookback_period.Value

        self._widths.append(width)
        if len(self._widths) > period:
            self._widths.pop(0)

        if len(self._widths) < period or not atr_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        total = Decimal(0)
        for value in self._widths:
            total += value
        squeeze = width < total / Decimal(period)
        atr = atr_value.GetValue[Decimal](None)
        close = candle.ClosePrice

        stop_atr = Decimal(self._atr_multiplier.Value)
        if squeeze and close > upper and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - stop_atr * atr
        elif squeeze and close < lower and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + stop_atr * atr
        elif self.Position > 0 and (close <= upper or (stop_atr > 0 and close <= self._stop_price)):
            self.SellMarket(self.Position)
        elif self.Position < 0 and (close >= lower or (stop_atr > 0 and close >= self._stop_price)):
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return bollinger_band_squeeze_strategy()
