import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy


class x2_ma_digit_dm361_strategy(Strategy):
    def __init__(self):
        super(x2_ma_digit_dm361_strategy, self).__init__()
        self._fast_ma_length = self.Param("FastMaLength", 5) \
            .SetDisplay("Fast MA Length", "Length of fast moving average", "Moving Averages")
        self._slow_ma_length = self.Param("SlowMaLength", 12) \
            .SetDisplay("Slow MA Length", "Length of slow moving average", "Moving Averages")
        self._adx_length = self.Param("AdxLength", 14) \
            .SetDisplay("ADX Length", "Length of Average Directional Index", "Directional Movement")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candles timeframe", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss percent", "Risk Management")
        self._take_profit_percent = self.Param("TakeProfitPercent", 2.0) \
            .SetDisplay("Take Profit %", "Take profit percent", "Risk Management")

    @property
    def fast_ma_length(self):
        return self._fast_ma_length.Value

    @property
    def slow_ma_length(self):
        return self._slow_ma_length.Value

    @property
    def adx_length(self):
        return self._adx_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def stop_loss_percent(self):
        return self._stop_loss_percent.Value

    @property
    def take_profit_percent(self):
        return self._take_profit_percent.Value

    def OnStarted(self, time):
        super(x2_ma_digit_dm361_strategy, self).OnStarted(time)
        fast_ma = SimpleMovingAverage()
        fast_ma.Length = int(self.fast_ma_length)
        slow_ma = SimpleMovingAverage()
        slow_ma.Length = int(self.slow_ma_length)
        adx = AverageDirectionalIndex()
        adx.Length = int(self.adx_length)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(fast_ma, slow_ma, adx, self.process_candle).Start()
        self.StartProtection(
            takeProfit=Unit(float(self.take_profit_percent), UnitTypes.Percent),
            stopLoss=Unit(float(self.stop_loss_percent), UnitTypes.Percent),
            useMarketOrders=True)
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawIndicator(area, adx)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fast_ma_value, slow_ma_value, adx_value):
        if candle.State != CandleStates.Finished:
            return
        if not fast_ma_value.IsFinal or not slow_ma_value.IsFinal or not adx_value.IsFinal:
            return
        fast = float(fast_ma_value)
        slow = float(slow_ma_value)
        plus_di = adx_value.Dx.Plus
        minus_di = adx_value.Dx.Minus
        if plus_di is None or minus_di is None:
            return
        plus_di = float(plus_di)
        minus_di = float(minus_di)
        if fast > slow and plus_di > minus_di and self.Position <= 0:
            self.BuyMarket()
        elif fast < slow and minus_di > plus_di and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return x2_ma_digit_dm361_strategy()
