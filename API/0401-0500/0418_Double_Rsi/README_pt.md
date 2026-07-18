# Estratégia Double RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Double RSI usa dois cálculos do Índice de Força Relativa: um no gráfico de trading e
outro em um período superior. As operações são realizadas apenas quando ambas as
leituras de RSI suportam a mesma direção, alinhando entradas de curto prazo com o
impulso de longo prazo.

O período principal busca o RSI cruzando para fora de zonas de sobrecompra ou
sobrevenda. Se o RSI do período superior confirmar o movimento, a estratégia abre uma
posição. Uma tomada de lucro opcional pode assegurar ganhos após um movimento
predefinido.

## Detalhes
- **Dados**: Velas de preço em dois períodos.
- **Critérios de entrada**:
  - **Comprado**: RSI do período inferior sai de sobrevenda E RSI do período superior está em alta.
  - **Vendido**: RSI do período inferior sai de sobrecompra E RSI do período superior está em baixa.
- **Critérios de saída**: Sinal RSI oposto ou tomada de lucro se `UseTP` for verdadeiro.
- **Stops**: Nenhum por padrão.
- **Valores padrão**:
  - `CandleType` = tf(5)
  - `RSILength` = 14
  - `MTFTimeframe` = tf(15)
  - `UseTP` = False
- **Filtros**:
  - Categoria: Momentum
  - Direção: Comprado/Vendido
  - Indicadores: RSI (multi‑timeframe)
  - Complexidade: Moderado
  - Nível de risco: Médio
