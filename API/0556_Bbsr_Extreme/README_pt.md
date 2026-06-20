# Bbsr Extreme
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Bbsr Extreme** combina rompimentos de Bollinger Bands com um filtro de tendência baseado em uma média móvel.
Uma posição comprada aparece quando o preço recua da banda inferior enquanto a média está subindo.
Uma posição vendida é aberta em um recuo da banda superior quando a média cai.
As saídas dependem de stop-loss e take profit calculados por ATR.

## Detalhes
- **Critérios de entrada**: O preço cruza as bandas com confirmação de tendência.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop ATR ou take profit.
- **Stops**: Sim, baseados em ATR.
- **Valores padrão**:
  - `BollingerPeriod = 20`
  - `BollingerMultiplier = 2`
  - `MaLength = 7`
  - `AtrLength = 14`
  - `AtrStopMultiplier = 2`
  - `AtrProfitMultiplier = 3`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Bollinger Bands, EMA, ATR
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
