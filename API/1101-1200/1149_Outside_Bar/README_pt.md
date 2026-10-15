# Estratégia de Barra Externa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera rompimentos de barras externas. Uma barra externa de alta ocorre quando a máxima da vela atual está acima da máxima anterior e sua mínima está abaixo da mínima anterior. As ordens são colocadas dentro da barra com tomada de lucro parcial opcional e movimento do stop para o ponto de equilíbrio.

## Detalhes

- **Critérios de entrada**: Barra externa com classificação de alta ou baixa.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop-loss ou take-profit derivados do intervalo da barra.
- **Stops**: Sim.
- **Valores padrão**:
  - `CandleType` = 5 minute
  - `EntryPercentage` = 0.5
  - `TpPercentage` = 1
  - `PartialRR` = 1
  - `PartialExitPercent` = 0.5
  - `StopLossOffset` = 10
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: Candlestick
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
