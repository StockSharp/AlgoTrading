import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal, Math
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
        self._stop_loss_pips = self.Param("StopLossPips", Decimal(50)) \
            .SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", Decimal(40)) \
            .SetDisplay("Take Profit (pips)", "Profit target distance in pips", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Data series used for calculations", "General")

        self._pip_size = Decimal(0)
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

    @CciPeriod.setter
    def CciPeriod(self, value):
        self._cci_period.Value = value

    @property
    def CciClosePeriod(self):
        return self._cci_close_period.Value

    @CciClosePeriod.setter
    def CciClosePeriod(self, value):
        self._cci_close_period.Value = value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @StopLossPips.setter
    def StopLossPips(self, value):
        self._stop_loss_pips.Value = value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @TakeProfitPips.setter
    def TakeProfitPips(self, value):
        self._take_profit_pips.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _calc_pip_size(self):
        sec = self.Security
        if sec is None or sec.PriceStep is None:
            return Decimal(0)
        step = sec.PriceStep
        if step <= Decimal(0):
            return Decimal(0)
        bits = Decimal.GetBits(step)
        scale = (bits[3] >> 16) & 0xFF
        if scale == 3 or scale == 5:
            multiplier = Decimal(10)
        else:
            multiplier = Decimal(1)
        return Decimal.Multiply(step, multiplier)

    def OnStarted2(self, time):
        super(icci_ima_strategy, self).OnStarted2(time)

        self._reset_state()

        self._cci = CommodityChannelIndex()
        self._cci.Length = self.CciPeriod
        self._cci_close = CommodityChannelIndex()
        self._cci_close.Length = self.CciClosePeriod
        self._cci_ma = ExponentialMovingAverage()
        self._cci_ma.Length = self.MaPeriod

        self._pip_size = self._calc_pip_size()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._cci, self._cci_close, self._process_candle).Start()

    def _process_candle(self, candle, cci_value, cci_close_value):
        if candle.State != CandleStates.Finished:
            return

        cci_val = Decimal(float(cci_value))
        cci_close_val = Decimal(float(cci_close_value))

        div = DecimalIndicatorValue(self._cci_ma, cci_val, candle.OpenTime)
        div.IsFinal = True
        ma_result = self._cci_ma.Process(div)
        ma_val = Decimal(float(ma_result))

        if not self._cci.IsFormed or not self._cci_close.IsFormed or not self._cci_ma.IsFormed:
            self._update_history(cci_val, cci_close_val, ma_val)
            return

        if self._history_count < 2:
            self._update_history(cci_val, cci_close_val, ma_val)
            return

        self._handle_stops(candle)

        cci_two_ago = self._prev2_cci if self._prev2_cci is not None else Decimal(0)
        ma_two_ago = self._prev2_ma if self._prev2_ma is not None else Decimal(0)
        cci_close_two_ago = self._prev2_cci_close if self._prev2_cci_close is not None else Decimal(0)

        d100 = Decimal(100)
        dm100 = Decimal(-100)

        should_close_long = ((cci_close_two_ago > d100 and cci_close_val <= d100) or
                             (cci_val < ma_val and cci_two_ago >= ma_two_ago))
        should_close_short = ((cci_close_two_ago < dm100 and cci_close_val >= dm100) or
                              (cci_val > ma_val and cci_two_ago <= ma_two_ago))

        if self.Position > 0 and should_close_long:
            self.SellMarket()
            self._entry_price = None
        elif self.Position < 0 and should_close_short:
            self.BuyMarket()
            self._entry_price = None

        if cci_val > ma_val and cci_two_ago < ma_two_ago and self.Position <= 0:
            self.BuyMarket()
            self._entry_price = candle.ClosePrice
        elif cci_val < ma_val and cci_two_ago > ma_two_ago and self.Position >= 0:
            self.SellMarket()
            self._entry_price = candle.ClosePrice

        if self.Position == 0:
            self._entry_price = None

        self._update_history(cci_val, cci_close_val, ma_val)

    def _handle_stops(self, candle):
        if self._entry_price is None:
            return

        if self._pip_size > Decimal(0):
            price_step = self._pip_size
        else:
            sec = self.Security
            price_step = sec.PriceStep if sec is not None and sec.PriceStep is not None else Decimal(0)
        if price_step <= Decimal(0):
            return

        sl_pips = self.StopLossPips
        tp_pips = self.TakeProfitPips
        sl_dist = Decimal.Multiply(sl_pips, price_step) if sl_pips > Decimal(0) else Decimal(0)
        tp_dist = Decimal.Multiply(tp_pips, price_step) if tp_pips > Decimal(0) else Decimal(0)

        if self.Position > 0:
            entry = self._entry_price
            if sl_dist > Decimal(0) and candle.LowPrice <= Decimal.Subtract(entry, sl_dist):
                self.SellMarket()
                self._entry_price = None
                return
            if tp_dist > Decimal(0) and candle.HighPrice >= Decimal.Add(entry, tp_dist):
                self.SellMarket()
                self._entry_price = None
        elif self.Position < 0:
            entry = self._entry_price
            if sl_dist > Decimal(0) and candle.HighPrice >= Decimal.Add(entry, sl_dist):
                self.BuyMarket()
                self._entry_price = None
                return
            if tp_dist > Decimal(0) and candle.LowPrice <= Decimal.Subtract(entry, tp_dist):
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
        self._pip_size = Decimal(0)
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
