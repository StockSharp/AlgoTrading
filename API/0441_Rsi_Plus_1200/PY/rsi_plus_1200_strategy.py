import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Sides, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class rsi_plus_1200_strategy(Strategy):
    """RSI + 1200 strategy with EMA trend filter.

    Enters long when RSI crosses above the oversold level while price trades
    slightly above a higher timeframe EMA. Enters short on the opposite
    conditions. The strategy optionally places a percentage based stop loss.
    """

    def __init__(self):
        super(rsi_plus_1200_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI calculation length", "RSI")

        self._rsi_overbought = self.Param("RsiOverbought", 72) \
            .SetDisplay("RSI Overbought", "RSI overbought level", "RSI")

        self._rsi_oversold = self.Param("RsiOversold", 28) \
            .SetDisplay("RSI Oversold", "RSI oversold level", "RSI")

        self._ema_length = self.Param("EmaLength", 150) \
            .SetDisplay("EMA Length", "EMA period for trend filter", "Moving Average")

        self._mtf_timeframe = self.Param("MtfTimeframe", TimeSpan.FromMinutes(120)) \
            .SetDisplay("MTF Timeframe", "Multi-timeframe for EMA", "Moving Average")

        self._show_long = self.Param("ShowLong", True) \
            .SetDisplay("Long Entries", "Enable long entries", "Strategy")

        self._show_short = self.Param("ShowShort", True) \
            .SetDisplay("Short Entries", "Enable short entries", "Strategy")

        self._stop_loss_percent = self.Param("StopLossPercent", 0.10) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Strategy")

        self._rsi = None
        self._ema = None
        self._previous_rsi = 0
        self._previous_close = 0
        self._rsi_crossed_over_oversold = False
        self._rsi_crossed_under_overbought = False

    # region parameters properties
    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def rsi_length(self):
        return self._rsi_length.Value

    @property
    def rsi_overbought(self):
        return self._rsi_overbought.Value

    @property
    def rsi_oversold(self):
        return self._rsi_oversold.Value

    @property
    def ema_length(self):
        return self._ema_length.Value

    @property
    def mtf_timeframe(self):
        return self._mtf_timeframe.Value

    @property
    def show_long(self):
        return self._show_long.Value

    @property
    def show_short(self):
        return self._show_short.Value

    @property
    def stop_loss_percent(self):
        return self._stop_loss_percent.Value
    # endregion

    def OnReseted(self):
        super(rsi_plus_1200_strategy, self).OnReseted()
        self._previous_rsi = 0
        self._previous_close = 0
        self._rsi_crossed_over_oversold = False
        self._rsi_crossed_under_overbought = False

    def OnStarted(self, time):
        super(rsi_plus_1200_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_length

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self.ProcessMainCandle).Start()

        mtf_sub = self.SubscribeCandles(self.mtf_timeframe.TimeFrame())
        mtf_sub.Bind(self._ema, self.ProcessMtfCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema)
            self.DrawOwnTrades(area)

        self.StartProtection(Unit(), Unit(self.stop_loss_percent, UnitTypes.Percent))

    def ProcessMtfCandle(self, candle, ema_value):
        # EMA processed on higher timeframe, no extra logic required
        pass

    def ProcessMainCandle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if self._previous_rsi != 0:
            self._rsi_crossed_over_oversold = self._previous_rsi <= self.rsi_oversold and rsi_value > self.rsi_oversold
            self._rsi_crossed_under_overbought = self._previous_rsi >= self.rsi_overbought and rsi_value < self.rsi_overbought

        self.CheckEntryConditions(candle, rsi_value)
        self.CheckExitConditions(rsi_value)

        self._previous_rsi = rsi_value
        self._previous_close = candle.ClosePrice

    def CheckEntryConditions(self, candle, rsi_value):
        if not self._ema.IsFormed:
            return

        price = candle.ClosePrice
        ema_value = self._ema.GetCurrentValue()

        if (self.show_long and self._rsi_crossed_over_oversold and price > ema_value and price <= ema_value * 1.01 and
                self.Position <= 0):
            self.RegisterOrder(self.CreateOrder(Sides.Buy, price, self.Volume))

        if (self.show_short and self._rsi_crossed_under_overbought and price < ema_value and price >= ema_value * 0.99 and
                self.Position >= 0):
            self.RegisterOrder(self.CreateOrder(Sides.Sell, price, self.Volume))

    def CheckExitConditions(self, rsi_value):
        price = self._previous_close
        ema_value = self._ema.GetCurrentValue() if self._ema.IsFormed else 0

        if self.Position > 0 and rsi_value > self.rsi_overbought:
            self.RegisterOrder(self.CreateOrder(Sides.Sell, price, abs(self.Position)))
        elif self.Position < 0 and rsi_value < self.rsi_oversold:
            self.RegisterOrder(self.CreateOrder(Sides.Buy, price, abs(self.Position)))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return rsi_plus_1200_strategy()
