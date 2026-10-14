import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class vwap_mean_reversion_strategy(Strategy):
    """
    VWAP Mean Reversion strategy.
    The market trades around the clock, so the session VWAP restarts with each UTC day and weighs each candle's typical price by its volume.
    A close more than K times the AtrPeriod ATR below VWAP goes long and one that far above it goes short, reversing an opposite position.
    A long closes once the close is back at or above VWAP and a short once it is back at or below it. The stop lies K ATR from the entry
    close and is checked on candle closes.
    """

    def __init__(self):
        super(vwap_mean_reversion_strategy, self).__init__()
        self._k = self.Param("K", 2.0).SetGreaterThanZero().SetDisplay("K", "ATR multiplier for the entry distance and the stop", "Parameters")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Period of the ATR", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._day = None
        self._cumulative_price_volume = Decimal(0)
        self._cumulative_volume = Decimal(0)
        self._stop_price = Decimal(0)

    def OnReseted(self):
        super(vwap_mean_reversion_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(vwap_mean_reversion_strategy, self).OnStarted2(time)

        self._reset_state()

        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, atr)

    def _process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        day = candle.OpenTime.Date
        if self._day is None or self._day != day:
            self._day = day
            self._cumulative_price_volume = Decimal(0)
            self._cumulative_volume = Decimal(0)

        typical_price = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / Decimal(3)
        self._cumulative_price_volume += typical_price * candle.TotalVolume
        self._cumulative_volume += candle.TotalVolume

        if not atr_value.IsFormed or self._cumulative_volume <= 0:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        vwap = self._cumulative_price_volume / self._cumulative_volume
        distance = Decimal(self._k.Value) * atr_value.GetValue[Decimal](None)
        close = candle.ClosePrice

        if close < vwap - distance and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - distance
        elif close > vwap + distance and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + distance
        elif self.Position > 0 and (close >= vwap or close <= self._stop_price):
            self.SellMarket(self.Position)
        elif self.Position < 0 and (close <= vwap or close >= self._stop_price):
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return vwap_mean_reversion_strategy()
