import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage as EMA
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class multi_timeframe_trend_200_ema_filter_longs_only_strategy(Strategy):
    def __init__(self):
        super(multi_timeframe_trend_200_ema_filter_longs_only_strategy, self).__init__()

        self._fast_length = self.Param("FastLength", 9) \
            .SetDisplay("Fast EMA Length", "Fast EMA period", "Parameters")
        self._slow_length = self.Param("SlowLength", 21) \
            .SetDisplay("Slow EMA Length", "Slow EMA period", "Parameters")
        self._ema200_length = self.Param("Ema200Length", 200) \
            .SetDisplay("200 EMA Length", "200 EMA filter length", "Parameters")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss percent", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2))) \
            .SetDisplay("Candle Type", "Base timeframe", "General")

        self._trend5 = 0
        self._trend15 = 0
        self._trend30 = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(multi_timeframe_trend_200_ema_filter_longs_only_strategy, self).OnReseted()
        self._trend5 = 0
        self._trend15 = 0
        self._trend30 = 0

    def OnStarted(self, time):
        super(multi_timeframe_trend_200_ema_filter_longs_only_strategy, self).OnStarted(time)

        fast_len = self._fast_length.Value
        slow_len = self._slow_length.Value

        self._fast5 = EMA()
        self._fast5.Length = fast_len
        self._slow5 = EMA()
        self._slow5.Length = slow_len
        self._ema200 = EMA()
        self._ema200.Length = self._ema200_length.Value

        self._fast15 = EMA()
        self._fast15.Length = fast_len
        self._slow15 = EMA()
        self._slow15.Length = slow_len

        self._fast30 = EMA()
        self._fast30.Length = fast_len
        self._slow30 = EMA()
        self._slow30.Length = slow_len

        self.StartProtection(
            Unit(self._take_profit_percent.Value, UnitTypes.Percent),
            Unit(self._stop_loss_percent.Value, UnitTypes.Percent)
        )

        sub5 = self.SubscribeCandles(self.candle_type)
        sub5.Bind(self._fast5, self._slow5, self._ema200, self._process_candle5).Start()

        sub15 = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        sub15.Bind(self._fast15, self._slow15, self._process_candle15).Start()

        sub30 = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        sub30.Bind(self._fast30, self._slow30, self._process_candle30).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub5)
            self.DrawIndicator(area, self._fast5)
            self.DrawIndicator(area, self._slow5)
            self.DrawIndicator(area, self._ema200)
            self.DrawOwnTrades(area)

    def _process_candle5(self, candle, fast, slow, ema200):
        if candle.State != CandleStates.Finished:
            return

        if not self._fast5.IsFormed or not self._slow5.IsFormed or not self._ema200.IsFormed:
            return
        if self._trend15 == 0 or self._trend30 == 0:
            return

        f = float(fast)
        s = float(slow)
        self._trend5 = 1 if f > s else -1

        combined = self._trend5 + self._trend15 + self._trend30
        price = float(candle.ClosePrice)
        ema200_val = float(ema200)

        enter_long = combined == 3 and price > ema200_val
        exit_long = combined < 3 or price < ema200_val

        if enter_long and self.Position <= 0:
            self.BuyMarket()
        elif exit_long and self.Position > 0:
            self.SellMarket(self.Position)

    def _process_candle15(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        if not self._fast15.IsFormed or not self._slow15.IsFormed:
            return
        self._trend15 = 1 if float(fast) > float(slow) else -1

    def _process_candle30(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        if not self._fast30.IsFormed or not self._slow30.IsFormed:
            return
        self._trend30 = 1 if float(fast) > float(slow) else -1

    def CreateClone(self):
        return multi_timeframe_trend_200_ema_filter_longs_only_strategy()
