# Estratégia 2pbIdeal XOSMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma tradução em C# do consultor especialista MQL5 **Exp_2pbIdealXOSMA**. Ela analisa a inclinação do histograma MACD para determinar o momentum do mercado. Quando o histograma sobe durante duas barras consecutivas, o sistema entra numa posição comprada e fecha qualquer posição vendida aberta. Quando o histograma cai durante duas barras consecutivas, a estratégia entra numa posição vendida e fecha qualquer posição comprada aberta.

Por padrão, o algoritmo opera em velas de 4 horas, mas o período é configurável. Todas as operações são executadas a preço de mercado e a posição é invertida quando o sinal contrário aparece. Nenhum stop-loss ou take-profit é aplicado no exemplo; o controle de risco pode ser adicionado externamente, se desejado.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O histograma na barra `t-1` está abaixo de `t-2` e o histograma atual supera `t-1`.
  - **Vendido**: O histograma na barra `t-1` está acima de `t-2` e o histograma atual está abaixo de `t-1`.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: O sinal oposto fecha a posição atual.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `FastPeriod` = 10
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `SignalBar` = 1
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Único (MACD)
  - Stops: Não
  - Complexidade: Simples
  - Período: 4 horas (configurável)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
