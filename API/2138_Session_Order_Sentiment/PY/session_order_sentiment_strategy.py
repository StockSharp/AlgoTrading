import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class session_order_sentiment_strategy(Strategy):
    def __init__(self):
        super(session_order_sentiment_strategy, self).__init__()
        self._volume_ratio = self.Param("VolumeRatio", 1.5) \
            .SetDisplay("Volume Ratio", "Bull/bear volume ratio for entry", "General")
        self._lookback = self.Param("Lookback", 10) \
            .SetDisplay("Lookback", "Number of candles to look back", "General")
        self._stop_loss_pct = self.Param("StopLossPct", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for analysis", "General")
        self._volume_history = []
        self._entry_price = 0.0

    @property
    def volume_ratio(self):
        return self._volume_ratio.Value

    @property
    def lookback(self):
        return self._lookback.Value

    @property
    def stop_loss_pct(self):
        return self._stop_loss_pct.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(session_order_sentiment_strategy, self).OnReseted()
        self._volume_history = []
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(session_order_sentiment_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        is_bull = float(candle.ClosePrice) >= float(candle.OpenPrice)
        vol = float(candle.TotalVolume)
        lb = int(self.lookback)
        self._volume_history.append((vol, is_bull))
        if len(self._volume_history) > lb:
            self._volume_history.pop(0)
        if len(self._volume_history) < lb:
            return
        bull_volume = 0.0
        bear_volume = 0.0
        for v, b in self._volume_history:
            if b:
                bull_volume += v
            else:
                bear_volume += v
        if bear_volume == 0:
            bear_volume = 1.0
        if bull_volume == 0:
            bull_volume = 1.0
        bull_bear_ratio = bull_volume / bear_volume
        bear_bull_ratio = bear_volume / bull_volume
        close = float(candle.ClosePrice)
        sl = float(self.stop_loss_pct)
        vr = float(self.volume_ratio)
        # Check stop loss
        if self.Position > 0 and close <= self._entry_price * (1.0 - sl / 100.0):
            self.SellMarket()
            return
        if self.Position < 0 and close >= self._entry_price * (1.0 + sl / 100.0):
            self.BuyMarket()
            return
        # Bullish sentiment
        if bull_bear_ratio >= vr:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self._entry_price = close
                self.BuyMarket()
        # Bearish sentiment
        elif bear_bull_ratio >= vr:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self._entry_price = close
                self.SellMarket()

    def CreateClone(self):
        return session_order_sentiment_strategy()
