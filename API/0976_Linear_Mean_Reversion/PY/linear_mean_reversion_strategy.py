import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy

class linear_mean_reversion_strategy(Strategy):
    """
    Linear mean reversion based on price z-score.
    Buys when z-score below entry threshold, sells above. Exits near zero.
    """

    def __init__(self):
        super(linear_mean_reversion_strategy, self).__init__()
        self._half_life = self.Param("HalfLife", 30) \
            .SetDisplay("Half-Life", "Lookback window", "General")
        self._entry_threshold = self.Param("EntryThreshold", 2.2) \
            .SetDisplay("Entry Threshold", "Z-score entry threshold", "Parameters")
        self._exit_threshold = self.Param("ExitThreshold", 0.5) \
            .SetDisplay("Exit Threshold", "Z-score exit threshold", "Parameters")
        self._stop_loss_points = self.Param("StopLossPoints", 50.0) \
            .SetDisplay("Stop Loss Points", "Fixed stop loss in price points", "Risk Management")
        self._cooldown_bars = self.Param("CooldownBars", 12) \
            .SetDisplay("Cooldown Bars", "Min bars between trade actions", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(10))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._entry_price = 0.0
        self._bars_from_trade = 999999

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(linear_mean_reversion_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._bars_from_trade = 999999

    def OnStarted(self, time):
        super(linear_mean_reversion_strategy, self).OnStarted(time)

        sma = SimpleMovingAverage()
        sma.Length = self._half_life.Value
        std = StandardDeviation()
        std.Length = self._half_life.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, std, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, mean_val, dev_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        mean = float(mean_val)
        deviation = float(dev_val)

        if deviation == 0:
            return

        close = float(candle.ClosePrice)
        zscore = (close - mean) / deviation
        self._bars_from_trade += 1

        if self._bars_from_trade < self._cooldown_bars.Value:
            return

        entry_th = self._entry_threshold.Value
        exit_th = self._exit_threshold.Value
        sl_pts = self._stop_loss_points.Value

        if self.Position == 0:
            if zscore < -entry_th:
                self.BuyMarket()
                self._entry_price = close
                self._bars_from_trade = 0
            elif zscore > entry_th:
                self.SellMarket()
                self._entry_price = close
                self._bars_from_trade = 0
        elif self.Position > 0:
            stop = self._entry_price - sl_pts
            if close <= stop or zscore >= -exit_th:
                self.SellMarket()
                self._entry_price = 0.0
                self._bars_from_trade = 0
        elif self.Position < 0:
            stop = self._entry_price + sl_pts
            if close >= stop or zscore <= exit_th:
                self.BuyMarket()
                self._entry_price = 0.0
                self._bars_from_trade = 0

    def CreateClone(self):
        return linear_mean_reversion_strategy()
