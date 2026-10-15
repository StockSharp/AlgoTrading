# Estratégia de Volume de Compra e Venda
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza a distribuição do volume de compra e venda para detectar pressão.
Uma posição comprada é aberta quando o volume de compra domina e a métrica de volume
rompe acima de uma banda de volatilidade enquanto o preço está acima do VWAP semanal. Uma posição vendida
usa as condições opostas.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Volume de compra ajustado > volume de venda ajustado, métrica de volume acima da banda superior, close acima do VWAP semanal.
  - **Vendido**: Volume de venda ajustado > volume de compra ajustado, métrica de volume acima da banda superior, close abaixo do VWAP semanal.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto ou take profit/stop loss baseado em ATR.
- **Stops**: Multiplicadores de percentual ATR via `ProfitTargetLong`, `StopLossLong`, `ProfitTargetShort`, `StopLossShort`.
- **Valores padrão**:
  - Length 20, StdDev 2.
  - ProfitTargetLong 100, StopLossLong 1.
  - ProfitTargetShort 100, StopLossShort 5.
- **Filtros**:
  - Categoria: Baseado em volume
  - Direção: Ambos
  - Indicadores: Personalizado
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Médio prazo
