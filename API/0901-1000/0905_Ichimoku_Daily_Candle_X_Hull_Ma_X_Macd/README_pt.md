# Estratégia Ichimoku Daily Candle X Hull MA X MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina as linhas avançadas do Ichimoku, a direção da vela diária, a tendência da Hull Moving Average e um MACD baseado em HMA. Posições compradas são abertas quando todos os componentes estão alinhados em alta; vendidas ocorrem quando todas as condições ficam baixistas.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: HMA em ascensão, preço atual acima do HMA anterior, vela diária atual maior que a anterior, SenkouA > SenkouB, linha MACD > sinal.
  - **Vendido**: HMA em declínio, preço abaixo do HMA anterior, vela diária atual menor que a anterior, SenkouA < SenkouB, linha MACD < sinal.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `HmaPeriod` = 14
  - `ConversionPeriod` = 9
  - `BasePeriod` = 26
  - `SpanPeriod` = 52
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `PriceSource` = Open
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Ichimoku, Hull MA, MACD
  - Stops: Nenhum
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
