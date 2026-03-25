import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Highest, Lowest, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class exp_multitrend_signal_kvn_strategy(Strategy):
    def __init__(self):
        super(exp_multitrend_signal_kvn_strategy, self).__init__()
        self._k = self.Param("K", 10.0) \
            .SetDisplay("K", "Percent of swing used for channel width", "Indicator")
        self._k_period = self.Param("KPeriod", 20) \
            .SetDisplay("K Period", "Base period for swing calculation", "Indicator")
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles for calculation", "General")
        self._max_high = None
        self._min_low = None
        self._trend = 0

    @property
    def k(self):
        return self._k.Value

    @property
    def k_period(self):
        return self._k_period.Value

    @property
    def stop_loss_pct(self):
        return self._stop_loss_pct.Value

    @property
    def take_profit_pct(self):
        return self._take_profit_pct.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(exp_multitrend_signal_kvn_strategy, self).OnReseted()
        self._max_high = None
        self._min_low = None
        self._trend = 0

    def OnStarted(self, time):
        super(exp_multitrend_signal_kvn_strategy, self).OnStarted(time)
        self._max_high = Highest()
        self._max_high.Length = self.k_period
        self._min_low = Lowest()
        self._min_low.Length = self.k_period
        self.Indicators.Add(self._max_high)
        self.Indicators.Add(self._min_low)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        self.StartProtection(
            takeProfit=Unit(self.take_profit_pct, UnitTypes.Percent),
            stopLoss=Unit(self.stop_loss_pct, UnitTypes.Percent),
            useMarketOrders=True)
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        cv1 = CandleIndicatorValue(self._max_high, candle)
        max_result = self._max_high.Process(cv1)
        cv2 = CandleIndicatorValue(self._min_low, candle)
        min_result = self._min_low.Process(cv2)

        if not max_result.IsFormed or not min_result.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ss_max = float(max_result)
        ss_min = float(min_result)

        swing = (ss_max - ss_min) * float(self.k) / 100.0
        smin = ss_min + swing
        smax = ss_max - swing

        close = float(candle.ClosePrice)

        if close > smax:
            if self._trend <= 0 and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            self._trend = 1
        elif close < smin:
            if self._trend >= 0 and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
            self._trend = -1

    def CreateClone(self):
        return exp_multitrend_signal_kvn_strategy()
