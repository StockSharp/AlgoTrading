# Estratégia de Tendência EMA com Entrada Heikin Ashi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que usa Bandas de Bollinger em candles Heikin Ashi com um filtro de tendência EMA de período superior. Compra após candles Heikin Ashi baixistas consecutivos tocando a banda inferior seguidos de um candle altista acima quando a EMA rápida do período superior está acima da EMA lenta. Vende ao contrário.

Após entrar, um primeiro alvo igual ao risco é atingido e o stop é ajustado usando os extremos do candle anterior.

## Detalhes

- **Critérios de entrada**:
  - Comprado: pelo menos dois candles HA baixistas tocando a banda inferior, depois altista acima com EMA rápida do período superior acima da EMA lenta
  - Vendido: pelo menos dois candles HA altistas tocando a banda superior, depois baixista abaixo com EMA rápida do período superior abaixo da EMA lenta
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Comprado: primeiro alvo em 1R, depois trailing stop nas mínimas anteriores
  - Vendido: primeiro alvo em 1R, depois trailing stop nas máximas anteriores
- **Stops**: Mínima/máxima do candle anterior
- **Valores padrão**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2m
  - `FastEmaPeriod` = 9
  - `SlowEmaPeriod` = 21
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `HigherTimeframe` = TimeSpan.FromMinutes(180).TimeFrame()
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: Bollinger Bands, Heikin Ashi, EMA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
