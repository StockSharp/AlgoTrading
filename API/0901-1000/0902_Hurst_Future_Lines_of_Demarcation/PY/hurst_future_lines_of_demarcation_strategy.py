import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class hurst_future_lines_of_demarcation_strategy(Strategy):
    def __init__(self):
        super(hurst_future_lines_of_demarcation_strategy, self).__init__()

        self._smooth_fld = self.Param("SmoothFld", False)
        self._fld_smoothing = self.Param("FldSmoothing", 5).SetGreaterThanZero()
        self._signal_cycle = self.Param("SignalCycleLength", 5).SetGreaterThanZero()
        self._trade_cycle = self.Param("TradeCycleLength", 20).SetGreaterThanZero()
        self._trend_cycle = self.Param("TrendCycleLength", 80).SetGreaterThanZero()
        self._close_trigger1 = self.Param("CloseTrigger1", "Price")
        self._close_trigger2 = self.Param("CloseTrigger2", "Trade")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1)))

        self._closes = []
        self._prev_price = None
        self._prev_signal = None
        self._prev_trade = None
        self._prev_trend = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(hurst_future_lines_of_demarcation_strategy, self).OnReseted()
        self._closes = []
        self._prev_price = None
        self._prev_signal = None
        self._prev_trade = None
        self._prev_trend = None

    def OnStarted2(self, time):
        super(hurst_future_lines_of_demarcation_strategy, self).OnStarted2(time)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        self._closes.append(price)

        signal = self._get_fld(int(self._signal_cycle.Value))
        trade = self._get_fld(int(self._trade_cycle.Value))
        trend = self._get_fld(int(self._trend_cycle.Value))

        if signal is None or trade is None or trend is None:
            self._trim()
            return

        state = self._trend_state(price, trade, trend)

        if self._prev_price is not None:
            trigger1 = self._resolve(str(self._close_trigger1.Value), price, signal, trade, trend)
            trigger2 = self._resolve(str(self._close_trigger2.Value), price, signal, trade, trend)
            prev_trigger1 = self._resolve(str(self._close_trigger1.Value), self._prev_price, self._prev_signal, self._prev_trade, self._prev_trend)
            prev_trigger2 = self._resolve(str(self._close_trigger2.Value), self._prev_price, self._prev_signal, self._prev_trade, self._prev_trend)

            if self.Position > 0 and prev_trigger1 >= prev_trigger2 and trigger1 < trigger2:
                self.SellMarket(abs(self.Position))
            elif self.Position < 0 and prev_trigger1 <= prev_trigger2 and trigger1 > trigger2:
                self.BuyMarket(abs(self.Position))
            else:
                cross_up = self._prev_price <= self._prev_signal and price > signal
                cross_down = self._prev_price >= self._prev_signal and price < signal

                if self.Position == 0 and cross_up and state == 1:
                    self.BuyMarket()
                elif self.Position == 0 and cross_down and state == 6:
                    self.SellMarket()

        self._prev_price = price
        self._prev_signal = signal
        self._prev_trade = trade
        self._prev_trend = trend
        self._trim()

    def _trend_state(self, price, trade, trend):
        if price > trade and trade > trend:
            return 1
        if price < trade and trade < trend:
            return 6
        return 0

    def _get_fld(self, cycle):
        displacement = max(1, cycle // 2)
        target = len(self._closes) - 1 - displacement
        if target < 0:
            return None

        if not bool(self._smooth_fld.Value):
            return self._closes[target]

        smoothing = int(self._fld_smoothing.Value)
        start = max(0, target - smoothing + 1)
        values = self._closes[start:target + 1]
        return sum(values) / len(values)

    @staticmethod
    def _resolve(trigger, price, signal, trade, trend):
        key = trigger.lower()
        if key == "signal":
            return signal
        if key == "trade":
            return trade
        if key == "trend":
            return trend
        return price

    def _trim(self):
        keep = max(int(self._trend_cycle.Value), int(self._trade_cycle.Value), int(self._signal_cycle.Value)) + int(self._fld_smoothing.Value) + 4
        if len(self._closes) > keep:
            del self._closes[0:len(self._closes) - keep]

    def CreateClone(self):
        return hurst_future_lines_of_demarcation_strategy()
