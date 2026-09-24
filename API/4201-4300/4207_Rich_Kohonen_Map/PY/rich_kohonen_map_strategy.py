import clr
import os
import struct
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class rich_kohonen_map_strategy(Strategy):
    MAGIC = 0x534B4D31
    VECTOR_SIZE = 7

    def __init__(self):
        super(rich_kohonen_map_strategy, self).__init__()

        self._min_pips = self.Param("MinPips", 10.0).SetNotNegative()
        self._max_pips = self.Param("MaxPips", 100.0).SetGreaterThanZero()
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
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0:
            step = 1.0

        move_pips = (float(latest.OpenPrice) - float(prior.OpenPrice)) / step
        min_pips = float(self._min_pips.Value)
        max_pips = float(self._max_pips.Value)

        if min_pips <= move_pips <= max_pips:
            self._add_prototype(self._buy_map, previous, 10000)
        elif -max_pips <= move_pips <= -min_pips:
            self._add_prototype(self._sell_map, previous, 10000)
        else:
            self._add_prototype(self._hold_map, previous, 25000)

        self._save_maps()

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
        current = self._candles[last_index]

        pivot_sum = r1_sum = s1_sum = range_sum = body_sum = close_sum = 0.0

        for i in range(last_index - 5, last_index):
            c = self._candles[i]
            o = float(c.OpenPrice)
            h = float(c.HighPrice)
            l = float(c.LowPrice)
            close = float(c.ClosePrice)

            if close < o:
                x = h + 2.0 * l + close
            elif close > o:
                x = 2.0 * h + l + close
            else:
                x = h + l + 2.0 * close

            pivot_sum += x / 4.0
            r1_sum += x / 2.0 - l
            s1_sum += x / 2.0 - h
            range_sum += h - l
            body_sum += close - o
            close_sum += close

        return [
            float(current.OpenPrice),
            pivot_sum / 5.0,
            r1_sum / 5.0,
            s1_sum / 5.0,
            range_sum / 5.0,
            body_sum / 5.0,
            close_sum / 5.0,
        ]

    @staticmethod
    def _best_distance(map_values, vector):
        if not map_values:
            return float("inf")
        return min(math.sqrt(sum((vector[i] - prototype[i]) ** 2 for i in range(7))) for prototype in map_values)

    def _add_prototype(self, map_values, vector, capacity):
        if map_values:
            best_index = min(range(len(map_values)), key=lambda i: self._best_distance([map_values[i]], vector))
            prototype = map_values[best_index]
            for j in range(self.VECTOR_SIZE):
                prototype[j] += 0.05 * (vector[j] - prototype[j])

        if len(map_values) < capacity:
            map_values.append(list(vector))

    def _load_maps(self):
        path = str(self._map_path.Value)
        if not path or not os.path.exists(path):
            return
        try:
            with open(path, "rb") as f:
                magic = struct.unpack("<i", f.read(4))[0]
                if magic != self.MAGIC:
                    return
                self._buy_map = self._read_map(f, 10000)
                self._sell_map = self._read_map(f, 10000)
                self._hold_map = self._read_map(f, 25000)
        except Exception:
            self._buy_map = []
            self._sell_map = []
            self._hold_map = []

    def _save_maps(self):
        path = str(self._map_path.Value)
        if not path:
            return
        try:
            with open(path, "wb") as f:
                f.write(struct.pack("<i", self.MAGIC))
                self._write_map(f, self._buy_map)
                self._write_map(f, self._sell_map)
                self._write_map(f, self._hold_map)
        except Exception:
            pass

    @classmethod
    def _read_map(cls, f, capacity):
        raw = f.read(4)
        if len(raw) != 4:
            return []
        count = min(struct.unpack("<i", raw)[0], capacity)
        result = []
        for _ in range(count):
            data = f.read(8 * cls.VECTOR_SIZE)
            if len(data) != 8 * cls.VECTOR_SIZE:
                break
            result.append(list(struct.unpack("<" + "d" * cls.VECTOR_SIZE, data)))
        return result

    @classmethod
    def _write_map(cls, f, values):
        f.write(struct.pack("<i", len(values)))
        for vector in values:
            f.write(struct.pack("<" + "d" * cls.VECTOR_SIZE, *vector))

    def CreateClone(self):
        return rich_kohonen_map_strategy()
