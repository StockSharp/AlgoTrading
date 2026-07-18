import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, DayOfWeek
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import (
    RelativeStrengthIndex, StochasticOscillator,
    ExponentialMovingAverage, BollingerBands,
)
from StockSharp.Algo.Strategies import Strategy


class hybrid_scalper_strategy(Strategy):

    def __init__(self):
        super(hybrid_scalper_strategy, self).__init__()

        self._rsi_period = self.Param("RsiPeriod", 7) \
            .SetDisplay("RSI Period", "RSI calculation period", "Indicators")
        self._ema_fast_period = self.Param("EmaFastPeriod", 21) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._ema_slow_period = self.Param("EmaSlowPeriod", 89) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._bb_period = self.Param("BbPeriod", 50) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Indicators")
        self._bb_deviation = self.Param("BbDeviation", 4.0) \
            .SetDisplay("BB Deviation", "Bollinger Bands deviation", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 2) \
            .SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Risk")
        self._trade_monday = self.Param("TradeMonday", True) \
            .SetDisplay("Trade Monday", "Allow trading on Monday", "Schedule")
        self._trade_tuesday = self.Param("TradeTuesday", True) \
            .SetDisplay("Trade Tuesday", "Allow trading on Tuesday", "Schedule")
        self._trade_wednesday = self.Param("TradeWednesday", True) \
            .SetDisplay("Trade Wednesday", "Allow trading on Wednesday", "Schedule")
        self._trade_thursday = self.Param("TradeThursday", True) \
            .SetDisplay("Trade Thursday", "Allow trading on Thursday", "Schedule")
        self._trade_friday = self.Param("TradeFriday", True) \
            .SetDisplay("Trade Friday", "Allow trading on Friday", "Schedule")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Candle type for the strategy", "General")

        self._prev_stoch_k = 0.0
        self._prev_stoch_d = 0.0
        self._is_initialized = False
        self._bars_since_trade = 0

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsi_period.Value = value

    @property
    def EmaFastPeriod(self):
        return self._ema_fast_period.Value

    @EmaFastPeriod.setter
    def EmaFastPeriod(self, value):
        self._ema_fast_period.Value = value

    @property
    def EmaSlowPeriod(self):
        return self._ema_slow_period.Value

    @EmaSlowPeriod.setter
    def EmaSlowPeriod(self, value):
        self._ema_slow_period.Value = value

    @property
    def BbPeriod(self):
        return self._bb_period.Value

    @BbPeriod.setter
    def BbPeriod(self, value):
        self._bb_period.Value = value

    @property
    def BbDeviation(self):
        return self._bb_deviation.Value

    @BbDeviation.setter
    def BbDeviation(self, value):
        self._bb_deviation.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    @property
    def TradeMonday(self):
        return self._trade_monday.Value

    @TradeMonday.setter
    def TradeMonday(self, value):
        self._trade_monday.Value = value

    @property
    def TradeTuesday(self):
        return self._trade_tuesday.Value

    @TradeTuesday.setter
    def TradeTuesday(self, value):
        self._trade_tuesday.Value = value

    @property
    def TradeWednesday(self):
        return self._trade_wednesday.Value

    @TradeWednesday.setter
    def TradeWednesday(self, value):
        self._trade_wednesday.Value = value

    @property
    def TradeThursday(self):
        return self._trade_thursday.Value

    @TradeThursday.setter
    def TradeThursday(self, value):
        self._trade_thursday.Value = value

    @property
    def TradeFriday(self):
        return self._trade_friday.Value

    @TradeFriday.setter
    def TradeFriday(self, value):
        self._trade_friday.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _is_trading_day(self, day):
        if day == DayOfWeek.Monday:
            return self.TradeMonday
        elif day == DayOfWeek.Tuesday:
            return self.TradeTuesday
        elif day == DayOfWeek.Wednesday:
            return self.TradeWednesday
        elif day == DayOfWeek.Thursday:
            return self.TradeThursday
        elif day == DayOfWeek.Friday:
            return self.TradeFriday
        return False

    def OnStarted2(self, time):
        super(hybrid_scalper_strategy, self).OnStarted2(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod
        stochastic = StochasticOscillator()
        ema_fast = ExponentialMovingAverage()
        ema_fast.Length = self.EmaFastPeriod
        ema_slow = ExponentialMovingAverage()
        ema_slow.Length = self.EmaSlowPeriod
        bollinger = BollingerBands()
        bollinger.Length = self.BbPeriod
        bollinger.Width = self.BbDeviation

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .BindEx(rsi, stochastic, ema_fast, ema_slow, bollinger, self.ProcessIndicators) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawIndicator(area, stochastic)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)

    def ProcessIndicators(self, candle, rsi_value, stoch_value, ema_fast_value,
                          ema_slow_value, bollinger_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._is_trading_day(candle.OpenTime.DayOfWeek):
            return

        rsi = float(rsi_value)

        stoch_k_raw = stoch_value.K
        stoch_d_raw = stoch_value.D
        if stoch_k_raw is None or stoch_d_raw is None:
            return
        stoch_k = float(stoch_k_raw)
        stoch_d = float(stoch_d_raw)

        ema_fast = float(ema_fast_value)
        ema_slow = float(ema_slow_value)

        upper_raw = bollinger_value.UpBand
        lower_raw = bollinger_value.LowBand
        middle_raw = bollinger_value.MovingAverage
        if upper_raw is None or lower_raw is None or middle_raw is None:
            return
        upper_band = float(upper_raw)
        lower_band = float(lower_raw)
        middle_band = float(middle_raw)
        if middle_band == 0.0:
            return

        if self._bars_since_trade < self.CooldownBars:
            self._bars_since_trade += 1

        relative_width = (upper_band - lower_band) / middle_band

        if not self._is_initialized:
            self._prev_stoch_k = stoch_k
            self._prev_stoch_d = stoch_d
            self._is_initialized = True
            return

        long_signal = (self._prev_stoch_k <= self._prev_stoch_d
                       and stoch_k > stoch_d
                       and stoch_k < 30.0
                       and rsi < 40.0
                       and ema_fast > ema_slow
                       and 0.005 <= relative_width <= 0.12)

        short_signal = (self._prev_stoch_k >= self._prev_stoch_d
                        and stoch_k < stoch_d
                        and stoch_k > 70.0
                        and rsi > 60.0
                        and ema_fast < ema_slow
                        and 0.005 <= relative_width <= 0.12)

        pos = self.Position

        if self._bars_since_trade >= self.CooldownBars:
            if long_signal and pos <= 0:
                self.BuyMarket(self.Volume + abs(pos))
                self._bars_since_trade = 0
            elif short_signal and pos >= 0:
                self.SellMarket(self.Volume + abs(pos))
                self._bars_since_trade = 0

        self._prev_stoch_k = stoch_k
        self._prev_stoch_d = stoch_d

    def OnReseted(self):
        super(hybrid_scalper_strategy, self).OnReseted()
        self._prev_stoch_k = 0.0
        self._prev_stoch_d = 0.0
        self._is_initialized = False
        self._bars_since_trade = self.CooldownBars

    def CreateClone(self):
        return hybrid_scalper_strategy()
