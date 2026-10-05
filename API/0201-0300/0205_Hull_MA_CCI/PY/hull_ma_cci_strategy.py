import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, CommodityChannelIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class hull_ma_cci_strategy(Strategy):
    """
    Hull MA CCI strategy.
    A rising HullPeriod Hull moving average with CCI below CciOversold goes long and a falling one with CCI above CciOverbought goes short,
    reversing an opposite position. A long closes once the Hull MA starts falling and a short once it starts rising. The stop lies
    AtrMultiplier ATR from the entry close and is checked on candle closes.
    """

    def __init__(self):
        super(hull_ma_cci_strategy, self).__init__()
        self._hull_period = self.Param("HullPeriod", 9).SetGreaterThanZero().SetDisplay("Hull Period", "Period of the Hull moving average", "Indicators")
        self._cci_period = self.Param("CciPeriod", 20).SetGreaterThanZero().SetDisplay("CCI Period", "Period of CCI", "Indicators")
        self._cci_oversold = self.Param("CciOversold", -100.0).SetDisplay("CCI Oversold", "CCI level for longs", "Indicators")
        self._cci_overbought = self.Param("CciOverbought", 100.0).SetDisplay("CCI Overbought", "CCI level for shorts", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Period of the stop ATR", "Risk")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetNotNegative().SetDisplay("ATR Multiplier", "Stop distance from the entry in ATRs", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_hull = None
        self._stop_price = Decimal(0)

    def OnReseted(self):
        super(hull_ma_cci_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(hull_ma_cci_strategy, self).OnStarted2(time)

        self._reset_state()

        hull = HullMovingAverage()
        hull.Length = self._hull_period.Value
        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(hull, cci, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hull)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, cci)

    def _process_candle(self, candle, hull_value, cci_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not hull_value.IsFormed:
            return

        hull = hull_value.GetValue[Decimal](None)
        previous = self._prev_hull
        self._prev_hull = hull

        if previous is None or not cci_value.IsFormed or not atr_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        cci = cci_value.GetValue[Decimal](None)
        atr = atr_value.GetValue[Decimal](None)
        rising = hull > previous
        falling = hull < previous
        close = candle.ClosePrice

        stop_atr = Decimal(self._atr_multiplier.Value)
        if rising and cci < Decimal(self._cci_oversold.Value) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - stop_atr * atr
        elif falling and cci > Decimal(self._cci_overbought.Value) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + stop_atr * atr
        elif self.Position > 0 and (falling or (stop_atr > 0 and close <= self._stop_price)):
            self.SellMarket(self.Position)
        elif self.Position < 0 and (rising or (stop_atr > 0 and close >= self._stop_price)):
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return hull_ma_cci_strategy()
