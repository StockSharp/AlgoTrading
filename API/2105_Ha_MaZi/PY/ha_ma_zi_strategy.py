import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class ha_ma_zi_strategy(Strategy):
    def __init__(self):
        super(ha_ma_zi_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 40) \
            .SetDisplay("MA Period", "EMA period", "General")
        self._zigzag_length = self.Param("ZigzagLength", 13) \
            .SetDisplay("ZigZag Length", "Lookback for pivot search", "ZigZag")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._highest = None
        self._lowest = None
        self._ha_open_prev = 0.0
        self._ha_close_prev = 0.0
        self._last_zigzag = 0.0
        self._last_zigzag_high = 0.0
        self._last_zigzag_low = 0.0

    @property
    def ma_period(self):
        return self._ma_period.Value
    @property
    def zigzag_length(self):
        return self._zigzag_length.Value
    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def stop_loss_pct(self):
        return self._stop_loss_pct.Value
    @property
    def take_profit_pct(self):
        return self._take_profit_pct.Value

    def OnReseted(self):
        super(ha_ma_zi_strategy, self).OnReseted()
        self._highest = None
        self._lowest = None
        self._ha_open_prev = 0.0
        self._ha_close_prev = 0.0
        self._last_zigzag = 0.0
        self._last_zigzag_high = 0.0
        self._last_zigzag_low = 0.0

    def OnStarted(self, time):
        super(ha_ma_zi_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ma_period
        self._highest = Highest()
        self._highest.Length = self.zigzag_length
        self._lowest = Lowest()
        self._lowest.Length = self.zigzag_length
        self.Indicators.Add(self._highest)
        self.Indicators.Add(self._lowest)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self.on_candle).Start()
        self.StartProtection(
            takeProfit=Unit(self.take_profit_pct, UnitTypes.Percent),
            stopLoss=Unit(self.stop_loss_pct, UnitTypes.Percent),
            useMarketOrders=True)
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def on_candle(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return
        high_result = self._highest.Process(candle)
        low_result = self._lowest.Process(candle)
        if not high_result.IsFormed or not low_result.IsFormed:
            return
        highest = float(high_result.ToDecimal())
        lowest = float(low_result.ToDecimal())
        ema_val = float(ema_val)

        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)

        ha_close = (o + h + l + c) / 4.0
        if self._ha_open_prev == 0 and self._ha_close_prev == 0:
            ha_open = (o + c) / 2.0
        else:
            ha_open = (self._ha_open_prev + self._ha_close_prev) / 2.0
        ha_bull = ha_close > ha_open
        ha_bear = ha_close < ha_open

        if h >= highest and self._last_zigzag != h:
            self._last_zigzag = h
            self._last_zigzag_high = h
            self._last_zigzag_low = 0.0
        elif l <= lowest and self._last_zigzag != l:
            self._last_zigzag = l
            self._last_zigzag_low = l
            self._last_zigzag_high = 0.0

        if (self._last_zigzag == self._last_zigzag_low and self._last_zigzag_low > 0 and
                ha_bull and c > ema_val and self.Position <= 0):
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif (self._last_zigzag == self._last_zigzag_high and self._last_zigzag_high > 0 and
                ha_bear and c < ema_val and self.Position >= 0):
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._ha_open_prev = ha_open
        self._ha_close_prev = ha_close

    def CreateClone(self):
        return ha_ma_zi_strategy()
