# Estratégia Modular de Operações em Faixa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia visa mercados em faixa usando dois módulos que não podem estar ativos ao mesmo tempo. O primeiro módulo baseia-se na confirmação de momentum do MACD com RSI e reversão à média das Bandas de Bollinger. O segundo módulo compra ou vende em extremos quando o preço rebate de volta para dentro das Bandas de Bollinger com níveis de RSI sobrevendido ou sobrecomprado. Stops baseados em ATR e saídas opcionais via Bandas de Bollinger ou reversões do RSI gerenciam o risco.

## Detalhes

- **Critérios de entrada**:
  - **Lógica 1 Comprado**: ADX abaixo do limiar, MACD cruza acima da linha de sinal, RSI acima da sua SMA, preço abaixo da banda média de Bollinger.
  - **Lógica 1 Vendido**: ADX abaixo do limiar, MACD cruza abaixo da linha de sinal, RSI abaixo da sua SMA, preço acima da banda média de Bollinger.
  - **Lógica 2 Comprado**: ADX abaixo do limiar, preço cruza de volta acima da banda inferior, RSI abaixo do nível de sobrevenda.
  - **Lógica 2 Vendido**: ADX abaixo do limiar, preço cruza de volta abaixo da banda superior, RSI acima do nível de sobrecompra.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**:
  - Stop loss por ATR.
  - Sinais opcionais de Bollinger ou RSI dependendo da lógica ativa.
- **Stops**: Múltiplos de ATR.
- **Valores padrão**: Bollinger 20/2, RSI 14, MACD 12/26/9, ATR 14, ADX 14.
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Sim
  - Complexidade: Complexo
  - Período: Médio prazo
