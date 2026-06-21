# Estratégia de Canal Keltner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia negocia rompimentos do Canal Keltner e cruzamentos de tendência de EMA.

## Detalhes

- **Critérios de entrada**:
  - Comprado: o preço cruza abaixo da banda inferior do Keltner ou a EMA9 cruza acima da EMA21 enquanto o preço está acima da EMA50.
  - Vendido: o preço cruza acima da banda superior do Keltner ou a EMA9 cruza abaixo da EMA21 enquanto o preço está abaixo da EMA50.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - O preço cruza a banda do meio na direção oposta ou as EMAs se cruzam de volta.
  - Stop loss a 1.5 ATR.
  - Take profit a 3 ATR.
- **Stops**: Sim.
- **Valores padrão**:
  - `Length` = 20
  - `Multiplier` = 1.5
  - `AtrMultiplier` = 1.5
  - `FastEmaPeriod` = 9
  - `SlowEmaPeriod` = 21
  - `TrendEmaPeriod` = 50
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Canal
  - Direção: Ambos
  - Indicadores: EMA, ATR, Keltner
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
