import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import DoubleExponentialMovingAverage, BollingerBands, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class nifty_50_5mint_strategy(Strategy):
    """
    Nifty 50 5-minute breakout strategy.
    A long opens when the close breaks above the highest high of the previous LookbackPeriod candles and above the upper Bollinger band
    while DEMA is above the session VWAP; a short uses the lowest low, the lower band and DEMA below VWAP. An opposite signal reverses
    the position, and a fixed stop of StopLossPoints price steps closes it otherwise.
    """

    def __init__(self):
        super(nifty_50_5mint_strategy, self).__init__()
        self._dema_period = self.Param("DemaPeriod", 6).SetGreaterThanZero().SetDisplay("DEMA Period", "DEMA period", "Indicators")
        self._bollinger_length = self.Param("BollingerLength", 20).SetGreaterThanZero().SetDisplay("Bollinger Length", "Bollinger Bands period", "Indicators")
        self._bollinger_std_dev = self.Param("BollingerStdDev", 2.0).SetGreaterThanZero().SetDisplay("Bollinger StdDev", "Bollinger Bands standard deviation multiplier", "Indicators")
        self._lookback_period = self.Param("LookbackPeriod", 5).SetGreaterThanZero().SetDisplay("Lookback Period", "Previous candles whose high or low has to be broken", "Signals")
        self._stop_loss_points = self.Param("StopLossPoints", 25.0).SetNotNegative().SetDisplay("Stop Loss Points", "Stop loss distance in price steps", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._highest = None
        self._lowest = None
        self._prev_highest = None
        self._prev_lowest = None
        self._vwap_date = None
        self._vwap_price_volume = 0.0
        self._vwap_volume = 0.0

    def OnReseted(self):
        super(nifty_50_5mint_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(nifty_50_5mint_strategy, self).OnStarted2(time)

        self._reset_state()

        self._highest = Highest()
        self._highest.Length = self._lookback_period.Value
        self._lowest = Lowest()
        self._lowest.Length = self._lookback_period.Value

        dema = DoubleExponentialMovingAverage()
        dema.Length = self._dema_period.Value
        bollinger = BollingerBands()
        bollinger.Length = self._bollinger_length.Value
        bollinger.Width = Decimal(self._bollinger_std_dev.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(dema, bollinger, self._process_candle).Start()

        step = self.Security.PriceStep if self.Security is not None and self.Security.PriceStep is not None else Decimal(1)
        stop_points = float(self._stop_loss_points.Value)
        stop = Unit(Decimal(stop_points) * step, UnitTypes.Absolute) if stop_points > 0 else Unit()
        self.StartProtection(Unit(), stop, useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, dema)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, dema_value, bollinger_value):
        if candle.State != CandleStates.Finished:
            return

        # Session VWAP restarts every UTC day.
        date = candle.OpenTime.Date
        if self._vwap_date is None or date != self._vwap_date:
            self._vwap_date = date
            self._vwap_price_volume = 0.0
            self._vwap_volume = 0.0

        volume = float(candle.TotalVolume)
        typical = (float(candle.HighPrice) + float(candle.LowPrice) + float(candle.ClosePrice)) / 3.0
        self._vwap_price_volume += typical * volume
        self._vwap_volume += volume

        # The breakout levels are measured on the candles before this one.
        last_high = self._prev_highest
        last_low = self._prev_lowest

        highest_value = process_value(self._highest, candle.HighPrice, candle.ServerTime, True)
        lowest_value = process_value(self._lowest, candle.LowPrice, candle.ServerTime, True)

        if self._highest.IsFormed and self._lowest.IsFormed:
            self._prev_highest = float(to_decimal(highest_value))
            self._prev_lowest = float(to_decimal(lowest_value))

        if not dema_value.IsFormed or not bollinger_value.IsFormed or last_high is None or last_low is None:
            return

        if bollinger_value.UpBand is None or bollinger_value.LowBand is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading() or self._vwap_volume <= 0:
            return

        close = float(candle.ClosePrice)
        upper = float(bollinger_value.UpBand)
        lower = float(bollinger_value.LowBand)
        dema = float(dema_value.GetValue[Decimal](None))
        vwap = self._vwap_price_volume / self._vwap_volume

        if close > last_high and close > upper and dema > vwap and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close < last_low and close < lower and dema < vwap and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return nifty_50_5mint_strategy()
