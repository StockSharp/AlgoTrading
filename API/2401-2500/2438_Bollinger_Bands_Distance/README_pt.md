# Estratégia de Distância de Bollinger Bands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que opera reversões de Bollinger Bands com um filtro de distância adicional. Vende quando o preço fecha acima da banda superior mais uma distância definida e compra quando fecha abaixo da banda inferior menos a mesma distância. As posições são fechadas por objetivo de lucro ou stop loss medidos em passos de preço.

## Detalhes

- **Critérios de entrada**:
  - Comprado: fecho abaixo da banda inferior de Bollinger menos distância
  - Vendido: fecho acima da banda superior de Bollinger mais distância
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Objetivo de lucro atingido
  - Stop loss atingido
- **Stops**: Absolutos em passos de preço
- **Valores padrão**:
  - `BollingerPeriod` = 4
  - `BollingerDeviation` = 2m
  - `BandDistance` = 3m
  - `ProfitTarget` = 3m
  - `LossLimit` = 20m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: Bollinger Bands
  - Stops: Sim
  - Complexidade: Básico
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
