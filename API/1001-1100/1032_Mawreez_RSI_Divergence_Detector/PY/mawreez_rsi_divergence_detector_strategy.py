import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class mawreez_rsi_divergence_detector_strategy(Strategy):
    def __init__(self):
        super(mawreez_rsi_divergence_detector_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Candles timeframe", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Length", "RSI period", "General")
        self._min_div_length = self.Param("MinDivLength", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("Min Div Length", "Minimum divergence length", "General")
        self._max_div_length = self.Param("MaxDivLength", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Div Length", "Maximum divergence length", "General")
        self._min_price_move_percent = self.Param("MinPriceMovePercent", 0.35) \
            .SetGreaterThanZero() \
            .SetDisplay("Min Price Move %", "Minimum price distance for divergence", "General")
        self._min_rsi_move = self.Param("MinRsiMove", 6.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Min RSI Move", "Minimum RSI distance for divergence", "General")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 12) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "General")
        self._price_history = []
        self._rsi_history = []
        self._index = 0
        self._bars_from_signal = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(mawreez_rsi_divergence_detector_strategy, self).OnReseted()
        self._price_history = []
        self._rsi_history = []
        self._index = 0
        self._bars_from_signal = 0

    def OnStarted2(self, time):
        super(mawreez_rsi_divergence_detector_strategy, self).OnStarted2(time)
        buf_size = self._max_div_length.Value + 1
        self._price_history = [0.0] * buf_size
        self._rsi_history = [0.0] * buf_size
        self._index = 0
        self._bars_from_signal = self._signal_cooldown_bars.Value
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self._rsi_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self.OnProcess).Start()

    def OnProcess(self, candle, rsi):
        if candle.State != CandleStates.Finished:
            return
        if not self._rsi.IsFormed:
            return
        price = float(candle.ClosePrice)
        rv = float(rsi)
        buf_size = len(self._price_history)
        pos = self._index % buf_size
        self._price_history[pos] = price
        self._rsi_history[pos] = rv
        self._index += 1
        max_div = self._max_div_length.Value
        if self._index <= max_div:
            return
        self._bars_from_signal += 1
        cd = self._signal_cooldown_bars.Value
        if self._bars_from_signal < cd:
            return
        min_div = self._min_div_length.Value
        min_price_pct = float(self._min_price_move_percent.Value)
        min_rsi_mv = float(self._min_rsi_move.Value)
        winner = 0
        for l in range(min_div, max_div + 1):
            idx = (self._index - l - 1) % buf_size
            if idx < 0:
                idx += buf_size
            past_price = self._price_history[idx]
            past_rsi = self._rsi_history[idx]
            dsrc = price - past_price
            dosc = rv - past_rsi
            price_move_pct = abs(dsrc) / price * 100.0 if price > 0.0 else 0.0
            rsi_move = abs(dosc)
            if price_move_pct < min_price_pct or rsi_move < min_rsi_mv:
                continue
            if (dsrc > 0 and dosc > 0) or (dsrc < 0 and dosc < 0) or (dsrc == 0 or dosc == 0):
                continue
            if winner == 0:
                if dsrc < 0 and dosc > 0:
                    winner = 1
                elif dsrc > 0 and dosc < 0:
                    winner = -1
        if winner > 0 and self.Position <= 0:
            self.BuyMarket()
            self._bars_from_signal = 0
        elif winner < 0 and self.Position >= 0:
            self.SellMarket()
            self._bars_from_signal = 0

    def CreateClone(self):
        return mawreez_rsi_divergence_detector_strategy()
