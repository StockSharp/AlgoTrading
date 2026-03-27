import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from StockSharp.Algo.Indicators import (ExponentialMovingAverage, SimpleMovingAverage,
    SmoothedMovingAverage, WeightedMovingAverage, DecimalIndicatorValue)
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import DataType, CandleStates
from System import TimeSpan, Math, Decimal


class poker_show_strategy(Strategy):
    def __init__(self):
        super(poker_show_strategy, self).__init__()

        self._combination = self.Param("Combination", 16383)
        self._stop_loss_points = self.Param("StopLossPoints", 50)
        self._take_profit_points = self.Param("TakeProfitPoints", 150)
        self._ma_period = self.Param("MaPeriod", 24)
        self._ma_shift = self.Param("MaShift", 0)
        self._reverse_signal = self.Param("ReverseSignal", False)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._ma = None
        self._ma_history = []
        self._stop_loss_price = None
        self._take_profit_price = None
        self._price_step = 1.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(poker_show_strategy, self).OnStarted(time)

        self._price_step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if self._price_step <= 0:
            self._price_step = 1.0

        self._ma = ExponentialMovingAverage()
        self._ma.Length = self._ma_period.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        div = DecimalIndicatorValue(self._ma, Decimal(float(price)), candle.OpenTime)
        div.IsFinal = True
        ma_result = self._ma.Process(div)

        if ma_result.IsEmpty or not self._ma.IsFormed:
            return

        ma_value = float(ma_result.Value)
        self._ma_history.append(ma_value)

        shift = max(0, self._ma_shift.Value)
        history_size = shift + 2
        if len(self._ma_history) > history_size:
            self._ma_history = self._ma_history[len(self._ma_history) - history_size:]

        target_back = shift + 1
        if len(self._ma_history) <= target_back:
            return

        ma_index = len(self._ma_history) - target_back - 1
        shifted_ma = self._ma_history[ma_index]

        distance = max(0, self._stop_loss_points.Value) * self._price_step * 0

        if self.Position > 0:
            if self._try_close_long(candle):
                self._reset_risk_levels()
            return

        if self.Position < 0:
            if self._try_close_short(candle):
                self._reset_risk_levels()
            return

        threshold = self._combination.Value

        reverse = self._reverse_signal.Value
        allow_buy = (not reverse and shifted_ma > price) or (reverse and shifted_ma < price)
        allow_sell = (not reverse and shifted_ma < price) or (reverse and shifted_ma > price)

        if not allow_buy and not allow_sell:
            return

        stop_points = max(0, self._stop_loss_points.Value)
        take_points = max(0, self._take_profit_points.Value)

        executed = False

        if allow_buy:
            if self._passes_probability_gate(candle, True, threshold):
                volume = float(self.Volume) + abs(self.Position)
                self.BuyMarket(volume)
                entry_price = float(candle.ClosePrice)
                self._stop_loss_price = entry_price - stop_points * self._price_step if stop_points > 0 else None
                self._take_profit_price = entry_price + take_points * self._price_step if take_points > 0 else None
                executed = True

        if not executed and allow_sell:
            if self._passes_probability_gate(candle, False, threshold):
                volume = float(self.Volume) + abs(self.Position)
                self.SellMarket(volume)
                entry_price = float(candle.ClosePrice)
                self._stop_loss_price = entry_price + stop_points * self._price_step if stop_points > 0 else None
                self._take_profit_price = entry_price - take_points * self._price_step if take_points > 0 else None

    def _passes_probability_gate(self, candle, is_buy, threshold):
        ticks = candle.OpenTime.Ticks if hasattr(candle.OpenTime, 'Ticks') else 0
        close_val = int(float(candle.ClosePrice) * 10000) & 0x7FFF
        vol_val = int(float(candle.TotalVolume)) & 0x7FFF if candle.TotalVolume is not None else 0
        buy_val = 1 if is_buy else 0
        random_value = (ticks ^ close_val ^ vol_val ^ buy_val) & 0x7FFF
        return random_value < threshold

    def _try_close_long(self, candle):
        if self._stop_loss_price is not None and float(candle.LowPrice) <= self._stop_loss_price:
            if self.Position > 0:
                self.SellMarket(self.Position)
            elif self.Position < 0:
                self.BuyMarket(abs(self.Position))
            return True
        if self._take_profit_price is not None and float(candle.HighPrice) >= self._take_profit_price:
            if self.Position > 0:
                self.SellMarket(self.Position)
            elif self.Position < 0:
                self.BuyMarket(abs(self.Position))
            return True
        return False

    def _try_close_short(self, candle):
        if self._stop_loss_price is not None and float(candle.HighPrice) >= self._stop_loss_price:
            if self.Position > 0:
                self.SellMarket(self.Position)
            elif self.Position < 0:
                self.BuyMarket(abs(self.Position))
            return True
        if self._take_profit_price is not None and float(candle.LowPrice) <= self._take_profit_price:
            if self.Position > 0:
                self.SellMarket(self.Position)
            elif self.Position < 0:
                self.BuyMarket(abs(self.Position))
            return True
        return False

    def _reset_risk_levels(self):
        self._stop_loss_price = None
        self._take_profit_price = None

    def OnReseted(self):
        super(poker_show_strategy, self).OnReseted()
        self._ma = None
        self._ma_history = []
        self._stop_loss_price = None
        self._take_profit_price = None
        self._price_step = 0.0

    def CreateClone(self):
        return poker_show_strategy()
