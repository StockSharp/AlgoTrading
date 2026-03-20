import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    SimpleMovingAverage,
    RelativeStrengthIndex,
)


class larry_conners_rsi2_strategy(Strategy):
    """Larry Connors RSI-2: mean-reversion with SMA trend filter and optional pip-based SL/TP."""

    def __init__(self):
        super(larry_conners_rsi2_strategy, self).__init__()

        self._trade_volume = self.Param("TradeVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Trade Volume", "Order volume", "Trading")
        self._fast_sma_period = self.Param("FastSmaPeriod", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast SMA Period", "Fast SMA length", "Indicators")
        self._slow_sma_period = self.Param("SlowSmaPeriod", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow SMA Period", "Slow SMA length", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 2) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "RSI lookback", "Indicators")
        self._rsi_long_entry = self.Param("RsiLongEntry", 6.0) \
            .SetDisplay("RSI Long Entry", "RSI threshold for longs", "Signals")
        self._rsi_short_entry = self.Param("RsiShortEntry", 95.0) \
            .SetDisplay("RSI Short Entry", "RSI threshold for shorts", "Signals")
        self._use_stop_loss = self.Param("UseStopLoss", True) \
            .SetDisplay("Use Stop Loss", "Enable stop-loss management", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", 30.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk")
        self._use_take_profit = self.Param("UseTakeProfit", True) \
            .SetDisplay("Use Take Profit", "Enable take-profit management", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 60.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")

        self._pip_size = 0.0
        self._long_entry_price = None
        self._short_entry_price = None

    @property
    def TradeVolume(self):
        return float(self._trade_volume.Value)
    @property
    def FastSmaPeriod(self):
        return int(self._fast_sma_period.Value)
    @property
    def SlowSmaPeriod(self):
        return int(self._slow_sma_period.Value)
    @property
    def RsiPeriod(self):
        return int(self._rsi_period.Value)
    @property
    def RsiLongEntry(self):
        return float(self._rsi_long_entry.Value)
    @property
    def RsiShortEntry(self):
        return float(self._rsi_short_entry.Value)
    @property
    def UseStopLoss(self):
        return self._use_stop_loss.Value
    @property
    def StopLossPips(self):
        return float(self._stop_loss_pips.Value)
    @property
    def UseTakeProfit(self):
        return self._use_take_profit.Value
    @property
    def TakeProfitPips(self):
        return float(self._take_profit_pips.Value)
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
        decimals = 0
        if sec.Decimals is not None:
            decimals = int(sec.Decimals)
        pip_multiplier = 10.0 if decimals in (1, 3, 5) else 1.0
        result = step * pip_multiplier
        if result <= 0:
            result = step
        return result

    def OnStarted(self, time):
        super(larry_conners_rsi2_strategy, self).OnStarted(time)

        self._pip_size = self._calc_pip_size()
        self._long_entry_price = None
        self._short_entry_price = None

        fast_sma = SimpleMovingAverage()
        fast_sma.Length = self.FastSmaPeriod
        slow_sma = SimpleMovingAverage()
        slow_sma.Length = self.SlowSmaPeriod
        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        self._fast_sma_ind = fast_sma
        self._slow_sma_ind = slow_sma
        self._rsi_ind = rsi

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_sma, slow_sma, rsi, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_sma)
            self.DrawIndicator(area, slow_sma)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fast_sma, slow_sma, rsi):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        h = float(candle.HighPrice)
        lo = float(candle.LowPrice)
        fast_sma_val = float(fast_sma)
        slow_sma_val = float(slow_sma)
        rsi_val = float(rsi)

        # Manage open long position exits
        if self.Position > 0:
            if self.UseStopLoss and self._long_entry_price is not None:
                stop_price = self._long_entry_price - self.StopLossPips * self._pip_size
                if lo <= stop_price:
                    self.SellMarket()
                    self._long_entry_price = None
                    return

            if self.UseTakeProfit and self._long_entry_price is not None:
                target_price = self._long_entry_price + self.TakeProfitPips * self._pip_size
                if h >= target_price:
                    self.SellMarket()
                    self._long_entry_price = None
                    return

            if close > fast_sma_val:
                self.SellMarket()
                self._long_entry_price = None
                return

        elif self.Position < 0:
            if self.UseStopLoss and self._short_entry_price is not None:
                stop_price = self._short_entry_price + self.StopLossPips * self._pip_size
                if h >= stop_price:
                    self.BuyMarket()
                    self._short_entry_price = None
                    return

            if self.UseTakeProfit and self._short_entry_price is not None:
                target_price = self._short_entry_price - self.TakeProfitPips * self._pip_size
                if lo <= target_price:
                    self.BuyMarket()
                    self._short_entry_price = None
                    return

            if close < fast_sma_val:
                self.BuyMarket()
                self._short_entry_price = None
                return

        # New entries only when flat
        if self.Position == 0:
            can_go_long = rsi_val < self.RsiLongEntry and close > slow_sma_val
            if can_go_long:
                self.BuyMarket()
                self._long_entry_price = close
                self._short_entry_price = None
                return

            can_go_short = rsi_val > self.RsiShortEntry and close < slow_sma_val
            if can_go_short:
                self.SellMarket()
                self._short_entry_price = close
                self._long_entry_price = None

    def OnReseted(self):
        super(larry_conners_rsi2_strategy, self).OnReseted()
        self._pip_size = 0.0
        self._long_entry_price = None
        self._short_entry_price = None

    def CreateClone(self):
        return larry_conners_rsi2_strategy()
