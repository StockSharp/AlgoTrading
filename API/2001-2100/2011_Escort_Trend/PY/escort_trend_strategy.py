import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, OrderStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import WeightedMovingAverage, CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class escort_trend_strategy(Strategy):
    """WMA and CCI-confirmed MACD crossover with native risk and close-based trailing."""

    def __init__(self):
        super(escort_trend_strategy, self).__init__()
        self._fast_wma_period = self.Param("FastWmaPeriod", 8).SetGreaterThanZero().SetDisplay("Fast WMA", "Fast WMA", "Indicator")
        self._slow_wma_period = self.Param("SlowWmaPeriod", 18).SetGreaterThanZero().SetDisplay("Slow WMA", "Slow WMA", "Indicator")
        self._cci_period = self.Param("CciPeriod", 14).SetGreaterThanZero().SetDisplay("CCI Period", "CCI period", "Indicator")
        self._cci_threshold = self.Param("CciThreshold", 100.0).SetNotNegative().SetDisplay("CCI Threshold", "CCI confirmation", "Indicator")
        self._macd_fast = self.Param("MacdFastEma", 8).SetGreaterThanZero().SetDisplay("MACD Fast EMA", "MACD fast", "Indicator")
        self._macd_slow = self.Param("MacdSlowEma", 18).SetGreaterThanZero().SetDisplay("MACD Slow EMA", "MACD slow", "Indicator")
        self._macd_signal = self.Param("MacdSignalPeriod", 9).SetGreaterThanZero().SetDisplay("MACD Signal", "MACD signal EMA", "Indicator")
        self._stop_loss = self.Param("StopLoss", 55.0).SetNotNegative().SetDisplay("Stop Loss", "Absolute stop distance", "Risk")
        self._take_profit = self.Param("TakeProfit", 200.0).SetNotNegative().SetDisplay("Take Profit", "Absolute target distance", "Risk")
        self._trailing_stop = self.Param("TrailingStop", 35.0).SetNotNegative().SetDisplay("Trailing Stop", "Close-based trailing distance", "Risk")
        self._trailing_step = self.Param("TrailingStep", 3.0).SetNotNegative().SetDisplay("Trailing Step", "Trail adjustment increment", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Signal candles", "General")
        self._reset_history()

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _reset_history(self):
        self._macd_fast_value = self._macd_slow_value = self._signal_value = self._previous_difference = None
        self._entry = self._favorable = self._trailing_level = None
        self._tracked_position = 0.0
        self._samples = 0
        self._pending = None

    def OnReseted(self):
        super(escort_trend_strategy, self).OnReseted()
        self._reset_history()

    def OnStarted2(self, time):
        super(escort_trend_strategy, self).OnStarted2(time)
        self._reset_history()
        self.StartProtection(takeProfit=Unit(self._take_profit.Value, UnitTypes.Absolute),
                             stopLoss=Unit(self._stop_loss.Value, UnitTypes.Absolute),
                             useMarketOrders=True)
        fast = WeightedMovingAverage()
        fast.Length = int(self._fast_wma_period.Value)
        slow = WeightedMovingAverage()
        slow.Length = int(self._slow_wma_period.Value)
        cci = CommodityChannelIndex()
        cci.Length = int(self._cci_period.Value)
        self.SubscribeCandles(self.CandleType).Bind(fast, slow, cci, self.ProcessCandle).Start()

    def OnNewPosition(self, position):
        super(escort_trend_strategy, self).OnNewPosition(position)
        self._observe_position(position)

    def OnPositionChanged(self, position):
        super(escort_trend_strategy, self).OnPositionChanged(position)
        self._observe_position(position)

    def OnPositionReceived(self, position):
        super(escort_trend_strategy, self).OnPositionReceived(position)
        self._observe_position(position)

    def _observe_position(self, position):
        if position.Security != self.Security or position.Portfolio != self.Portfolio:
            return
        size = float(position.CurrentValue or 0)
        if size == 0:
            self._entry = self._favorable = self._trailing_level = None
            self._tracked_position = 0.0
            return
        if position.AveragePrice is None or position.AveragePrice <= 0:
            return
        average = float(position.AveragePrice)
        if self._entry == average and self._tracked_position * size > 0:
            self._tracked_position = size
            return
        self._entry = self._favorable = average
        self._trailing_level = None
        self._tracked_position = size

    @staticmethod
    def _ema(previous, value, period):
        return value if previous is None else previous + 2.0 / (period + 1.0) * (value - previous)

    def ProcessCandle(self, candle, fast_wma, slow_wma, cci):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        self._macd_fast_value = self._ema(self._macd_fast_value, close, int(self._macd_fast.Value))
        self._macd_slow_value = self._ema(self._macd_slow_value, close, int(self._macd_slow.Value))
        macd = self._macd_fast_value - self._macd_slow_value
        self._signal_value = self._ema(self._signal_value, macd, int(self._macd_signal.Value))
        difference = macd - self._signal_value
        self._samples += 1
        cross_up = self._previous_difference is not None and self._previous_difference <= 0 and difference > 0
        cross_down = self._previous_difference is not None and self._previous_difference >= 0 and difference < 0
        self._previous_difference = difference
        required = max(int(self._fast_wma_period.Value), int(self._slow_wma_period.Value),
                       int(self._cci_period.Value), int(self._macd_slow.Value) + int(self._macd_signal.Value))
        if self._samples < required:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending is not None and self._pending.State not in (OrderStates.Done, OrderStates.Failed):
            return

        position = float(self.Position)
        distance = float(self._trailing_stop.Value)
        step = float(self._trailing_step.Value)
        if position != 0 and distance > 0 and self._entry is not None and self._favorable is not None:
            side_long = position > 0
            advance = close - self._favorable if side_long else self._favorable - close
            if advance >= step:
                self._favorable = close
            if self._trailing_level is None and (close - self._entry if side_long else self._entry - close) >= distance:
                self._trailing_level = close - distance if side_long else close + distance
            elif self._trailing_level is not None and advance >= step:
                candidate = close - distance if side_long else close + distance
                self._trailing_level = max(self._trailing_level, candidate) if side_long else min(self._trailing_level, candidate)
            if self._trailing_level is not None and (close <= self._trailing_level if side_long else close >= self._trailing_level):
                self._pending = self.SellMarket(self.Position) if side_long else self.BuyMarket(-self.Position)
                return

        buy = float(fast_wma) > float(slow_wma) and cross_up and float(cci) > float(self._cci_threshold.Value)
        sell = float(fast_wma) < float(slow_wma) and cross_down and float(cci) < -float(self._cci_threshold.Value)
        if buy:
            if position < 0:
                self._pending = self.BuyMarket(-self.Position + self.Volume)
            elif position == 0:
                self._pending = self.BuyMarket(self.Volume)
        elif sell:
            if position > 0:
                self._pending = self.SellMarket(self.Position + self.Volume)
            elif position == 0:
                self._pending = self.SellMarket(self.Volume)

    def CreateClone(self):
        return escort_trend_strategy()
