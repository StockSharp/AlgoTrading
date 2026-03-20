import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class volume_and_volatility_ratio_indicator_wodi_strategy(Strategy):
    def __init__(self):
        super(volume_and_volatility_ratio_indicator_wodi_strategy, self).__init__()
        self._vol_length = self.Param("VolLength", 20) \
            .SetDisplay("Volume MA Length", "Volume average period", "Parameters")
        self._index_length = self.Param("IndexLength", 20) \
            .SetDisplay("Index Length", "Volatility index average period", "Parameters")
        self._stop_pct = self.Param("StopPct", 0.5) \
            .SetDisplay("Stop %", "Stop loss percent", "Risk")
        self._tp_pct = self.Param("TpPct", 1) \
            .SetDisplay("TP %", "Take profit percent", "Risk")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 24) \
            .SetDisplay("Signal Cooldown", "Bars to wait between trades", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._entry_price = 0.0
        self._stop_dist = 0.0
        self._prev_candle = None
        self._prev_prev_candle = None
        self._cooldown_remaining = 0

    @property
    def vol_length(self):
        return self._vol_length.Value

    @property
    def index_length(self):
        return self._index_length.Value

    @property
    def stop_pct(self):
        return self._stop_pct.Value

    @property
    def tp_pct(self):
        return self._tp_pct.Value

    @property
    def signal_cooldown_bars(self):
        return self._signal_cooldown_bars.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volume_and_volatility_ratio_indicator_wodi_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._stop_dist = 0.0
        self._prev_candle = None
        self._prev_prev_candle = None
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(volume_and_volatility_ratio_indicator_wodi_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = 2
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, _dummy):
        if candle.State != CandleStates.Finished:
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        vol = candle.TotalVolume
        volatility = ((candle.HighPrice - candle.LowPrice) / candle.ClosePrice * 100 if candle.ClosePrice > 0 else 0)
        vol_index = vol * volatility
        self._volumes.append(vol)
        self._vol_indices.append(vol_index)
        while (len(self._volumes) > self.vol_length + 1)
        self._volumes.pop(0)
        while (len(self._vol_indices) > self.index_length + 1)
        self._vol_indices.pop(0)
        # TP/SL management
        if self.Position > 0 and self._entry_price > 0 and self._stop_dist > 0:
            if candle.ClosePrice <= self._entry_price - self._stop_dist or candle.ClosePrice >= self._entry_price + self._stop_dist * (self.tp_pct / self.stop_pct):
                self.SellMarket()
                self._entry_price = 0
                self._stop_dist = 0
                self._cooldown_remaining = self.signal_cooldown_bars
        elif self.Position < 0 and self._entry_price > 0 and self._stop_dist > 0:
            if candle.ClosePrice >= self._entry_price + self._stop_dist or candle.ClosePrice <= self._entry_price - self._stop_dist * (self.tp_pct / self.stop_pct):
                self.BuyMarket()
                self._entry_price = 0
                self._stop_dist = 0
                self._cooldown_remaining = self.signal_cooldown_bars
        if len(self._volumes) < self.vol_length or len(self._vol_indices) < self.index_length or self._prev_candle == None or self._prev_prev_candle == None:
            self._prev_prev_candle = self._prev_candle
            self._prev_candle = candle
            return
        # Calculate averages
        vol_avg = self._volumes.Take(self.vol_length).Sum() / self.vol_length
        index_avg = self._vol_indices.Take(self.index_length).Sum() / self.index_length
        # Entry conditions
        high_vol = vol > vol_avg
        high_vol_index = vol_index > index_avg * 2.5
        is_long_pattern = high_vol and high_vol_index
        and self._prev_candle.ClosePrice < self._prev_prev_candle.ClosePrice
        and candle.ClosePrice > self._prev_candle.ClosePrice
        is_short_pattern = high_vol and high_vol_index
        and self._prev_candle.ClosePrice > self._prev_prev_candle.ClosePrice
        and candle.ClosePrice < self._prev_candle.ClosePrice
        if self._cooldown_remaining == 0 and is_long_pattern and self.Position == 0:
            self.BuyMarket()
            self._entry_price = candle.ClosePrice
            self._stop_dist = candle.ClosePrice * self.stop_pct / 100
            self._cooldown_remaining = self.signal_cooldown_bars
        elif self._cooldown_remaining == 0 and is_short_pattern and self.Position == 0:
            self.SellMarket()
            self._entry_price = candle.ClosePrice
            self._stop_dist = candle.ClosePrice * self.stop_pct / 100
            self._cooldown_remaining = self.signal_cooldown_bars
        self._prev_prev_candle = self._prev_candle
        self._prev_candle = candle

    def CreateClone(self):
        return volume_and_volatility_ratio_indicator_wodi_strategy()
