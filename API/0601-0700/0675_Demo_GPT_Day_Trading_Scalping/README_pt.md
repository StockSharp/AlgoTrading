# Demo GPT - Estratégia de Scalping de Day Trading
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Entra comprado após dois candles verdes consecutivos quando o segundo abre acima do fechamento do primeiro e o volume excede sua média de 20 barras. Fecha a posição em dois candles vermelhos consecutivos onde o segundo abre abaixo do fechamento do primeiro sob a mesma condição de volume. Calcula EMA(20), VWAP e níveis Camarilla como referência.

### Regras executadas e padrões

Padrões: `EmaLength=20`, `VolumeLookback=20`, `CandleType=1 minuto`. Após **20 candles concluídos anteriores**, `volumeHigh = TotalVolume atual > média dos 20 volumes anteriores`; o candle atual não compõe sua própria média. Sem posição, abre **somente long** quando os dois candles são verdes (`Close > Open`), o segundo abre com **`Open > Close anterior`** e há confirmação de volume. Encerra a **posição long restante inteira** quando ambos são vermelhos (`Close < Open`), `Open atual < Close anterior` e há volume alto. Sem gap estrito na abertura ou confirmação de volume **não há sinal**; não abre short. Para referência apenas: EMA20 dos fechamentos, VWAP diário `sum(preço típico × volume)/sum(volume)` reiniciado a cada dia, Camarilla anterior `H3/L3 = fechamento anterior ± 1.1 × (máxima − mínima) / 4`. Essas referências não filtram negócios. Sem stop-loss adicional.

