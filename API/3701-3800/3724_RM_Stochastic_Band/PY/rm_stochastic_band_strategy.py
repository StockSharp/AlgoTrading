import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class rm_stochastic_band_strategy(Strategy):
    """Multi-timeframe stochastic oscillator strategy with ATR-based stop-loss and take-profit."""

    def __init__(self):
        super(rm_stochastic_band_strategy, self).__init__()

        self._order_volume = self.Param("OrderVolume", 0.1) \
            .SetGreaterThanZero() \
            .SetDisplay("Order Volume", "Volume of each market order", "Trading")
        self._stochastic_length = self.Param("StochasticLength", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic Length", "%K lookback period", "Indicators")
        self._stochastic_smoothing = self.Param("StochasticSmoothing", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic Smoothing", "Smoothing period applied to %K", "Indicators")
        self._stochastic_signal_length = self.Param("StochasticSignalLength", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic Signal", "%D moving average length", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Lookback for ATR volatility filter", "Indicators")
        self._stop_loss_multiplier = self.Param("StopLossMultiplier", 1.5) \
            .SetGreaterThanZero() \
            .SetDisplay("SL Multiplier", "ATR multiplier for stop-loss", "Risk")
        self._take_profit_multiplier = self.Param("TakeProfitMultiplier", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("TP Multiplier", "ATR multiplier for take-profit", "Risk")
        self._oversold_level = self.Param("OversoldLevel", 20.0) \
            .SetDisplay("Oversold Level", "Threshold that defines oversold conditions", "Signals")
        self._overbought_level = self.Param("OverboughtLevel", 80.0) \
            .SetDisplay("Overbought Level", "Threshold that defines overbought conditions", "Signals")
        self._base_candle_type = self.Param("BaseCandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Base Timeframe", "Primary execution timeframe", "General")
        self._mid_candle_type = self.Param("MidCandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Mid Timeframe", "Secondary confirmation timeframe", "General")
        self._high_candle_type = self.Param("HighCandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("High Timeframe", "Higher confirmation timeframe", "General")

        self._stoch_m1 = None
        self._stoch_m5 = None
        self._stoch_m15 = None
        self._atr_value = None
        self._long_stop_price = None
        self._long_take_profit = None
        self._short_stop_price = None
        self._short_take_profit = None

    @property
    def CandleType(self):
        return self._base_candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._base_candle_type.Value = value

    @property
    def OrderVolume(self):
        return self._order_volume.Value

    @property
    def StochasticLength(self):
        return self._stochastic_length.Value

    @property
    def StochasticSignalLength(self):
        return self._stochastic_signal_length.Value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @property
    def StopLossMultiplier(self):
        return self._stop_loss_multiplier.Value

    @property
    def TakeProfitMultiplier(self):
        return self._take_profit_multiplier.Value

    @property
    def OversoldLevel(self):
        return self._oversold_level.Value

    @property
    def OverboughtLevel(self):
        return self._overbought_level.Value

    @property
    def BaseCandleType(self):
        return self._base_candle_type.Value

    @property
    def MidCandleType(self):
        return self._mid_candle_type.Value

    @property
    def HighCandleType(self):
        return self._high_candle_type.Value

    def OnReseted(self):
        super(rm_stochastic_band_strategy, self).OnReseted()
        self._stoch_m1 = None
        self._stoch_m5 = None
        self._stoch_m15 = None
        self._atr_value = None
        self._long_stop_price = None
        self._long_take_profit = None
        self._short_stop_price = None
        self._short_take_profit = None

    def _create_stochastic(self):
        stoch = StochasticOscillator()
        stoch.K.Length = self.StochasticLength
        stoch.D.Length = self.StochasticSignalLength
        return stoch

    def OnStarted2(self, time):
        super(rm_stochastic_band_strategy, self).OnStarted2(time)

        base_stochastic = self._create_stochastic()
        mid_stochastic = self._create_stochastic()
        high_stochastic = self._create_stochastic()
        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod

        base_subscription = self.SubscribeCandles(self.BaseCandleType)
        base_subscription.BindEx(base_stochastic, self._process_base_candle).Start()

        self.SubscribeCandles(self.MidCandleType) \
            .BindEx(mid_stochastic, self._process_mid_candle).Start()

        self.SubscribeCandles(self.HighCandleType) \
            .BindEx(high_stochastic, atr, self._process_high_candle).Start()

    def _process_mid_candle(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        if not stoch_value.IsFinal:
            return

        k = stoch_value.K
        if k is not None:
            self._stoch_m5 = float(k)

    def _process_high_candle(self, candle, stoch_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not stoch_value.IsFinal or not atr_value.IsFinal:
            return

        k = stoch_value.K
        if k is not None:
            self._stoch_m15 = float(k)

        self._atr_value = float(atr_value)

    def _process_base_candle(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        if not stoch_value.IsFinal:
            return

        k = stoch_value.K
        if k is None:
            return

        self._stoch_m1 = float(k)

        self._manage_open_position(candle)
        self._try_enter_position(candle)

    def _manage_open_position(self, candle):
        if self.Position == 0:
            self._long_stop_price = None
            self._long_take_profit = None
            self._short_stop_price = None
            self._short_take_profit = None
            return

        if self.Position > 0:
            if self._long_stop_price is not None and float(candle.LowPrice) <= self._long_stop_price:
                self.SellMarket(self.Position)
                self._long_stop_price = None
                self._long_take_profit = None
                return

            if self._long_take_profit is not None and float(candle.HighPrice) >= self._long_take_profit:
                self.SellMarket(self.Position)
                self._long_stop_price = None
                self._long_take_profit = None

        elif self.Position < 0:
            short_volume = abs(self.Position)
            if self._short_stop_price is not None and float(candle.HighPrice) >= self._short_stop_price:
                self.BuyMarket(short_volume)
                self._short_stop_price = None
                self._short_take_profit = None
                return

            if self._short_take_profit is not None and float(candle.LowPrice) <= self._short_take_profit:
                self.BuyMarket(short_volume)
                self._short_stop_price = None
                self._short_take_profit = None

    def _try_enter_position(self, candle):
        if self.Position != 0:
            return

        if self._stoch_m1 is None or self._stoch_m5 is None or self._stoch_m15 is None or self._atr_value is None:
            return

        stoch_fast = self._stoch_m1
        stoch_mid = self._stoch_m5
        stoch_slow = self._stoch_m15
        atr = self._atr_value

        oversold = float(self.OversoldLevel)
        overbought = float(self.OverboughtLevel)

        if stoch_fast < oversold and stoch_mid < oversold and stoch_slow < oversold:
            self._enter_long(float(candle.ClosePrice), atr)
        elif stoch_fast > overbought and stoch_mid > overbought and stoch_slow > overbought:
            self._enter_short(float(candle.ClosePrice), atr)

    def _enter_long(self, price, atr):
        volume = float(self.OrderVolume)
        if volume <= 0:
            return

        self.BuyMarket(volume)
        self._long_stop_price = price - atr * float(self.StopLossMultiplier)
        self._long_take_profit = price + atr * float(self.TakeProfitMultiplier)
        self._short_stop_price = None
        self._short_take_profit = None

    def _enter_short(self, price, atr):
        volume = float(self.OrderVolume)
        if volume <= 0:
            return

        self.SellMarket(volume)
        self._short_stop_price = price + atr * float(self.StopLossMultiplier)
        self._short_take_profit = price - atr * float(self.TakeProfitMultiplier)
        self._long_stop_price = None
        self._long_take_profit = None

    def CreateClone(self):
        return rm_stochastic_band_strategy()
