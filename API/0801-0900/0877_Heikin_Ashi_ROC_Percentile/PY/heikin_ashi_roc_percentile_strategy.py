import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from collections import deque
from System import TimeSpan, Decimal, DateTimeOffset
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy

class heikin_ashi_roc_percentile_strategy(Strategy):
    """
    Heikin Ashi ROC Percentile strategy.
    The Heikin Ashi close is smoothed with an SMA of RocLength and its Rate of Change over RocLength candles is measured. The upper
    band is the highest and the lower band the lowest ROC of the previous RocLength candles. ROC crossing back above the lower band
    buys and crossing back below the upper band sells, reversing an opposite position. Trading starts at StartDate and a percent
    stop limits the loss.
    """

    def __init__(self):
        super(heikin_ashi_roc_percentile_strategy, self).__init__()
        self._roc_length = self.Param("RocLength", 100).SetGreaterThanZero().SetDisplay("ROC Length", "Length of the smoothing SMA, the ROC and the band window", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percent from the entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._start_date = self.Param("StartDate", DateTimeOffset(2015, 3, 3, 0, 0, 0, TimeSpan.Zero)).SetDisplay("Start Date", "Date trading starts from", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._ha_closes = deque()
        self._ha_close_sum = 0.0
        self._smoothed = deque()
        self._roc_history = deque()
        self._ha_open = None
        self._ha_close = None
        self._prev_roc = None
        self._prev_upper = None
        self._prev_lower = None

    def OnReseted(self):
        super(heikin_ashi_roc_percentile_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(heikin_ashi_roc_percentile_strategy, self).OnStarted2(time)

        self._reset_state()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        sl = float(self._stop_loss_percent.Value)
        self.StartProtection(Unit(), Unit(Decimal(sl), UnitTypes.Percent) if sl > 0 else Unit(), useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        open_price = float(candle.OpenPrice)
        close = float(candle.ClosePrice)
        ha_close = (open_price + float(candle.HighPrice) + float(candle.LowPrice) + close) / 4.0
        if self._ha_open is not None and self._ha_close is not None:
            ha_open = (self._ha_open + self._ha_close) / 2.0
        else:
            ha_open = (open_price + close) / 2.0

        self._ha_open = ha_open
        self._ha_close = ha_close

        length = self._roc_length.Value

        # SMA of the Heikin Ashi close.
        self._ha_closes.append(ha_close)
        self._ha_close_sum += ha_close
        if len(self._ha_closes) > length:
            self._ha_close_sum -= self._ha_closes.popleft()
        if len(self._ha_closes) < length:
            return

        smoothed = self._ha_close_sum / length

        self._smoothed.append(smoothed)
        if len(self._smoothed) <= length:
            return

        past = self._smoothed.popleft()
        if past == 0:
            return

        roc = (smoothed - past) / past * 100.0

        upper = None
        lower = None
        if len(self._roc_history) >= length:
            upper = max(self._roc_history)
            lower = min(self._roc_history)

        prev_roc = self._prev_roc
        prev_upper = self._prev_upper
        prev_lower = self._prev_lower

        self._roc_history.append(roc)
        while len(self._roc_history) > length:
            self._roc_history.popleft()

        self._prev_roc = roc
        self._prev_upper = upper
        self._prev_lower = lower

        if prev_roc is None or upper is None or lower is None or prev_upper is None or prev_lower is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if candle.OpenTime < self._start_date.Value.UtcDateTime:
            return

        if prev_roc <= prev_lower and roc > lower and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif prev_roc >= prev_upper and roc < upper and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return heikin_ashi_roc_percentile_strategy()
