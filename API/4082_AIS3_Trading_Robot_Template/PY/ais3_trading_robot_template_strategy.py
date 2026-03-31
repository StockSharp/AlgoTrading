import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class ais3_trading_robot_template_strategy(Strategy):
    """
    AIS3 Trading Robot: breakout strategy with ATR-based stops and trailing.
    Enters on breakout above/below previous candle range with EMA filter.
    """

    def __init__(self):
        super(ais3_trading_robot_template_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe.", "General")

        self._ema_length = self.Param("EmaLength", 20) \
            .SetDisplay("EMA Length", "EMA period for trend filter.", "Indicators")

        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period.", "Indicators")

        self._take_multiplier = self.Param("TakeMultiplier", 2.0) \
            .SetDisplay("Take Multiplier", "ATR multiplier for TP.", "Risk")

        self._stop_multiplier = self.Param("StopMultiplier", 1.5) \
            .SetDisplay("Stop Multiplier", "ATR multiplier for SL.", "Risk")

        self._prev_high = 0.0
        self._prev_low = 0.0
        self._entry_price = 0.0
        self._stop_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def EmaLength(self):
        return self._ema_length.Value

    @EmaLength.setter
    def EmaLength(self, value):
        self._ema_length.Value = value

    @property
    def AtrLength(self):
        return self._atr_length.Value

    @AtrLength.setter
    def AtrLength(self, value):
        self._atr_length.Value = value

    @property
    def TakeMultiplier(self):
        return self._take_multiplier.Value

    @TakeMultiplier.setter
    def TakeMultiplier(self, value):
        self._take_multiplier.Value = value

    @property
    def StopMultiplier(self):
        return self._stop_multiplier.Value

    @StopMultiplier.setter
    def StopMultiplier(self, value):
        self._stop_multiplier.Value = value

    def OnReseted(self):
        super(ais3_trading_robot_template_strategy, self).OnReseted()
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._entry_price = 0.0
        self._stop_price = 0.0

    def OnStarted2(self, time):
        super(ais3_trading_robot_template_strategy, self).OnStarted2(time)

        self._prev_high = 0.0
        self._prev_low = 0.0
        self._entry_price = 0.0
        self._stop_price = 0.0

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaLength
        atr = AverageTrueRange()
        atr.Length = self.AtrLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, atr, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ema_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        if self._prev_high == 0 or atr_val <= 0:
            self._prev_high = float(candle.HighPrice)
            self._prev_low = float(candle.LowPrice)
            return

        close = float(candle.ClosePrice)
        take_distance = atr_val * self.TakeMultiplier
        stop_distance = atr_val * self.StopMultiplier

        # Manage position
        if self.Position > 0:
            if close - self._entry_price >= take_distance:
                self.SellMarket()
                self._entry_price = 0.0
                self._stop_price = 0.0
            elif self._stop_price > 0 and close <= self._stop_price:
                self.SellMarket()
                self._entry_price = 0.0
                self._stop_price = 0.0
            else:
                trail = close - stop_distance
                if trail > self._stop_price:
                    self._stop_price = trail
        elif self.Position < 0:
            if self._entry_price - close >= take_distance:
                self.BuyMarket()
                self._entry_price = 0.0
                self._stop_price = 0.0
            elif self._stop_price > 0 and close >= self._stop_price:
                self.BuyMarket()
                self._entry_price = 0.0
                self._stop_price = 0.0
            else:
                trail = close + stop_distance
                if trail < self._stop_price or self._stop_price == 0:
                    self._stop_price = trail

        # Entry on breakout + EMA filter
        if self.Position == 0:
            if close > self._prev_high and close > ema_val:
                self._entry_price = close
                self._stop_price = close - stop_distance
                self.BuyMarket()
            elif close < self._prev_low and close < ema_val:
                self._entry_price = close
                self._stop_price = close + stop_distance
                self.SellMarket()

        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return ais3_trading_robot_template_strategy()
