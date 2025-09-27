# Стратегия Energy Advanced Policy

**Energy Advanced Policy** использует сочетание настроений политики и базовых технических фильтров.

- **Лонг**: EMA(21) выше EMA(55), RSI ниже уровня перекупленности, полосы Боллинджера не сжаты.
- **Выход**: RSI превышает уровень перекупленности или тренд EMA меняется.

## Параметры
- `NewsSentiment` – ручное настроение.
- `EnableNewsFilter` – использовать фильтр новостей.
- `EnablePolicyDetection` – учитывать события политики.
- `PolicyVolumeThreshold` – множитель объёма.
- `PolicyPriceThreshold` – порог изменения цены (%).
- `RsiLength` – период RSI.
- `RsiOverbought` – уровень перекупленности RSI.
- `FastLength` – период быстрой EMA.
- `SlowLength` – период медленной EMA.
- `BbLength` / `BbMult` – настройки полос Боллинджера.

Индикаторы: RSI, EMA, Bollinger Bands.
