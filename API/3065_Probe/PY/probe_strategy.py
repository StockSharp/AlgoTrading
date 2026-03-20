import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class probe_strategy(Strategy):
    def __init__(self):
        super(probe_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Primary timeframe used for indicator calculations", "General")
        self._cci_length = self.Param("CciLength", 60) \
            .SetDisplay("CCI Length", "Averaging period of the Commodity Channel Index", "Indicators")
        self._cci_channel_level = self.Param("CciChannelLevel", 120.0) \
            .SetDisplay("CCI Channel", "Absolute CCI level used as the channel boundary", "Indicators")
        self._stop_loss_pips = self.Param("StopLossPips", 50.0) \
            .SetDisplay("Stop Loss (pips)", "Protective stop loss distance expressed in pips", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 5.0) \
            .SetDisplay("Trailing Stop (pips)", "Minimum profit required before trailing activates", "Risk")
        self._trailing_step_pips = self.Param("TrailingStepPips", 5.0) \
            .SetDisplay("Trailing Step (pips)", "Additional profit required before the stop is moved again", "Risk")

        self._previous_cci = None
        self._entry_price = None
        self._stop_price = None
        self._pip_size = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def CciLength(self):
        return self._cci_length.Value
    @property
    def CciChannelLevel(self):
        return self._cci_channel_level.Value
    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value
    @property
    def TrailingStopPips(self):
        return self._trailing_stop_pips.Value
    @property
    def TrailingStepPips(self):
        return self._trailing_step_pips.Value

    def OnReseted(self):
        super(probe_strategy, self).OnReseted()
        self._previous_cci = None
        self._entry_price = None
        self._stop_price = None
        self._pip_size = 0.0

    def OnStarted(self, time):
        super(probe_strategy, self).OnStarted(time)
        self._pip_size = self._calculate_pip_size()
        self._previous_cci = None
        self._entry_price = None
        self._stop_price = None

        cci = CommodityChannelIndex()
        cci.Length = self.CciLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(cci, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, cci)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, cci_value):
        if candle.State != CandleStates.Finished:
            return

        cv = float(cci_value)

        if self._pip_size <= 0.0:
            self._pip_size = self._calculate_pip_size()

        exited = self._manage_position(candle)
        if exited:
            self._previous_cci = cv
            return

        if self._previous_cci is not None and self.Position == 0:
            channel = float(self.CciChannelLevel)
            lower = -channel

            cross_up = self._previous_cci < lower and cv > lower
            cross_down = self._previous_cci > channel and cv < channel

            if cross_up or cross_down:
                if not self.IsFormedAndOnlineAndAllowTrading():
                    self._previous_cci = cv
                    return

                if cross_up:
                    self.BuyMarket()
                    self._entry_price = float(candle.ClosePrice)
                    stop_dist = float(self.StopLossPips) * self._pip_size
                    self._stop_price = self._entry_price - stop_dist if stop_dist > 0.0 else None
                elif cross_down:
                    self.SellMarket()
                    self._entry_price = float(candle.ClosePrice)
                    stop_dist = float(self.StopLossPips) * self._pip_size
                    self._stop_price = self._entry_price + stop_dist if stop_dist > 0.0 else None

        self._previous_cci = cv

    def _manage_position(self, candle):
        if self.Position == 0:
            self._entry_price = None
            self._stop_price = None
            return False

        trailing_stop = float(self.TrailingStopPips) * self._pip_size
        trailing_step = float(self.TrailingStepPips) * self._pip_size

        if self.Position > 0:
            if self._entry_price is None:
                self._entry_price = float(candle.ClosePrice)

            if self._stop_price is not None and float(candle.LowPrice) <= self._stop_price:
                self.SellMarket()
                self._reset_trade_state()
                return True

            if float(self.TrailingStopPips) > 0.0 and trailing_stop > 0.0 and self._entry_price is not None:
                profit = float(candle.ClosePrice) - self._entry_price
                threshold = trailing_stop + trailing_step
                if profit > threshold:
                    desired_stop = float(candle.ClosePrice) - trailing_stop
                    if self._stop_price is None or desired_stop > self._stop_price:
                        self._stop_price = desired_stop

        elif self.Position < 0:
            if self._entry_price is None:
                self._entry_price = float(candle.ClosePrice)

            if self._stop_price is not None and float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket()
                self._reset_trade_state()
                return True

            if float(self.TrailingStopPips) > 0.0 and trailing_stop > 0.0 and self._entry_price is not None:
                profit = self._entry_price - float(candle.ClosePrice)
                threshold = trailing_stop + trailing_step
                if profit > threshold:
                    desired_stop = float(candle.ClosePrice) + trailing_stop
                    if self._stop_price is None or desired_stop < self._stop_price:
                        self._stop_price = desired_stop

        return False

    def _reset_trade_state(self):
        self._entry_price = None
        self._stop_price = None

    def _calculate_pip_size(self):
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 0.0
        if step <= 0.0:
            return 0.01
        return step

    def CreateClone(self):
        return probe_strategy()
