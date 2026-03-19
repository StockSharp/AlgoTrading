import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class alma_ut_bot_confluence_strategy(Strategy):
    """
    Strategy that combines ALMA filter with UT Bot trailing stop.
    Enters long when UT Bot gives a buy signal above EMA.
    Short entries occur on UT Bot sell signals below EMA.
    Exits are handled by UT Bot trailing stop or ATR-based stop/target.
    """

    def __init__(self):
        super(alma_ut_bot_confluence_strategy, self).__init__()

        self._ema_length = self.Param("EmaLength", 50) \
            .SetDisplay("EMA Length", "Length for long-term EMA", "Main")

        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period", "Main")

        self._stop_loss_atr_mult = self.Param("StopLossAtrMultiplier", 5.0) \
            .SetDisplay("Stop Loss ATR Mult", "ATR multiplier for stop loss", "Risk")

        self._take_profit_atr_mult = self.Param("TakeProfitAtrMultiplier", 4.0) \
            .SetDisplay("Take Profit ATR Mult", "ATR multiplier for take profit", "Risk")

        self._ut_key_value = self.Param("UtKeyValue", 1) \
            .SetDisplay("UT Key", "UT Bot key value", "UT Bot")

        self._ut_atr_period = self.Param("UtAtrPeriod", 10) \
            .SetDisplay("UT ATR Period", "ATR period for UT Bot", "UT Bot")

        self._base_cooldown_bars = self.Param("BaseCooldownBars", 30) \
            .SetDisplay("Base Cooldown", "Cooldown in bars between trades", "Filters")

        self._use_ut_exit = self.Param("UseUtExit", True) \
            .SetDisplay("Use UT Exit", "Use UT Bot trailing stop for exits", "Exit")

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles", "Main")

        self._x_atr_trailing_stop = 0.0
        self._prev_src = 0.0
        self._prev_stop = 0.0
        self._bar_index = 0
        self._last_trade_index = None
        self._entry_price = 0.0

    @property
    def EmaLength(self): return self._ema_length.Value
    @EmaLength.setter
    def EmaLength(self, v): self._ema_length.Value = v
    @property
    def AtrLength(self): return self._atr_length.Value
    @AtrLength.setter
    def AtrLength(self, v): self._atr_length.Value = v
    @property
    def StopLossAtrMultiplier(self): return self._stop_loss_atr_mult.Value
    @StopLossAtrMultiplier.setter
    def StopLossAtrMultiplier(self, v): self._stop_loss_atr_mult.Value = v
    @property
    def TakeProfitAtrMultiplier(self): return self._take_profit_atr_mult.Value
    @TakeProfitAtrMultiplier.setter
    def TakeProfitAtrMultiplier(self, v): self._take_profit_atr_mult.Value = v
    @property
    def UtKeyValue(self): return self._ut_key_value.Value
    @UtKeyValue.setter
    def UtKeyValue(self, v): self._ut_key_value.Value = v
    @property
    def UtAtrPeriod(self): return self._ut_atr_period.Value
    @UtAtrPeriod.setter
    def UtAtrPeriod(self, v): self._ut_atr_period.Value = v
    @property
    def BaseCooldownBars(self): return self._base_cooldown_bars.Value
    @BaseCooldownBars.setter
    def BaseCooldownBars(self, v): self._base_cooldown_bars.Value = v
    @property
    def UseUtExit(self): return self._use_ut_exit.Value
    @UseUtExit.setter
    def UseUtExit(self, v): self._use_ut_exit.Value = v
    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, v): self._candle_type.Value = v

    def OnReseted(self):
        super(alma_ut_bot_confluence_strategy, self).OnReseted()
        self._x_atr_trailing_stop = 0.0
        self._prev_src = 0.0
        self._prev_stop = 0.0
        self._bar_index = 0
        self._last_trade_index = None
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(alma_ut_bot_confluence_strategy, self).OnStarted(time)

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaLength
        atr = AverageTrueRange()
        atr.Length = self.AtrLength
        atr_ut = AverageTrueRange()
        atr_ut.Length = self.UtAtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, atr, atr_ut, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ema_value, atr_value, atr_ut_value):
        if candle.State != CandleStates.Finished:
            return

        src = float(candle.ClosePrice)
        n_loss = self.UtKeyValue * atr_ut_value

        if self._bar_index == 0:
            self._x_atr_trailing_stop = src + n_loss

        if src > self._prev_stop and self._prev_src > self._prev_stop:
            self._x_atr_trailing_stop = max(self._prev_stop, src - n_loss)
        elif src < self._prev_stop and self._prev_src < self._prev_stop:
            self._x_atr_trailing_stop = min(self._prev_stop, src + n_loss)
        else:
            self._x_atr_trailing_stop = src - n_loss if src > self._prev_stop else src + n_loss

        buy_ut = src > self._x_atr_trailing_stop and self._prev_src <= self._prev_stop
        sell_ut = src < self._x_atr_trailing_stop and self._prev_src >= self._prev_stop

        cooldown_ok = self._last_trade_index is None or self._bar_index - self._last_trade_index >= self.BaseCooldownBars

        buy_condition = buy_ut and src > ema_value and cooldown_ok
        sell_condition = sell_ut and src < ema_value and cooldown_ok

        if buy_condition and self.Position <= 0:
            self.BuyMarket()
            self._last_trade_index = self._bar_index
            self._entry_price = src
        elif sell_condition and self.Position >= 0:
            self.SellMarket()
            self._last_trade_index = self._bar_index
            self._entry_price = src
        else:
            self._manage_exit(candle, atr_value, src)

        self._prev_src = src
        self._prev_stop = self._x_atr_trailing_stop
        self._bar_index += 1

    def _manage_exit(self, candle, atr, src):
        if self.UseUtExit:
            if self.Position > 0 and src < self._x_atr_trailing_stop and self._prev_src >= self._prev_stop:
                self.SellMarket()
                self._last_trade_index = self._bar_index
            elif self.Position < 0 and src > self._x_atr_trailing_stop and self._prev_src <= self._prev_stop:
                self.BuyMarket()
                self._last_trade_index = self._bar_index
        elif self.Position != 0:
            stop_loss = atr * self.StopLossAtrMultiplier
            take_profit = atr * self.TakeProfitAtrMultiplier
            close = float(candle.ClosePrice)
            if self.Position > 0:
                if close <= self._entry_price - stop_loss or close >= self._entry_price + take_profit:
                    self.SellMarket()
                    self._last_trade_index = self._bar_index
            else:
                if close >= self._entry_price + stop_loss or close <= self._entry_price - take_profit:
                    self.BuyMarket()
                    self._last_trade_index = self._bar_index

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return alma_ut_bot_confluence_strategy()
