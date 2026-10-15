# Divergência CVD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia combina a divergência do delta de volume acumulado (CVD) com Hull Moving Averages, RSI, MACD e um filtro de volume. Uma operação é aberta quando tendência, momentum e volume concordam e o CVD mostra divergência ou continua na direção do trade. As posições fecham em sinais opostos ou cruzamento de indicadores.

## Detalhes

- **Critérios de entrada**: Alinhamento de tendência pelo HMA, confirmação de RSI e MACD, alto volume e divergência/continuação do CVD.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou cruzamento de indicadores.
- **Stops**: Sem stops explícitos.
- **Valores padrão**:
  - `HmaFastLength` = 20
  - `HmaSlowLength` = 50
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `VolumeMaLength` = 20
  - `VolumeMultiplier` = 1.5m
  - `CvdLength` = 14
  - `DivergenceLookback` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Divergência
  - Direção: Ambos
  - Indicadores: HMA, RSI, MACD, Volume
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
