# Estratégia Silver Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma estratégia de seguidor de tendência baseada no indicador SilverTrend personalizado. O indicador constrói um canal de preços dinâmico usando a máxima mais alta e a mínima mais baixa em uma janela de lookback e um fator de risco. Um sinal de negociação ocorre quando o preço cruza o canal e a direção da tendência se reverte.

## Detalhes

- **Entrada**: Comprar quando o indicador muda para uma tendência de alta. Vender quando o indicador muda para uma tendência de baixa.
- **Saída**: A posição é revertida no sinal oposto.
- **Indicadores**: Highest, Lowest, SimpleMovingAverage (dentro do cálculo do SilverTrend).
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Ssp` = 9 — número de barras para o cálculo do canal.
  - `Risk` = 3 — percentual que reduz a largura do canal.
  - `CandleType` = candles de 1 hora.
- **Direção**: Tanto comprado quanto vendido.

O indicador SilverTrend calcula o intervalo médio de máxima-mínima durante `Ssp + 1` barras e encontra a máxima mais alta e a mínima mais baixa durante `Ssp` barras. Os limites do canal são:

```
smin = minLow + (maxHigh - minLow) * (33 - Risk) / 100
smax = maxHigh - (maxHigh - minLow) * (33 - Risk) / 100
```

Se o fechamento cair abaixo de `smin`, a tendência torna-se baixista. Se o fechamento subir acima de `smax`, a tendência torna-se altista. Um sinal é gerado quando a tendência muda, e a estratégia inverte imediatamente sua posição de acordo.
