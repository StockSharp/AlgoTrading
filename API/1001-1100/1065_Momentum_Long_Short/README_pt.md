# Estratégia de Momentum Comprado + Vendido
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia de momentum opera tanto posições compradas quanto vendidas em um período de 3 horas. As configurações compradas exigem que o preço permaneça acima das médias móveis de 100 e 500 períodos e podem ser filtradas por RSI, ADX, ATR e alinhamento de tendência. As entradas vendidas buscam o preço romper abaixo da Banda de Bollinger inferior enquanto permanece abaixo de ambas as médias, com confirmação ATR opcional e a capacidade de bloquear vendas durante fortes tendências de alta.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: preço acima de MA100 e MA500, alinhamento de tendência opcional, RSI acima do seu valor suavizado, ADX acima do seu valor suavizado e ATR acima do seu valor suavizado.
  - **Vendido**: preço abaixo de MA100 e MA500, abaixo da Banda de Bollinger inferior, RSI abaixo do limiar, ATR acima do seu valor suavizado e bloqueio de tendência de alta opcional.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: stop-loss em `slPercentLong`% abaixo da entrada; fechamento antecipado se o preço cair abaixo de MA500.
  - **Vendido**: stop-loss e take-profit baseados nos percentuais `slPercentShort` e `tpPercentShort`.
- **Stops**: Sim.
- **Valores padrão**:
  - `slPercentLong = 3`
  - `slPercentShort = 3`
  - `tpPercentShort = 4`
  - `rsiLengthLong = 14`
  - `rsiLengthShort = 14`
  - `adxLength = 14`
  - `atrLength = 14`
  - `bbLength = 20`
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
