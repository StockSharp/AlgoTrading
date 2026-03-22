import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergence, SimpleMovingAverage, WilliamsR, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class smart_ass_trade_v2_strategy(Strategy):
    def __init__(self):
        super(smart_ass_trade_v2_strategy, self).__init__()
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk Management")
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle type", "General")
        self._macd = None
        self._ma = None
        self._wpr = None
        self._rsi = None
        self._prev_macd = None
        self._prev_ma = None
        self._prev_wpr = None
        self._prev_rsi = None

    @property
    def take_profit_pct(self):
        return self._take_profit_pct.Value
    @property
    def stop_loss_pct(self):
        return self._stop_loss_pct.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(smart_ass_trade_v2_strategy, self).OnReseted()
        self._prev_macd = None
        self._prev_ma = None
        self._prev_wpr = None
        self._prev_rsi = None

    def OnStarted(self, time):
        super(smart_ass_trade_v2_strategy, self).OnStarted(time)
        self._macd = MovingAverageConvergenceDivergence()
        self._ma = SimpleMovingAverage()
        self._ma.Length = 20
        self._wpr = WilliamsR()
        self._wpr.Length = 26
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        self.StartProtection(
            Unit(float(self.stop_loss_pct), UnitTypes.Percent),
            Unit(float(self.take_profit_pct), UnitTypes.Percent))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        macd_result = self._macd.Process(candle.ClosePrice, candle.OpenTime, True)
        ma_result = self._ma.Process(candle.ClosePrice, candle.OpenTime, True)
        wpr_result = self._wpr.Process(candle)
        rsi_result = self._rsi.Process(candle.ClosePrice, candle.OpenTime, True)
        if not macd_result.IsFormed or not ma_result.IsFormed or not wpr_result.IsFormed or not rsi_result.IsFormed:
            return
        curr_macd = float(macd_result)
        curr_ma = float(ma_result)
        curr_wpr = float(wpr_result)
        curr_rsi = float(rsi_result)
        if self._prev_macd is None or self._prev_ma is None or self._prev_wpr is None or self._prev_rsi is None:
            self._prev_macd = curr_macd
            self._prev_ma = curr_ma
            self._prev_wpr = curr_wpr
            self._prev_rsi = curr_rsi
            return
        macd_rising = curr_macd > self._prev_macd
        macd_falling = curr_macd < self._prev_macd
        ma_rising = curr_ma > self._prev_ma
        ma_falling = curr_ma < self._prev_ma
        buy_signal = (macd_rising and ma_rising and
                      curr_wpr > self._prev_wpr and curr_rsi > self._prev_rsi and
                      curr_rsi < 70.0)
        sell_signal = (macd_falling and ma_falling and
                       curr_wpr < self._prev_wpr and curr_rsi < self._prev_rsi and
                       curr_rsi > 30.0)
        if self.Position <= 0 and buy_signal:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self.Position >= 0 and sell_signal:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        self._prev_macd = curr_macd
        self._prev_ma = curr_ma
        self._prev_wpr = curr_wpr
        self._prev_rsi = curr_rsi

    def CreateClone(self):
        return smart_ass_trade_v2_strategy()
