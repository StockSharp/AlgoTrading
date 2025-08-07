import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import Math
from StockSharp.Messages import CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import BollingerBands, KeltnerChannels, Highest, Lowest, SimpleMovingAverage, LinearRegression, RelativeStrengthIndex
from datatype_extensions import *


class ttm_squeeze_strategy(Strategy):
    """TTM Squeeze strategy.

    Detects volatility contraction when Bollinger Bands fall inside Keltner Channels
    and waits for expansion. Momentum measured by a linear regression oscillator and
    a RSI filter confirm entries. Optional take-profit can be enabled via parameter.
    """

    def __init__(self):
        super(ttm_squeeze_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._squeeze_length = self.Param("SqueezeLength", 20) \
            .SetDisplay("Squeeze Length", "TTM Squeeze calculation length", "TTM Squeeze")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI calculation length", "RSI")
        self._use_tp = self.Param("UseTP", False) \
            .SetDisplay("Enable Take Profit", "Use take profit", "Take Profit")
        self._tp_percent = self.Param("TpPercent", 1.2) \
            .SetDisplay("TP Percent", "Take profit percentage", "Take Profit")

        self._bollinger = None
        self._keltner = None
        self._highest = None
        self._lowest = None
        self._close_sma = None
        self._momentum = None
        self._rsi = None

        self._previous_momentum = 0.0
        self._current_momentum = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def squeeze_length(self):
        return self._squeeze_length.Value

    @squeeze_length.setter
    def squeeze_length(self, value):
        self._squeeze_length.Value = value

    @property
    def rsi_length(self):
        return self._rsi_length.Value

    @rsi_length.setter
    def rsi_length(self, value):
        self._rsi_length.Value = value

    @property
    def use_tp(self):
        return self._use_tp.Value

    @use_tp.setter
    def use_tp(self, value):
        self._use_tp.Value = value

    @property
    def tp_percent(self):
        return self._tp_percent.Value

    @tp_percent.setter
    def tp_percent(self, value):
        self._tp_percent.Value = value

    def OnReseted(self):
        super(ttm_squeeze_strategy, self).OnReseted()
        self._previous_momentum = 0.0
        self._current_momentum = 0.0

    def OnStarted(self, time):
        super(ttm_squeeze_strategy, self).OnStarted(time)

        self._bollinger = BollingerBands()
        self._bollinger.Length = self.squeeze_length
        self._bollinger.Width = 2.0

        self._keltner = KeltnerChannels()
        self._keltner.Length = self.squeeze_length
        self._keltner.Multiplier = 1.5

        self._highest = Highest()
        self._highest.Length = self.squeeze_length
        self._lowest = Lowest()
        self._lowest.Length = self.squeeze_length
        self._close_sma = SimpleMovingAverage()
        self._close_sma.Length = self.squeeze_length
        self._momentum = LinearRegression()
        self._momentum.Length = self.squeeze_length
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx([self._rsi, self._highest, self._lowest, self._close_sma, self._momentum], self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

        if self.use_tp:
            from StockSharp.Algo import Unit, UnitTypes
            self.StartProtection(Unit(self.tp_percent / 100.0, UnitTypes.Percent), Unit())

    def ProcessCandle(self, candle, values):
        if candle.State != CandleStates.Finished:
            return

        if not self._momentum.IsFormed or not self._rsi.IsFormed:
            return

        rsi_value = float(values[0])
        highest_value = float(values[1])
        lowest_value = float(values[2])
        close_sma_value = float(values[3])
        lin_reg_val = values[4]
        if (rsi_value is None or highest_value is None or lowest_value is None or close_sma_value is None):
            return

        momentum_value = lin_reg_val.LinearRegSlope if hasattr(lin_reg_val, 'LinearRegSlope') else None
        if momentum_value is None:
            return

        bb_val = self._bollinger.Process(candle)
        kc_val = self._keltner.Process(candle)
        if not self._bollinger.IsFormed or not self._keltner.IsFormed:
            return

        bb_upper = bb_val.UpBand if hasattr(bb_val, 'UpBand') else None
        bb_lower = bb_val.LowBand if hasattr(bb_val, 'LowBand') else None
        kc_upper = kc_val.Upper if hasattr(kc_val, 'Upper') else None
        kc_lower = kc_val.Lower if hasattr(kc_val, 'Lower') else None
        if None in (bb_upper, bb_lower, kc_upper, kc_lower):
            return

        squeeze_on = bb_upper < kc_upper and bb_lower > kc_lower

        self._current_momentum = momentum_value
        self._check_entry(candle, rsi_value, squeeze_on)
        self._previous_momentum = self._current_momentum

    def _check_entry(self, candle, rsi_value, squeeze_on):
        if squeeze_on:
            return
        price = candle.ClosePrice
        if (self._current_momentum < 0 and self._previous_momentum != 0 and
                self._current_momentum > self._previous_momentum and rsi_value > 30 and self.Position == 0):
            self.RegisterOrder(self.CreateOrder(Sides.Buy, price, self.Volume))
        if (self._current_momentum > 0 and self._previous_momentum != 0 and
                self._current_momentum < self._previous_momentum and rsi_value < 70 and self.Position == 0):
            self.RegisterOrder(self.CreateOrder(Sides.Sell, price, self.Volume))

    def CreateClone(self):
        return ttm_squeeze_strategy()
