import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, WilliamsR, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class bollinger_williams_r_strategy(Strategy):
    """
    Bollinger Williams %R strategy.
    A close below the lower band with Williams %R below WilliamsROversold goes long and a close above the upper band with %R above
    WilliamsROverbought goes short, reversing an opposite position. The position closes when price returns to the middle band.
    The stop lies AtrMultiplier ATR from the entry close and is checked on candle closes.
    """

    def __init__(self):
        super(bollinger_williams_r_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20).SetGreaterThanZero().SetDisplay("Bollinger Period", "Period of the Bollinger Bands", "Indicators")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0).SetGreaterThanZero().SetDisplay("Bollinger Deviation", "Standard deviation multiplier of the bands", "Indicators")
        self._williams_r_period = self.Param("WilliamsRPeriod", 14).SetGreaterThanZero().SetDisplay("Williams %R Period", "Period of Williams %R", "Indicators")
        self._williams_r_oversold = self.Param("WilliamsROversold", -80.0).SetDisplay("Williams %R Oversold", "Williams %R level for longs", "Indicators")
        self._williams_r_overbought = self.Param("WilliamsROverbought", -20.0).SetDisplay("Williams %R Overbought", "Williams %R level for shorts", "Indicators")
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
        super(bollinger_williams_r_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(bollinger_williams_r_strategy, self).OnStarted2(time)

        self._reset_state()

        bollinger = BollingerBands()
        bollinger.Length = self._bollinger_period.Value
        bollinger.Width = Decimal(self._bollinger_deviation.Value)
        williams = WilliamsR()
        williams.Length = self._williams_r_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, williams, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, williams)

    def _process_candle(self, candle, bollinger_value, williams_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not bollinger_value.IsFormed or not williams_value.IsFormed or not atr_value.IsFormed:
            return
        if bollinger_value.UpBand is None or bollinger_value.LowBand is None or bollinger_value.MovingAverage is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        upper = bollinger_value.UpBand
        lower = bollinger_value.LowBand
        middle = bollinger_value.MovingAverage
        atr = atr_value.GetValue[Decimal](None)
        close = candle.ClosePrice
        williams = williams_value.GetValue[Decimal](None)

        stop_atr = Decimal(self._atr_multiplier.Value)
        if close < lower and williams < Decimal(self._williams_r_oversold.Value) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - stop_atr * atr
        elif close > upper and williams > Decimal(self._williams_r_overbought.Value) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + stop_atr * atr
        elif self.Position > 0 and (close >= middle or (stop_atr > 0 and close <= self._stop_price)):
            self.SellMarket(self.Position)
        elif self.Position < 0 and (close <= middle or (stop_atr > 0 and close >= self._stop_price)):
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return bollinger_williams_r_strategy()
