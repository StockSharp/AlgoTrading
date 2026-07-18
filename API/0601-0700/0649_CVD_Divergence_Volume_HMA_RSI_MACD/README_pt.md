# Estratégia CVD Divergência Volume HMA RSI MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina Hull Moving Averages, RSI, MACD, filtro de volume e divergência do delta de volume acumulado (CVD) para identificar oportunidades de tendência.

Posições compradas são abertas quando HMA20 está acima de HMA50, o RSI mostra momentum altista, o histograma MACD sobe, o volume supera sua média e o CVD forma divergência altista ou aumenta. Posições vendidas espelham essas condições de forma inversa.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: HMA20 > HMA50 e preço > HMA20; RSI entre 40 e `RsiOverbought`; linha MACD acima do sinal e histograma subindo; volume > SMA * `VolumeMultiplier`; divergência CVD altista ou CVD crescente.
  - **Vendido**: HMA20 < HMA50 e preço < HMA20; RSI entre `RsiOversold` e 60; linha MACD abaixo do sinal e histograma caindo; volume > SMA * `VolumeMultiplier`; divergência CVD baixista ou CVD decrescente.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Preço < HMA20 ou RSI > `RsiOverbought` ou linha MACD cruza abaixo do sinal.
  - **Vendido**: Preço > HMA20 ou RSI < `RsiOversold` ou linha MACD cruza acima do sinal.
- **Stops**: Não.
- **Valores padrão**:
  - `Hma20Length` = 20
  - `Hma50Length` = 50
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `VolumeMaLength` = 20
  - `VolumeMultiplier` = 1.5
  - `CvdLength` = 14
  - `DivergenceLookback` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Misto
  - Direção: Ambos
  - Indicadores: HMA, RSI, MACD, Volume, CVD
  - Stops: Não
  - Complexidade: Avançado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
