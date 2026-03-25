import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, DateTimeOffset, DateTime, TimeSpan as SysTimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class divergence_expert_strategy(Strategy):
    def __init__(self):
        super(divergence_expert_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI calculation period", "Parameters")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss (%)", "Max risk per trade in percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._start_date = self.Param("StartDate", DateTimeOffset(DateTime(2017, 1, 1), SysTimeSpan.Zero)) \
            .SetDisplay("Start Date", "Backtest start date", "General")
        self._end_date = self.Param("EndDate", DateTimeOffset(DateTime(2024, 7, 1), SysTimeSpan.Zero)) \
            .SetDisplay("End Date", "Backtest end date", "General")
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._last_price_high = 0.0
        self._last_price_low = 0.0
        self._last_rsi_high = 0.0
        self._last_rsi_low = 0.0

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def stop_loss_percent(self):
        return self._stop_loss_percent.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def start_date(self):
        return self._start_date.Value

    @property
    def end_date(self):
        return self._end_date.Value

    def OnReseted(self):
        super(divergence_expert_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._last_price_high = 0.0
        self._last_price_low = 0.0
        self._last_rsi_high = 0.0
        self._last_rsi_low = 0.0

    def OnStarted(self, time):
        super(divergence_expert_strategy, self).OnStarted(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, rsi):
        if candle.State != CandleStates.Finished:
            return
        t = DateTimeOffset(candle.OpenTime) if not isinstance(candle.OpenTime, DateTimeOffset) else candle.OpenTime
        in_range = t >= self.start_date and t <= self.end_date
        if not in_range:
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
            return
        rsi = float(rsi)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        sl_pct = float(self.stop_loss_percent)
        # Track new highs for bearish divergence detection
        if high > self._last_price_high:
            if self._last_price_high != 0.0 and rsi < self._last_rsi_high and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
                self._entry_price = close
                self._stop_price = self._entry_price * (1.0 + sl_pct / 100.0)
            self._last_price_high = high
            self._last_rsi_high = rsi
        # Track new lows for bullish divergence detection
        if self._last_price_low == 0.0 or low < self._last_price_low:
            if self._last_price_low != 0.0 and rsi > self._last_rsi_low and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
                self._entry_price = close
                self._stop_price = self._entry_price * (1.0 - sl_pct / 100.0)
            self._last_price_low = low
            self._last_rsi_low = rsi
        if self.Position > 0 and low <= self._stop_price:
            self.SellMarket()
        elif self.Position < 0 and high >= self._stop_price:
            self.BuyMarket()

    def CreateClone(self):
        return divergence_expert_strategy()
