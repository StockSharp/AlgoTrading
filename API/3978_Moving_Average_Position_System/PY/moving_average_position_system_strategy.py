import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    SimpleMovingAverage,
    ExponentialMovingAverage,
    SmoothedMovingAverage,
    WeightedMovingAverage,
)

class moving_average_position_system_strategy(Strategy):
    def __init__(self):
        super(moving_average_position_system_strategy, self).__init__()

        self._ma_period = self.Param("MaPeriod", 20) \
            .SetDisplay("MA Period", "Moving average length", "Indicators")
        self._ma_shift = self.Param("MaShift", 0) \
            .SetDisplay("MA Shift", "Forward shift for the moving average", "Indicators")
        self._initial_volume = self.Param("InitialVolume", 0.1) \
            .SetDisplay("Initial Volume", "Starting lot size", "Trading")
        self._start_volume = self.Param("StartVolume", 0.1) \
            .SetDisplay("Start Volume", "Base lot restored after profits", "Trading")
        self._max_volume = self.Param("MaxVolume", 10.0) \
            .SetDisplay("Max Volume", "Maximum allowed lot size", "Trading")
        self._loss_threshold_pips = self.Param("LossThresholdPips", 90.0) \
            .SetDisplay("Loss Threshold (pts)", "Loss in points that doubles the lot", "Risk")
        self._profit_threshold_pips = self.Param("ProfitThresholdPips", 170.0) \
            .SetDisplay("Profit Target (pts)", "Profit in points that resets the lot", "Risk")
        self._take_profit_pips = self.Param("TakeProfitPips", 1000.0) \
            .SetDisplay("Take Profit (pts)", "Fixed take profit distance", "Risk")
        self._use_money_management = self.Param("UseMoneyManagement", True) \
            .SetDisplay("Use Money Management", "Enable martingale volume control", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles used for calculations", "Market Data")

        self._close_history = []
        self._ma_history = []
        self._current_volume = 0.1
        self._cycle_start_realized_pnl = 0.0
        self._price_step = 1.0
        self._entry_price = 0.0

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @property
    def MaShift(self):
        return self._ma_shift.Value

    @property
    def InitialVolume(self):
        return self._initial_volume.Value

    @property
    def StartVolume(self):
        return self._start_volume.Value

    @property
    def MaxVolume(self):
        return self._max_volume.Value

    @property
    def LossThresholdPips(self):
        return self._loss_threshold_pips.Value

    @property
    def ProfitThresholdPips(self):
        return self._profit_threshold_pips.Value

    @property
    def TakeProfitPips(self):
        return self._take_profit_pips.Value

    @property
    def UseMoneyManagement(self):
        return self._use_money_management.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(moving_average_position_system_strategy, self).OnStarted2(time)

        self._current_volume = float(self.InitialVolume)
        self.Volume = self._current_volume
        ps = self.Security.PriceStep if self.Security is not None else None
        self._price_step = float(ps) if ps is not None else 1.0

        ma = WeightedMovingAverage()
        ma.Length = self.MaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ma, self.ProcessCandle).Start()

        tp_pips = float(self.TakeProfitPips)
        tp = Unit(tp_pips, UnitTypes.Absolute) if tp_pips > 0 else None
        self.StartProtection(tp, None)

    def ProcessCandle(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return

        ma_value = float(ma_value)
        can_trade = self.IsFormedAndOnlineAndAllowTrading()

        prev_close = self._close_history[-1] if len(self._close_history) >= 1 else None
        prev_prev_close = self._close_history[-2] if len(self._close_history) >= 2 else None

        shifted_ma = None
        ma_shift = self.MaShift
        if len(self._ma_history) > ma_shift:
            idx = len(self._ma_history) - 1 - ma_shift
            if idx >= 0:
                shifted_ma = self._ma_history[idx]

        if prev_close is not None and prev_prev_close is not None and shifted_ma is not None:
            self._manage_open_position(prev_close, shifted_ma)

            if can_trade:
                self._try_enter(prev_close, prev_prev_close, shifted_ma)

        self._close_history.append(float(candle.ClosePrice))
        self._ma_history.append(ma_value)

    def _manage_open_position(self, prev_close, shifted_ma):
        if self.Position > 0 and prev_close < shifted_ma:
            self.SellMarket(self.Position)
            return
        if self.Position < 0 and prev_close > shifted_ma:
            self.BuyMarket(abs(self.Position))

    def _try_enter(self, prev_close, prev_prev_close, shifted_ma):
        if self._current_volume <= 0:
            return

        crossed_up = prev_close > shifted_ma and prev_prev_close < shifted_ma
        if crossed_up and self.Position <= 0:
            self.BuyMarket(self._current_volume)
            return

        crossed_down = prev_close < shifted_ma and prev_prev_close > shifted_ma
        if crossed_down and self.Position >= 0:
            self.SellMarket(self._current_volume)

    def OnOwnTradeReceived(self, trade):
        super(moving_average_position_system_strategy, self).OnOwnTradeReceived(trade)
        if self.Position != 0 and self._entry_price == 0:
            self._entry_price = float(trade.Trade.Price)
        if self.Position == 0:
            self._entry_price = 0.0

    def OnReseted(self):
        super(moving_average_position_system_strategy, self).OnReseted()
        self._close_history = []
        self._ma_history = []
        self._current_volume = float(self.InitialVolume)
        self._entry_price = 0.0

    def CreateClone(self):
        return moving_average_position_system_strategy()
