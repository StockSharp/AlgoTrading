import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class silver_trend_signal_re_open_strategy(Strategy):
    def __init__(self):
        super(silver_trend_signal_re_open_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))
        self._ssp = self.Param("Ssp", 9)
        self._risk = self.Param("Risk", 3)
        self._price_step_param = self.Param("PriceStep", 1000.0)
        self._pos_total = self.Param("PosTotal", 1)
        self._stop_loss = self.Param("StopLoss", 1000.0)
        self._take_profit = self.Param("TakeProfit", 2000.0)

        self._entry_price = 0.0
        self._last_reopen_price = 0.0
        self._positions_count = 0
        self._uptrend = False
        self._prev_uptrend = False
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def Ssp(self):
        return self._ssp.Value

    @Ssp.setter
    def Ssp(self, value):
        self._ssp.Value = value

    @property
    def Risk(self):
        return self._risk.Value

    @Risk.setter
    def Risk(self, value):
        self._risk.Value = value

    @property
    def PriceStepParam(self):
        return self._price_step_param.Value

    @PriceStepParam.setter
    def PriceStepParam(self, value):
        self._price_step_param.Value = value

    @property
    def PosTotal(self):
        return self._pos_total.Value

    @PosTotal.setter
    def PosTotal(self, value):
        self._pos_total.Value = value

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

    def OnStarted2(self, time):
        super(silver_trend_signal_re_open_strategy, self).OnStarted2(time)

        self._has_prev = False
        self._uptrend = False
        self._prev_uptrend = False
        self._positions_count = 0
        self._entry_price = 0.0
        self._last_reopen_price = 0.0

        rsi = RelativeStrengthIndex()
        rsi.Length = self.Ssp

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        rsi_val = float(rsi_value)

        if rsi_val < 40.0:
            self._uptrend = False
        if rsi_val > 60.0:
            self._uptrend = True

        buy_signal = not self._prev_uptrend and self._uptrend
        sell_signal = self._prev_uptrend and not self._uptrend

        sl = float(self.StopLoss)
        tp = float(self.TakeProfit)
        ps = float(self.PriceStepParam)
        max_pos = int(self.PosTotal)

        if self._has_prev:
            if self.Position > 0:
                stop_price = self._entry_price - sl
                take_price = self._entry_price + tp

                if sell_signal or close <= stop_price or close >= take_price:
                    self.SellMarket()
                    self._positions_count = 0
                    self._entry_price = 0.0
                    self._last_reopen_price = 0.0
                elif ps > 0.0 and close - self._last_reopen_price >= ps and self._positions_count < max_pos:
                    self.BuyMarket()
                    self._last_reopen_price = close
                    self._positions_count += 1
            elif self.Position < 0:
                stop_price = self._entry_price + sl
                take_price = self._entry_price - tp

                if buy_signal or close >= stop_price or close <= take_price:
                    self.BuyMarket()
                    self._positions_count = 0
                    self._entry_price = 0.0
                    self._last_reopen_price = 0.0
                elif ps > 0.0 and self._last_reopen_price - close >= ps and self._positions_count < max_pos:
                    self.SellMarket()
                    self._last_reopen_price = close
                    self._positions_count += 1

            if self.Position == 0:
                if buy_signal:
                    self.BuyMarket()
                    self._entry_price = close
                    self._last_reopen_price = close
                    self._positions_count = 1
                elif sell_signal:
                    self.SellMarket()
                    self._entry_price = close
                    self._last_reopen_price = close
                    self._positions_count = 1

        self._prev_uptrend = self._uptrend
        self._has_prev = True

    def OnReseted(self):
        super(silver_trend_signal_re_open_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._last_reopen_price = 0.0
        self._positions_count = 0
        self._uptrend = False
        self._prev_uptrend = False
        self._has_prev = False

    def CreateClone(self):
        return silver_trend_signal_re_open_strategy()
