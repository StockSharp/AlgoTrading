# Estratégia BB RSI com Stop Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina Bollinger Bands com o momentum do RSI e protege as operações com um stop trailing condicional.
Posições compradas ocorrem quando o preço perfura a banda inferior e o RSI sai da zona de sobrevenda. As vendidas são acionadas na banda superior com RSI em sobrecompra.

O stop-loss começa a uma distância fixa e se converte em stop trailing assim que o preço se move favoravelmente por um deslocamento pré-definido.

## Detalhes

- **Critérios de entrada**: Rompimento de Bollinger Band com confirmação RSI
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Stop-loss inicial ou stop trailing
- **Stops**: Sim, trailing dinâmico
- **Valores padrão**:
  - `BollingerPeriod` = 25
  - `BollingerDeviation` = 2
  - `RsiPeriod` = 14
  - `RsiOverbought` = 60
  - `RsiOversold` = 33
  - `StopLossPoints` = 50
  - `TrailOffsetPoints` = 99
  - `TrailStopPoints` = 40
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Bollinger Bands, RSI
  - Stops: Trailing
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
