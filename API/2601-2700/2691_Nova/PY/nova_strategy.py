import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class nova_strategy(Strategy):
    """Nova: compares price vs reference N seconds ago, bullish/bearish previous candle filter, martingale on loss."""

    def __init__(self):
        super(nova_strategy, self).__init__()

        self._seconds_ago = self.Param("SecondsAgo", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Seconds window", "Seconds to look back for price comparison", "General")
        self._step_pips = self.Param("StepPips", 1) \
            .SetDisplay("Step (pips)", "Price offset in pips for breakout check", "Signals")
        self._base_volume = self.Param("BaseVolume", 1.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Base volume", "Initial order volume", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", 500) \
            .SetDisplay("Stop-loss (pips)", "Stop-loss distance in pips", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 500) \
            .SetDisplay("Take-profit (pips)", "Take-profit distance in pips", "Risk")
        self._loss_coefficient = self.Param("LossCoefficient", 1.6) \
            .SetGreaterThanZero() \
            .SetDisplay("Loss coefficient", "Multiplier for the next trade after a stop-loss", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle type", "Candles used for signal calculations", "General")

        self._reference_ask = None
        self._reference_bid = None
        self._last_check_time = None
        self._step_offset = 0.0
        self._stop_loss_offset = 0.0
        self._take_profit_offset = 0.0
        self._current_volume = 0.0
        self._last_trade_volume = None
        self._previous_pnl = 0.0
        self._has_previous_candle = False
        self._prev_candle_open = 0.0
        self._prev_candle_close = 0.0

    @property
    def SecondsAgo(self):
        return int(self._seconds_ago.Value)
    @property
    def StepPips(self):
        return int(self._step_pips.Value)
    @property
    def BaseVolume(self):
        return float(self._base_volume.Value)
    @property
    def StopLossPips(self):
        return int(self._stop_loss_pips.Value)
    @property
    def TakeProfitPips(self):
        return int(self._take_profit_pips.Value)
    @property
    def LossCoefficient(self):
        return float(self._loss_coefficient.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def _get_pip_size(self):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None and float(sec.PriceStep) > 0 else 1.0
        decimals = int(sec.Decimals) if sec is not None and sec.Decimals is not None else 0
        factor = 10.0 if (decimals == 3 or decimals == 5) else 1.0
        return step * factor

    def OnStarted2(self, time):
        super(nova_strategy, self).OnStarted2(time)

        self._previous_pnl = float(self.PnL)

        pip_size = self._get_pip_size()
        self._step_offset = self.StepPips * pip_size
        self._stop_loss_offset = self.StopLossPips * pip_size
        self._take_profit_offset = self.TakeProfitPips * pip_size
        self._current_volume = self.BaseVolume

        self._reference_ask = None
        self._reference_bid = None
        self._last_check_time = None
        self._last_trade_volume = None
        self._has_previous_candle = False
        self._prev_candle_open = 0.0
        self._prev_candle_close = 0.0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.process_candle).Start()

        if self.StopLossPips > 0 or self.TakeProfitPips > 0:
            tp = Unit(self._take_profit_offset, UnitTypes.Absolute) if self.TakeProfitPips > 0 else None
            sl = Unit(self._stop_loss_offset, UnitTypes.Absolute) if self.StopLossPips > 0 else None
            if tp is not None and sl is not None:
                self.StartProtection(takeProfit=tp, stopLoss=sl)
            elif tp is not None:
                self.StartProtection(takeProfit=tp)
            elif sl is not None:
                self.StartProtection(stopLoss=sl)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._update_volume_from_pnl()

        if not self._has_previous_candle:
            self._has_previous_candle = True
            self._prev_candle_open = float(candle.OpenPrice)
            self._prev_candle_close = float(candle.ClosePrice)
            return

        if self.Position != 0:
            return

        now = candle.CloseTime
        interval = TimeSpan.FromSeconds(self.SecondsAgo)

        if self._last_check_time is not None and now - self._last_check_time < interval:
            return

        current_ask = float(candle.ClosePrice)
        current_bid = float(candle.ClosePrice)

        if current_ask == 0 or current_bid == 0:
            self._reference_ask = None
            self._reference_bid = None
            self._last_check_time = now
            return

        if self._reference_ask is None or self._reference_bid is None:
            self._reference_ask = current_ask
            self._reference_bid = current_bid
            self._last_check_time = now
            return

        bullish_previous = self._prev_candle_close > self._prev_candle_open
        bearish_previous = self._prev_candle_close < self._prev_candle_open
        ref_ask = self._reference_ask

        if bullish_previous and current_ask - self._step_offset > ref_ask:
            if self._current_volume > 0:
                self.BuyMarket()
                self._last_trade_volume = self._current_volume
        elif bearish_previous and current_bid + self._step_offset < ref_ask:
            if self._current_volume > 0:
                self.SellMarket()
                self._last_trade_volume = self._current_volume

        self._reference_ask = current_ask
        self._reference_bid = current_bid
        self._last_check_time = now
        self._prev_candle_open = float(candle.OpenPrice)
        self._prev_candle_close = float(candle.ClosePrice)

    def _update_volume_from_pnl(self):
        realized_pnl = float(self.PnL)
        if realized_pnl == self._previous_pnl:
            return

        delta = realized_pnl - self._previous_pnl
        self._previous_pnl = realized_pnl

        if delta > 0:
            self._current_volume = self.BaseVolume
        elif delta < 0:
            ref_volume = self._last_trade_volume if self._last_trade_volume is not None else self._current_volume
            self._current_volume = ref_volume * self.LossCoefficient

    def OnReseted(self):
        super(nova_strategy, self).OnReseted()
        self._reference_ask = None
        self._reference_bid = None
        self._last_check_time = None
        self._step_offset = 0.0
        self._stop_loss_offset = 0.0
        self._take_profit_offset = 0.0
        self._current_volume = 0.0
        self._last_trade_volume = None
        self._previous_pnl = 0.0
        self._has_previous_candle = False
        self._prev_candle_open = 0.0
        self._prev_candle_close = 0.0

    def CreateClone(self):
        return nova_strategy()
