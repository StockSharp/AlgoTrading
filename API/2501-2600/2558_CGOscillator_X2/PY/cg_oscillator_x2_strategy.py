import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import CenterOfGravityOscillator
from StockSharp.Algo.Strategies import Strategy


class cg_oscillator_x2_strategy(Strategy):
    def __init__(self):
        super(cg_oscillator_x2_strategy, self).__init__()

        self._trend_candle_type = self.Param("TrendCandleType", DataType.TimeFrame(TimeSpan.FromHours(4)))
        self._signal_candle_type = self.Param("SignalCandleType", DataType.TimeFrame(TimeSpan.FromHours(2)))
        self._trend_length = self.Param("TrendLength", 10)
        self._signal_length = self.Param("SignalLength", 10)
        self._buy_open = self.Param("BuyOpen", True)
        self._sell_open = self.Param("SellOpen", True)
        self._buy_close = self.Param("BuyClose", True)
        self._sell_close = self.Param("SellClose", True)
        self._buy_close_signal = self.Param("BuyCloseSignal", False)
        self._sell_close_signal = self.Param("SellCloseSignal", False)
        self._stop_loss = self.Param("StopLoss", 0.0)
        self._take_profit = self.Param("TakeProfit", 0.0)
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 6)

        self._trend_direction = 0
        self._trend_prev_cg = None
        self._signal_prev_cg = None
        self._signal_prev_prev_cg = None
        self._entry_price = None
        self._stop_price = None
        self._take_price = None
        self._cooldown_remaining = 0

    @property
    def TrendCandleType(self):
        return self._trend_candle_type.Value

    @TrendCandleType.setter
    def TrendCandleType(self, value):
        self._trend_candle_type.Value = value

    @property
    def SignalCandleType(self):
        return self._signal_candle_type.Value

    @SignalCandleType.setter
    def SignalCandleType(self, value):
        self._signal_candle_type.Value = value

    @property
    def TrendLength(self):
        return self._trend_length.Value

    @TrendLength.setter
    def TrendLength(self, value):
        self._trend_length.Value = value

    @property
    def SignalLength(self):
        return self._signal_length.Value

    @SignalLength.setter
    def SignalLength(self, value):
        self._signal_length.Value = value

    @property
    def BuyOpen(self):
        return self._buy_open.Value

    @BuyOpen.setter
    def BuyOpen(self, value):
        self._buy_open.Value = value

    @property
    def SellOpen(self):
        return self._sell_open.Value

    @SellOpen.setter
    def SellOpen(self, value):
        self._sell_open.Value = value

    @property
    def BuyClose(self):
        return self._buy_close.Value

    @BuyClose.setter
    def BuyClose(self, value):
        self._buy_close.Value = value

    @property
    def SellClose(self):
        return self._sell_close.Value

    @SellClose.setter
    def SellClose(self, value):
        self._sell_close.Value = value

    @property
    def BuyCloseSignal(self):
        return self._buy_close_signal.Value

    @BuyCloseSignal.setter
    def BuyCloseSignal(self, value):
        self._buy_close_signal.Value = value

    @property
    def SellCloseSignal(self):
        return self._sell_close_signal.Value

    @SellCloseSignal.setter
    def SellCloseSignal(self, value):
        self._sell_close_signal.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def SignalCooldownBars(self):
        return self._signal_cooldown_bars.Value

    @SignalCooldownBars.setter
    def SignalCooldownBars(self, value):
        self._signal_cooldown_bars.Value = value

    def OnStarted2(self, time):
        super(cg_oscillator_x2_strategy, self).OnStarted2(time)

        self._trend_indicator = CenterOfGravityOscillator()
        self._trend_indicator.Length = self.TrendLength

        self._signal_indicator = CenterOfGravityOscillator()
        self._signal_indicator.Length = self.SignalLength

        self.SubscribeCandles(self.TrendCandleType).BindEx(self._trend_indicator, self._process_trend).Start()
        self.SubscribeCandles(self.SignalCandleType).BindEx(self._signal_indicator, self._process_signal).Start()

    def _process_trend(self, candle, value):
        if candle.State != CandleStates.Finished:
            return

        if not self._trend_indicator.IsFormed:
            return

        cg_value = float(value)
        self._trend_prev_cg = cg_value

        if cg_value > 0:
            self._trend_direction = 1
        elif cg_value < 0:
            self._trend_direction = -1
        else:
            self._trend_direction = 0

    def _process_signal(self, candle, value):
        if candle.State != CandleStates.Finished:
            return

        if not self._signal_indicator.IsFormed:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        cg_value = float(value)

        prev_cg = self._signal_prev_cg
        prev_prev_cg = self._signal_prev_prev_cg

        self._signal_prev_prev_cg = self._signal_prev_cg
        self._signal_prev_cg = cg_value

        if prev_cg is None:
            return

        if self._try_close_by_risk(candle):
            return

        close_buy = self.BuyCloseSignal and prev_cg < 0
        close_sell = self.SellCloseSignal and prev_cg > 0
        open_buy = False
        open_sell = False

        bullish_hook = prev_prev_cg is not None and prev_prev_cg >= prev_cg and cg_value > prev_cg
        bearish_hook = prev_prev_cg is not None and prev_prev_cg <= prev_cg and cg_value < prev_cg

        if self._trend_direction < 0:
            if self.BuyClose:
                close_buy = True
            if self._cooldown_remaining == 0 and self.SellOpen and bearish_hook:
                open_sell = True
        elif self._trend_direction > 0:
            if self.SellClose:
                close_sell = True
            if self._cooldown_remaining == 0 and self.BuyOpen and bullish_hook:
                open_buy = True

        if close_buy and self.Position > 0:
            self.SellMarket()
            self._reset_risk_targets()

        if close_sell and self.Position < 0:
            self.BuyMarket()
            self._reset_risk_targets()

        close = float(candle.ClosePrice)

        if open_buy and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._set_risk_targets(close, True)
            self._cooldown_remaining = int(self.SignalCooldownBars)
        elif open_sell and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._set_risk_targets(close, False)
            self._cooldown_remaining = int(self.SignalCooldownBars)

    def _try_close_by_risk(self, candle):
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self.Position > 0:
            if self._stop_price is not None and low <= self._stop_price:
                self.SellMarket()
                self._reset_risk_targets()
                return True
            if self._take_price is not None and high >= self._take_price:
                self.SellMarket()
                self._reset_risk_targets()
                return True
        elif self.Position < 0:
            if self._stop_price is not None and high >= self._stop_price:
                self.BuyMarket()
                self._reset_risk_targets()
                return True
            if self._take_price is not None and low <= self._take_price:
                self.BuyMarket()
                self._reset_risk_targets()
                return True

        return False

    def _set_risk_targets(self, entry_price, is_long):
        self._entry_price = entry_price
        sl = float(self.StopLoss)
        tp = float(self.TakeProfit)

        if sl > 0.0:
            self._stop_price = entry_price - sl if is_long else entry_price + sl
        else:
            self._stop_price = None

        if tp > 0.0:
            self._take_price = entry_price + tp if is_long else entry_price - tp
        else:
            self._take_price = None

    def _reset_risk_targets(self):
        self._entry_price = None
        self._stop_price = None
        self._take_price = None

    def OnReseted(self):
        super(cg_oscillator_x2_strategy, self).OnReseted()
        self._trend_direction = 0
        self._trend_prev_cg = None
        self._signal_prev_cg = None
        self._signal_prev_prev_cg = None
        self._entry_price = None
        self._stop_price = None
        self._take_price = None
        self._cooldown_remaining = 0

    def CreateClone(self):
        return cg_oscillator_x2_strategy()
