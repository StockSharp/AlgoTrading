import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import RelativeStrengthIndex

class rsi_ma_strategy(Strategy):
    def __init__(self):
        super(rsi_ma_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2))) \
            .SetDisplay("Candle TimeFrame", "", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "", "Oscillator")
        self._oversold_activation = self.Param("OversoldActivationLevel", 40.0) \
            .SetDisplay("Oversold Activation", "", "Oscillator")
        self._oversold_extreme = self.Param("OversoldExtremeLevel", 30.0) \
            .SetDisplay("Oversold Extreme", "", "Oscillator")
        self._overbought_activation = self.Param("OverboughtActivationLevel", 60.0) \
            .SetDisplay("Overbought Activation", "", "Oscillator")
        self._overbought_extreme = self.Param("OverboughtExtremeLevel", 70.0) \
            .SetDisplay("Overbought Extreme", "", "Oscillator")
        self._stop_loss_pips = self.Param("StopLossPips", 399.0) \
            .SetDisplay("Stop Loss (pips)", "", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 999.0) \
            .SetDisplay("Take Profit (pips)", "", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 299.0) \
            .SetDisplay("Trailing Stop (pips)", "", "Risk")
        self._use_stop_loss = self.Param("UseStopLoss", True) \
            .SetDisplay("Use Stop Loss", "", "Risk")
        self._use_take_profit = self.Param("UseTakeProfit", True) \
            .SetDisplay("Use Take Profit", "", "Risk")
        self._use_trailing_stop = self.Param("UseTrailingStop", True) \
            .SetDisplay("Use Trailing Stop", "", "Risk")
        self._prev_rsi = None
        self._entry_price = 0.0
        self._stop_loss_price = None
        self._take_profit_price = None

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def RsiPeriod(self):
        return self._rsi_period.Value
    @property
    def OversoldActivationLevel(self):
        return float(self._oversold_activation.Value)
    @property
    def OversoldExtremeLevel(self):
        return float(self._oversold_extreme.Value)
    @property
    def OverboughtActivationLevel(self):
        return float(self._overbought_activation.Value)
    @property
    def OverboughtExtremeLevel(self):
        return float(self._overbought_extreme.Value)
    @property
    def StopLossPips(self):
        return float(self._stop_loss_pips.Value)
    @property
    def TakeProfitPips(self):
        return float(self._take_profit_pips.Value)
    @property
    def TrailingStopPips(self):
        return float(self._trailing_stop_pips.Value)
    @property
    def UseStopLoss(self):
        return self._use_stop_loss.Value
    @property
    def UseTakeProfit(self):
        return self._use_take_profit.Value
    @property
    def UseTrailingStop(self):
        return self._use_trailing_stop.Value

    def OnStarted(self, time):
        super(rsi_ma_strategy, self).OnStarted(time)
        self._prev_rsi = None
        self._entry_price = 0.0
        self._stop_loss_price = None
        self._take_profit_price = None
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._rsi, self.ProcessCandle).Start()

    def _get_pip_size(self):
        sec = self.Security
        if sec is not None:
            ps = sec.PriceStep
            if ps is not None and float(ps) > 0:
                return float(ps)
        return 0.0001

    def ProcessCandle(self, candle, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        rv = float(rsi_val)
        if self._prev_rsi is None:
            self._prev_rsi = rv
            return
        close = float(candle.ClosePrice)
        low = float(candle.LowPrice)
        high = float(candle.HighPrice)
        prev = self._prev_rsi
        # manage open position
        if self.Position > 0:
            exit_signal = prev > self.OverboughtExtremeLevel and rv < self.OverboughtActivationLevel
            if exit_signal:
                self.SellMarket()
                self._reset_risk()
                self._prev_rsi = rv
                return
            self._update_trailing_long(close)
            if self._should_close_long(low, high):
                self.SellMarket()
                self._reset_risk()
                self._prev_rsi = rv
                return
        elif self.Position < 0:
            exit_signal = prev < self.OversoldExtremeLevel and rv > self.OversoldActivationLevel
            if exit_signal:
                self.BuyMarket()
                self._reset_risk()
                self._prev_rsi = rv
                return
            self._update_trailing_short(close)
            if self._should_close_short(low, high):
                self.BuyMarket()
                self._reset_risk()
                self._prev_rsi = rv
                return
        # evaluate entries
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_rsi = rv
            return
        if close > 0:
            if prev < self.OversoldExtremeLevel and rv > self.OversoldActivationLevel and self.Position <= 0:
                self._entry_price = close
                self._init_risk_long(close)
                self.BuyMarket()
            elif prev > self.OverboughtExtremeLevel and rv < self.OverboughtActivationLevel and self.Position >= 0:
                self._entry_price = close
                self._init_risk_short(close)
                self.SellMarket()
        self._prev_rsi = rv

    def _init_risk_long(self, price):
        pip = self._get_pip_size()
        if self.UseStopLoss and self.StopLossPips > 0:
            self._stop_loss_price = price - pip * self.StopLossPips
        else:
            self._stop_loss_price = None
        if self.UseTakeProfit and self.TakeProfitPips > 0:
            self._take_profit_price = price + pip * self.TakeProfitPips
        else:
            self._take_profit_price = None

    def _init_risk_short(self, price):
        pip = self._get_pip_size()
        if self.UseStopLoss and self.StopLossPips > 0:
            self._stop_loss_price = price + pip * self.StopLossPips
        else:
            self._stop_loss_price = None
        if self.UseTakeProfit and self.TakeProfitPips > 0:
            self._take_profit_price = price - pip * self.TakeProfitPips
        else:
            self._take_profit_price = None

    def _update_trailing_long(self, close):
        if not self.UseTrailingStop or self.TrailingStopPips <= 0:
            return
        pip = self._get_pip_size()
        pip_dist = pip * self.TrailingStopPips
        if pip_dist <= 0:
            return
        profit = close - self._entry_price
        if profit <= pip_dist:
            return
        new_stop = close - pip_dist
        if self._stop_loss_price is None or new_stop > self._stop_loss_price:
            self._stop_loss_price = new_stop

    def _update_trailing_short(self, close):
        if not self.UseTrailingStop or self.TrailingStopPips <= 0:
            return
        pip = self._get_pip_size()
        pip_dist = pip * self.TrailingStopPips
        if pip_dist <= 0:
            return
        profit = self._entry_price - close
        if profit <= pip_dist:
            return
        new_stop = close + pip_dist
        if self._stop_loss_price is None or new_stop < self._stop_loss_price:
            self._stop_loss_price = new_stop

    def _should_close_long(self, low, high):
        stop_hit = self.UseStopLoss and self._stop_loss_price is not None and low <= self._stop_loss_price
        tp_hit = self.UseTakeProfit and self._take_profit_price is not None and high >= self._take_profit_price
        return stop_hit or tp_hit

    def _should_close_short(self, low, high):
        stop_hit = self.UseStopLoss and self._stop_loss_price is not None and high >= self._stop_loss_price
        tp_hit = self.UseTakeProfit and self._take_profit_price is not None and low <= self._take_profit_price
        return stop_hit or tp_hit

    def _reset_risk(self):
        self._stop_loss_price = None
        self._take_profit_price = None
        self._entry_price = 0.0

    def OnReseted(self):
        super(rsi_ma_strategy, self).OnReseted()
        self._prev_rsi = None
        self._entry_price = 0.0
        self._stop_loss_price = None
        self._take_profit_price = None

    def CreateClone(self):
        return rsi_ma_strategy()
