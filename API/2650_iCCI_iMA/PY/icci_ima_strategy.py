import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    CommodityChannelIndex, ExponentialMovingAverage, DecimalIndicatorValue
)


class icci_ima_strategy(Strategy):
    """CCI and EMA crossover strategy: trades when CCI crosses its smoothed EMA."""

    def __init__(self):
        super(icci_ima_strategy, self).__init__()

        self._cci_period = self.Param("CciPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("CCI Period", "Length of the main CCI indicator", "Indicators")
        self._cci_close_period = self.Param("CciClosePeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("CCI Close Period", "Length of the CCI used for overbought/oversold exits", "Indicators")
        self._ma_period = self.Param("MaPeriod", 15) \
            .SetGreaterThanZero() \
            .SetDisplay("CCI EMA Period", "Length of the EMA applied to the CCI values", "Indicators")
        self._stop_loss_pips = self.Param("StopLossPips", 50.0) \
            .SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 40.0) \
            .SetDisplay("Take Profit (pips)", "Profit target distance in pips", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Data series used for calculations", "General")

        self._pip_size = 1.0
        self._entry_price = None
        self._prev_cci = None
        self._prev2_cci = None
        self._prev_cci_close = None
        self._prev2_cci_close = None
        self._prev_ma = None
        self._prev2_ma = None
        self._history_count = 0

    @property
    def CciPeriod(self):
        return self._cci_period.Value
    @property
    def CciClosePeriod(self):
        return self._cci_close_period.Value
    @property
    def MaPeriod(self):
        return self._ma_period.Value
    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value
    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value
    @property
    def CandleType(self):
        return self._candle_type.Value

    def _calc_pip_size(self):
        sec = self.Security
        if sec is None or sec.PriceStep is None or float(sec.PriceStep) <= 0:
            return 1.0
        step = float(sec.PriceStep)
        decimals = sec.Decimals if sec.Decimals is not None else 2
        if decimals == 3 or decimals == 5:
            return step * 10.0
        return step

    def OnStarted(self, time):
        super(icci_ima_strategy, self).OnStarted(time)

        self._reset_state()
        self._pip_size = self._calc_pip_size()

        self._cci = CommodityChannelIndex()
        self._cci.Length = self.CciPeriod
        self._cci_close = CommodityChannelIndex()
        self._cci_close.Length = self.CciClosePeriod
        self._cci_ma = ExponentialMovingAverage()
        self._cci_ma.Length = self.MaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._cci, self._cci_close, self.process_candle).Start()

    def process_candle(self, candle, cci_value, cci_close_value):
        if candle.State != CandleStates.Finished:
            return

        cci_val = float(cci_value)
        cci_close_val = float(cci_close_value)

        ma_result = self._cci_ma.Process(
            DecimalIndicatorValue(self._cci_ma, cci_val, candle.OpenTime)
        )
        ma_val = float(ma_result.GetValue[float]())

        if not self._cci.IsFormed or not self._cci_close.IsFormed or not self._cci_ma.IsFormed:
            self._update_history(cci_val, cci_close_val, ma_val)
            return

        if self._history_count < 2:
            self._update_history(cci_val, cci_close_val, ma_val)
            return

        # Check SL/TP
        self._handle_stops(candle)

        cci_two_ago = self._prev2_cci if self._prev2_cci is not None else 0.0
        ma_two_ago = self._prev2_ma if self._prev2_ma is not None else 0.0
        cci_close_two_ago = self._prev2_cci_close if self._prev2_cci_close is not None else 0.0

        should_close_long = ((cci_close_two_ago > 100.0 and cci_close_val <= 100.0) or
                             (cci_val < ma_val and cci_two_ago >= ma_two_ago))
        should_close_short = ((cci_close_two_ago < -100.0 and cci_close_val >= -100.0) or
                              (cci_val > ma_val and cci_two_ago <= ma_two_ago))

        if self.Position > 0 and should_close_long:
            self.SellMarket()
            self._entry_price = None
        elif self.Position < 0 and should_close_short:
            self.BuyMarket()
            self._entry_price = None

        close = float(candle.ClosePrice)

        # Entry signals: CCI crosses its MA
        if cci_val > ma_val and cci_two_ago < ma_two_ago and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = close
        elif cci_val < ma_val and cci_two_ago > ma_two_ago and self.Position >= 0:
            self.SellMarket()
            self._entry_price = close

        if self.Position == 0:
            self._entry_price = None

        self._update_history(cci_val, cci_close_val, ma_val)

    def _handle_stops(self, candle):
        if self._entry_price is None:
            return

        pip = self._pip_size if self._pip_size > 0 else 1.0
        sl_dist = float(self.StopLossPips) * pip if float(self.StopLossPips) > 0 else 0.0
        tp_dist = float(self.TakeProfitPips) * pip if float(self.TakeProfitPips) > 0 else 0.0

        if self.Position > 0:
            entry = self._entry_price
            if sl_dist > 0 and float(candle.LowPrice) <= entry - sl_dist:
                self.SellMarket()
                self._entry_price = None
                return
            if tp_dist > 0 and float(candle.HighPrice) >= entry + tp_dist:
                self.SellMarket()
                self._entry_price = None
        elif self.Position < 0:
            entry = self._entry_price
            if sl_dist > 0 and float(candle.HighPrice) >= entry + sl_dist:
                self.BuyMarket()
                self._entry_price = None
                return
            if tp_dist > 0 and float(candle.LowPrice) <= entry - tp_dist:
                self.BuyMarket()
                self._entry_price = None

    def _update_history(self, cci_val, cci_close_val, ma_val):
        self._prev2_cci = self._prev_cci
        self._prev_cci = cci_val
        self._prev2_cci_close = self._prev_cci_close
        self._prev_cci_close = cci_close_val
        self._prev2_ma = self._prev_ma
        self._prev_ma = ma_val
        if self._history_count < 2:
            self._history_count += 1

    def _reset_state(self):
        self._entry_price = None
        self._prev_cci = None
        self._prev2_cci = None
        self._prev_cci_close = None
        self._prev2_cci_close = None
        self._prev_ma = None
        self._prev2_ma = None
        self._history_count = 0

    def OnReseted(self):
        super(icci_ima_strategy, self).OnReseted()
        self._reset_state()

    def CreateClone(self):
        return icci_ima_strategy()
