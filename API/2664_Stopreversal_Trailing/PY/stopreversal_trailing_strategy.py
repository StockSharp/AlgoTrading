import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import AverageTrueRange


class stopreversal_trailing_strategy(Strategy):
    """Stopreversal indicator based trailing stop strategy with ATR and signal delay."""

    def __init__(self):
        super(stopreversal_trailing_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Stopreversal timeframe", "General")
        self._atr_period = self.Param("AtrPeriod", 15) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "ATR lookback for trailing stop", "Indicator")
        self._stop_loss_steps = self.Param("StopLossSteps", 10) \
            .SetDisplay("Stop Loss Steps", "Stop loss distance in price steps", "Risk")
        self._take_profit_steps = self.Param("TakeProfitSteps", 20) \
            .SetDisplay("Take Profit Steps", "Take profit distance in price steps", "Risk")
        self._buy_open = self.Param("BuyPositionOpen", True) \
            .SetDisplay("Open Long", "Allow opening long positions", "Trading")
        self._sell_open = self.Param("SellPositionOpen", True) \
            .SetDisplay("Open Short", "Allow opening short positions", "Trading")
        self._buy_close = self.Param("BuyPositionClose", True) \
            .SetDisplay("Close Long", "Close long positions on sell signals", "Trading")
        self._sell_close = self.Param("SellPositionClose", True) \
            .SetDisplay("Close Short", "Close short positions on buy signals", "Trading")
        self._npips = self.Param("Npips", 0.004) \
            .SetGreaterThanZero() \
            .SetDisplay("Trailing Offset", "Fractional offset applied to the stop line", "Indicator")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Signal Bar", "Bar delay before acting on a signal", "Indicator")

        self._prev_stop = None
        self._prev_price = None
        self._signals = []
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def AtrPeriod(self):
        return int(self._atr_period.Value)
    @property
    def StopLossSteps(self):
        return int(self._stop_loss_steps.Value)
    @property
    def TakeProfitSteps(self):
        return int(self._take_profit_steps.Value)
    @property
    def BuyPositionOpen(self):
        return self._buy_open.Value
    @property
    def SellPositionOpen(self):
        return self._sell_open.Value
    @property
    def BuyPositionClose(self):
        return self._buy_close.Value
    @property
    def SellPositionClose(self):
        return self._sell_close.Value
    @property
    def Npips(self):
        return float(self._npips.Value)
    @property
    def SignalBar(self):
        return int(self._signal_bar.Value)

    def _get_step(self):
        sec = self.Security
        if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0:
            return float(sec.PriceStep)
        return 0.0001

    def OnStarted(self, time):
        super(stopreversal_trailing_strategy, self).OnStarted(time)

        self._prev_stop = None
        self._prev_price = None
        self._signals = []
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None

        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._atr, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._atr)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        self._update_stops(candle)

        price = float(candle.ClosePrice)
        npips = self.Npips
        prev_stop = self._prev_stop if self._prev_stop is not None else price * (1.0 - npips)
        prev_price = self._prev_price if self._prev_price is not None else price
        has_prev = self._prev_stop is not None and self._prev_price is not None

        stop = self._calc_stop(price, prev_price, prev_stop)

        buy_signal = has_prev and price > stop and prev_price < prev_stop and prev_stop != 0
        sell_signal = has_prev and price < stop and prev_price > prev_stop and prev_stop != 0

        self._prev_price = price
        self._prev_stop = stop

        self._signals.append((buy_signal, sell_signal, float(candle.ClosePrice)))
        mx = max(self.SignalBar + 5, 10)
        while len(self._signals) > mx:
            self._signals.pop(0)

        if len(self._signals) <= self.SignalBar:
            return

        idx = len(self._signals) - 1 - self.SignalBar
        if idx < 0:
            return

        sig = self._signals[idx]
        allow_trading = self._atr.IsFormed
        self._execute_signal(sig, allow_trading)

    def _execute_signal(self, sig, allow_trading):
        buy_sig, sell_sig, close_price = sig

        if self.SellPositionClose and buy_sig and self.Position < 0:
            self.BuyMarket()
            self._short_stop = None
            self._short_take = None

        if self.BuyPositionClose and sell_sig and self.Position > 0:
            self.SellMarket()
            self._long_stop = None
            self._long_take = None

        if not allow_trading or self.Position != 0:
            return

        step = self._get_step()

        if self.BuyPositionOpen and buy_sig:
            self.BuyMarket()
            self._short_stop = None
            self._short_take = None
            self._long_stop = close_price - step * self.StopLossSteps if self.StopLossSteps > 0 else None
            self._long_take = close_price + step * self.TakeProfitSteps if self.TakeProfitSteps > 0 else None
        elif self.SellPositionOpen and sell_sig:
            self.SellMarket()
            self._long_stop = None
            self._long_take = None
            self._short_stop = close_price + step * self.StopLossSteps if self.StopLossSteps > 0 else None
            self._short_take = close_price - step * self.TakeProfitSteps if self.TakeProfitSteps > 0 else None

    def _update_stops(self, candle):
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)

        if self.Position > 0:
            if self._long_stop is not None and lo <= self._long_stop:
                self.SellMarket()
                self._long_stop = None
                self._long_take = None
                return
            if self._long_take is not None and h >= self._long_take:
                self.SellMarket()
                self._long_stop = None
                self._long_take = None
        elif self.Position < 0:
            if self._short_stop is not None and h >= self._short_stop:
                self.BuyMarket()
                self._short_stop = None
                self._short_take = None
                return
            if self._short_take is not None and lo <= self._short_take:
                self.BuyMarket()
                self._short_stop = None
                self._short_take = None

    def _calc_stop(self, price, prev_price, prev_stop):
        offset = self.Npips
        if price == prev_stop:
            return prev_stop
        if prev_price < prev_stop and price < prev_stop:
            return min(prev_stop, price * (1.0 + offset))
        if prev_price > prev_stop and price > prev_stop:
            return max(prev_stop, price * (1.0 - offset))
        if price > prev_stop:
            return price * (1.0 - offset)
        return price * (1.0 + offset)

    def OnReseted(self):
        super(stopreversal_trailing_strategy, self).OnReseted()
        self._prev_stop = None
        self._prev_price = None
        self._signals = []
        self._long_stop = None
        self._long_take = None
        self._short_stop = None
        self._short_take = None

    def CreateClone(self):
        return stopreversal_trailing_strategy()
