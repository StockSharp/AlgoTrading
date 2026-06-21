# Estratégia Caçadora de Reversões
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Reversal Catcher entra quando o preço ultrapassa uma Banda de Bollinger e depois retorna enquanto o momentum muda. Baseia-se em EMA rápida e lenta para definir a direção da tendência e usa cruzamentos do RSI em níveis de sobrecompra ou sobrevenda para sincronizar as entradas. Alvos e stops são derivados dos níveis das Bandas de Bollinger e do extremo do candle anterior. As posições podem ser fechadas opcionalmente em um horário de fim de dia especificado.

## Detalhes

- **Critérios de entrada**: O preço volta às Bandas de Bollinger com estrutura de topos/fundos mais altos e RSI cruzando extremos.
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Stop-loss, alvo ou encerramento no fim do dia
- **Stops**: Extremo do candle anterior
- **Valores padrão**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 1.5
  - `FastEmaPeriod` = 21
  - `SlowEmaPeriod` = 50
  - `RsiPeriod` = 14
  - `Overbought` = 70
  - `Oversold` = 30
  - `EndOfDay` = 1500
  - `CandleType` = 5 minutos
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: Bollinger Bands, EMA, RSI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
