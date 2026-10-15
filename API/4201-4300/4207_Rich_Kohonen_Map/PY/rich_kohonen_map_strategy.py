import clr
import os
import struct
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class rich_kohonen_map_strategy(Strategy):
    VECTOR_SIZE = 7
    BUY_CAPACITY = 10000
    SELL_CAPACITY = 10000
    HOLD_CAPACITY = 25000
    FILE_SIZE = (BUY_CAPACITY + SELL_CAPACITY + HOLD_CAPACITY) * VECTOR_SIZE * 8

    def __init__(self):
        super(rich_kohonen_map_strategy, self).__init__()

        # Pips are price steps. For the sample BTCUSDT (step 0.01) at 1h the band is 100-1000 USDT, the middle of the hourly
        # open-to-open moves: the quietest quarter of the hours and the rare spikes train the hold map.
        self._min_pips = self.Param("MinPips", 10000.0).SetNotNegative()
        self._max_pips = self.Param("MaxPips", 100000.0).SetGreaterThanZero()
        self._take_profit = self.Param("TakeProfit", 0.0).SetNotNegative()
        self._stop_loss = self.Param("StopLoss", 0.0).SetNotNegative()
        self._lots = self.Param("Lots", 0.1).SetGreaterThanZero()
        self._slippage = self.Param("Slippage", 3).SetNotNegative()
        self._map_path = self.Param("MapPath", "rl.bin")
        self._ea_name = self.Param("EAName", "Rich")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._candles = []
        self._buy_map = []
        self._sell_map = []
        self._hold_map = []

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(rich_kohonen_map_strategy, self).OnReseted()
        self._candles = []
        self._buy_map = []
        self._sell_map = []
        self._hold_map = []

    def OnStarted2(self, time):
        super(rich_kohonen_map_strategy, self).OnStarted2(time)
        self._load_maps()
        self.SubscribeCandles(self.CandleType).Bind(self._process_candle).Start()

    def OnStopped(self):
        self._save_maps()
        super(rich_kohonen_map_strategy, self).OnStopped()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._candles.append(candle)
        if len(self._candles) > 8:
            del self._candles[0]

        if len(self._candles) < 7:
            return

        current = self._build_vector(0)
        previous = self._build_vector(1)
        decision = self._classify(current)
        self._set_target(decision)

        latest = self._candles[-1]
        prior = self._candles[-2]
        step = self.Security.PriceStep if self.Security is not None and self.Security.PriceStep is not None else Decimal.One
        if step <= 0:
            step = Decimal.One

        # Exact decimal steps: in binary floating point a move of exactly MinPips or MaxPips can fall outside the band.
        move_pips = (latest.OpenPrice - prior.OpenPrice) / step
        min_pips = Decimal(self._min_pips.Value)
        max_pips = Decimal(self._max_pips.Value)

        if min_pips <= move_pips <= max_pips:
            self._add_prototype(self._buy_map, previous, self.BUY_CAPACITY)
        elif -max_pips <= move_pips <= -min_pips:
            self._add_prototype(self._sell_map, previous, self.SELL_CAPACITY)
        else:
            self._add_prototype(self._hold_map, previous, self.HOLD_CAPACITY)

    def _classify(self, vector):
        distances = [
            (self._best_distance(self._buy_map, vector), 1),
            (self._best_distance(self._sell_map, vector), -1),
            (self._best_distance(self._hold_map, vector), 0),
        ]
        finite = [item for item in distances if not math.isinf(item[0])]
        if not finite:
            return 0
        return min(finite, key=lambda item: item[0])[1]

    def _set_target(self, decision):
        volume = self._calculate_volume()
        target = decision * volume
        difference = target - float(self.Position)

        if difference > 0:
            self.BuyMarket(difference)
        elif difference < 0:
            self.SellMarket(abs(difference))

    def _calculate_volume(self):
        balance = 0.0
        if self.Portfolio is not None:
            if self.Portfolio.CurrentValue is not None:
                balance = float(self.Portfolio.CurrentValue)
            elif self.Portfolio.BeginValue is not None:
                balance = float(self.Portfolio.BeginValue)

        volume = math.floor(balance / 50.0) / 10.0 if balance > 0 else 0.0
        if volume <= 0:
            volume = float(self._lots.Value)

        if self.Security is not None:
            if self.Security.MaxVolume is not None and float(self.Security.MaxVolume) > 0:
                volume = min(volume, float(self.Security.MaxVolume))
            if self.Security.MinVolume is not None and float(self.Security.MinVolume) > 0:
                volume = max(volume, float(self.Security.MinVolume))
            if self.Security.VolumeStep is not None and float(self.Security.VolumeStep) > 0:
                step = float(self.Security.VolumeStep)
                volume = math.floor(volume / step) * step

        return volume if volume > 0 else float(self._lots.Value)

    def _build_vector(self, offset):
        last_index = len(self._candles) - 1 - offset
        first_index = last_index - 5
        current = self._candles[last_index]
        window = self._candles[first_index:last_index]

        pivot_sum = r1_sum = s1_sum = 0.0

        for c in window:
            pivot, r1, s1 = self._demark(float(c.OpenPrice), float(c.HighPrice), float(c.LowPrice), float(c.ClosePrice))
            pivot_sum += pivot
            r1_sum += r1
            s1_sum += s1

        # The five candles taken as one bar: first open, extreme high and low, last close.
        bar_pivot, bar_r1, bar_s1 = self._demark(
            float(window[0].OpenPrice),
            max(float(c.HighPrice) for c in window),
            min(float(c.LowPrice) for c in window),
            float(window[-1].ClosePrice))

        return [
            float(current.OpenPrice),
            pivot_sum / 5.0,
            r1_sum / 5.0,
            s1_sum / 5.0,
            bar_pivot,
            bar_r1,
            bar_s1,
        ]

    @staticmethod
    def _demark(o, h, l, close):
        if close < o:
            x = h + 2.0 * l + close
        elif close > o:
            x = 2.0 * h + l + close
        else:
            x = h + l + 2.0 * close

        return x / 4.0, x / 2.0 - l, x / 2.0 - h

    @staticmethod
    def _best_distance(map_values, vector):
        if not map_values:
            return float("inf")
        return min(math.sqrt(sum((vector[i] - prototype[i]) ** 2 for i in range(7))) for prototype in map_values)

    def _add_prototype(self, map_values, vector, capacity):
        if len(map_values) < capacity:
            map_values.append(list(vector))

    def _load_maps(self):
        path = str(self._map_path.Value)
        if not path or not os.path.isfile(path):
            return
        try:
            with open(path, "rb") as f:
                data = f.read(self.FILE_SIZE)

            if len(data) < self.FILE_SIZE:
                raise IOError("the file holds {0} bytes, the three matrices need {1}".format(len(data), self.FILE_SIZE))

            buy_map, offset = self._read_map(data, 0, self.BUY_CAPACITY)
            sell_map, offset = self._read_map(data, offset, self.SELL_CAPACITY)
            hold_map, offset = self._read_map(data, offset, self.HOLD_CAPACITY)

            self._buy_map = buy_map
            self._sell_map = sell_map
            self._hold_map = hold_map
        except Exception as ex:
            self._buy_map = []
            self._sell_map = []
            self._hold_map = []
            self.LogWarning("Unable to load Kohonen map '{0}': {1}".format(path, ex))

    def _save_maps(self):
        path = str(self._map_path.Value)
        if not path:
            return
        try:
            with open(path, "wb") as f:
                self._write_map(f, self._buy_map, self.BUY_CAPACITY)
                self._write_map(f, self._sell_map, self.SELL_CAPACITY)
                self._write_map(f, self._hold_map, self.HOLD_CAPACITY)
        except Exception as ex:
            self.LogWarning("Unable to save Kohonen map '{0}': {1}".format(path, ex))

    @classmethod
    def _read_map(cls, data, offset, capacity):
        # A zero-padded matrix of capacity rows; only its non-empty rows are kept.
        count = capacity * cls.VECTOR_SIZE
        end = offset + count * 8
        values = struct.unpack("<%dd" % count, data[offset:end])
        empty = (0.0,) * cls.VECTOR_SIZE
        rows = []
        for i in range(0, count, cls.VECTOR_SIZE):
            row = values[i:i + cls.VECTOR_SIZE]
            if row != empty:
                rows.append(list(row))
        return rows, end

    @classmethod
    def _write_map(cls, f, values, capacity):
        # A matrix of capacity rows whose unused rows are padded with zeros.
        row_format = "<%dd" % cls.VECTOR_SIZE
        for vector in values:
            f.write(struct.pack(row_format, *vector))
        f.write(bytes(8 * cls.VECTOR_SIZE * (capacity - len(values))))

    def CreateClone(self):
        return rich_kohonen_map_strategy()
