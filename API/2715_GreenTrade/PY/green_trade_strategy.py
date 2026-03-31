import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    SmoothedMovingAverage,
    RelativeStrengthIndex,
    DecimalIndicatorValue,
)


class green_trade_strategy(Strategy):
    """GreenTrade: smoothed MA slope filter with RSI momentum confirmation."""

    def __init__(self):
        super(green_trade_strategy, self).__init__()

        self._ma_period = self.Param("MaPeriod", 67) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Period", "Length of the smoothed moving average", "Indicators")
        self._shift_bar = self.Param("ShiftBar", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Shift #0", "Index of the most recent evaluated bar", "Signals")
        self._shift_bar1 = self.Param("ShiftBar1", 1) \
            .SetGreaterThanZero() \
            .SetDisplay("Shift #1", "Offset from bar #0 to bar #1", "Signals")
        self._shift_bar2 = self.Param("ShiftBar2", 2) \
            .SetGreaterThanZero() \
            .SetDisplay("Shift #2", "Offset from bar #1 to bar #2", "Signals")
        self._shift_bar3 = self.Param("ShiftBar3", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Shift #3", "Offset from bar #2 to bar #3", "Signals")
        self._rsi_period = self.Param("RsiPeriod", 57) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "Length of the RSI indicator", "Indicators")
        self._rsi_buy_level = self.Param("RsiBuyLevel", 60.0) \
            .SetDisplay("RSI Buy Level", "RSI threshold for bullish entries", "Signals")
        self._rsi_sell_level = self.Param("RsiSellLevel", 36.0) \
            .SetDisplay("RSI Sell Level", "RSI threshold for bearish entries", "Signals")
        self._trade_volume = self.Param("TradeVolume", 0.1) \
            .SetGreaterThanZero() \
            .SetDisplay("Trade Volume", "Volume used for each new order", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", 300.0) \
            .SetDisplay("Stop Loss", "Initial stop-loss distance in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 300.0) \
            .SetDisplay("Take Profit", "Initial take-profit distance in pips", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 12.0) \
            .SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk")
        self._trailing_step_pips = self.Param("TrailingStepPips", 5.0) \
            .SetDisplay("Trailing Step", "Required progress before trailing adjusts", "Risk")
        self._max_positions = self.Param("MaxPositions", 7) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Positions", "Maximum number of volume units allowed", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Primary candle subscription", "Data")

        self._ma_history = []
        self._rsi_history = []
        self._pip_size = 1.0
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None

    @property
    def MaPeriod(self):
        return int(self._ma_period.Value)
    @property
    def ShiftBar(self):
        return int(self._shift_bar.Value)
    @property
    def ShiftBar1(self):
        return int(self._shift_bar1.Value)
    @property
    def ShiftBar2(self):
        return int(self._shift_bar2.Value)
    @property
    def ShiftBar3(self):
        return int(self._shift_bar3.Value)
    @property
    def RsiPeriod(self):
        return int(self._rsi_period.Value)
    @property
    def RsiBuyLevel(self):
        return float(self._rsi_buy_level.Value)
    @property
    def RsiSellLevel(self):
        return float(self._rsi_sell_level.Value)
    @property
    def TradeVolume(self):
        return float(self._trade_volume.Value)
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
    def TrailingStepPips(self):
        return float(self._trailing_step_pips.Value)
    @property
    def MaxPositions(self):
        return int(self._max_positions.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def _calc_pip_size(self):
        sec = self.Security
        if sec is None or sec.PriceStep is None:
            return 1.0
        step = float(sec.PriceStep)
        if step <= 0:
            return 1.0
        if step < 0.01:
            step *= 10.0
        return step

    def OnStarted2(self, time):
        super(green_trade_strategy, self).OnStarted2(time)

        self._smma = SmoothedMovingAverage()
        self._smma.Length = self.MaPeriod
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod

        self._pip_size = self._calc_pip_size()
        self._ma_history = []
        self._rsi_history = []
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._smma)
            self.DrawIndicator(area, self._rsi)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        median = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        close = float(candle.ClosePrice)
        t = candle.OpenTime

        ma_iv = DecimalIndicatorValue(self._smma, Decimal(median), candle.ServerTime)
        ma_iv.IsFinal = True
        ma_result = self._smma.Process(ma_iv)

        rsi_iv = DecimalIndicatorValue(self._rsi, Decimal(close), candle.ServerTime)
        rsi_iv.IsFinal = True
        rsi_result = self._rsi.Process(rsi_iv)

        if not self._smma.IsFormed or not self._rsi.IsFormed:
            self._ma_history.append(0.0)
            self._rsi_history.append(0.0)
            self._trim_history()
            return

        ma_val = float(ma_result.Value)
        rsi_val = float(rsi_result.Value)

        self._ma_history.append(ma_val)
        self._rsi_history.append(rsi_val)
        self._trim_history()

        shift0 = self.ShiftBar
        shift1 = shift0 + self.ShiftBar1
        shift2 = shift1 + self.ShiftBar2
        shift3 = shift2 + self.ShiftBar3

        ma0 = self._get_hist(self._ma_history, shift0)
        ma1 = self._get_hist(self._ma_history, shift1)
        ma2 = self._get_hist(self._ma_history, shift2)
        ma3 = self._get_hist(self._ma_history, shift3)
        rsi_sample = self._get_hist(self._rsi_history, self.ShiftBar)

        if ma0 is None or ma1 is None or ma2 is None or ma3 is None or rsi_sample is None:
            return

        buy_signal = ma0 > ma1 and ma1 > ma2 and ma2 > ma3 and rsi_sample > self.RsiBuyLevel
        sell_signal = ma0 < ma1 and ma1 < ma2 and ma2 < ma3 and rsi_sample < self.RsiSellLevel

        if buy_signal and self._can_increase(True):
            self._open_position(True, candle)
        elif sell_signal and self._can_increase(False):
            self._open_position(False, candle)

        self._update_trailing(candle)
        self._manage_exits(candle)

    def _open_position(self, is_long, candle):
        close = float(candle.ClosePrice)
        cur_pos = self.Position

        if is_long:
            self.BuyMarket()
            if cur_pos > 0:
                total = cur_pos + self.TradeVolume
                self._entry_price = ((cur_pos * self._entry_price) + (self.TradeVolume * close)) / total if total > 0 else close
            else:
                self._entry_price = close
        else:
            self.SellMarket()
            if cur_pos < 0:
                total = abs(cur_pos) + self.TradeVolume
                self._entry_price = ((abs(cur_pos) * self._entry_price) + (self.TradeVolume * close)) / total if total > 0 else close
            else:
                self._entry_price = close

        stop_dist = self.StopLossPips * self._pip_size
        take_dist = self.TakeProfitPips * self._pip_size
        if is_long:
            self._stop_price = self._entry_price - stop_dist if stop_dist > 0 else None
            self._take_price = self._entry_price + take_dist if take_dist > 0 else None
        else:
            self._stop_price = self._entry_price + stop_dist if stop_dist > 0 else None
            self._take_price = self._entry_price - take_dist if take_dist > 0 else None

    def _update_trailing(self, candle):
        if self.TrailingStopPips <= 0:
            return
        trail_dist = self.TrailingStopPips * self._pip_size
        step_dist = self.TrailingStepPips * self._pip_size
        close = float(candle.ClosePrice)

        if self.Position > 0 and self._entry_price > 0:
            profit = close - self._entry_price
            if profit > trail_dist + step_dist:
                threshold = close - (trail_dist + step_dist)
                if self._stop_price is None or self._stop_price < threshold:
                    self._stop_price = close - trail_dist
        elif self.Position < 0 and self._entry_price > 0:
            profit = self._entry_price - close
            if profit > trail_dist + step_dist:
                threshold = close + trail_dist + step_dist
                if self._stop_price is None or self._stop_price > threshold:
                    self._stop_price = close + trail_dist

    def _manage_exits(self, candle):
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        if self.Position > 0:
            if self._take_price is not None and h >= self._take_price:
                self.SellMarket()
                self._reset_state()
                return
            if self._stop_price is not None and lo <= self._stop_price:
                self.SellMarket()
                self._reset_state()
                return
        elif self.Position < 0:
            if self._take_price is not None and lo <= self._take_price:
                self.BuyMarket()
                self._reset_state()
                return
            if self._stop_price is not None and h >= self._stop_price:
                self.BuyMarket()
                self._reset_state()
                return
        else:
            self._reset_state()

    def _can_increase(self, is_long):
        if self.TradeVolume <= 0:
            return False
        if self.MaxPositions <= 0:
            return True
        max_vol = self.MaxPositions * self.TradeVolume
        abs_pos = abs(self.Position)
        if is_long and self.Position < 0:
            return True
        if not is_long and self.Position > 0:
            return True
        return abs_pos + self.TradeVolume <= max_vol + 0.0000001

    def _get_hist(self, values, shift):
        if shift <= 0:
            return None
        index = len(values) - shift
        if index < 0:
            return None
        return values[index]

    def _trim_history(self):
        max_shift = self.ShiftBar + self.ShiftBar1 + self.ShiftBar2 + self.ShiftBar3
        max_count = max(max_shift + 5, 10)
        while len(self._ma_history) > max_count:
            self._ma_history.pop(0)
        while len(self._rsi_history) > max_count:
            self._rsi_history.pop(0)

    def _reset_state(self):
        if abs(self.Position) < 0.0000001:
            self._entry_price = 0.0
            self._stop_price = None
            self._take_price = None

    def OnReseted(self):
        super(green_trade_strategy, self).OnReseted()
        self._ma_history = []
        self._rsi_history = []
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None

    def CreateClone(self):
        return green_trade_strategy()
