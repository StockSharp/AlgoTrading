# Estratégia do Modelo Fed
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este sistema macroeconômico de timing compara o rendimento de lucros do mercado acionário com o rendimento dos títulos do Tesouro de 10 anos. Quando as ações oferecem maior rendimento, a estratégia mantém um ETF de ações; quando os títulos rendem mais, migra para caixa. Uma regressão mensal sobre o diferencial de rendimentos prevê o valor do próximo mês para reduzir trocas ruidosas.

No final de cada mês, o algoritmo prevê o diferencial de rendimento do mês seguinte usando o último ano de dados. Se a previsão for positiva, compra ações; caso contrário, mantém o proxy de caixa. As posições mudam apenas quando a previsão cruza zero, minimizando a rotatividade.

## Detalhes

- **Critérios de entrada**:
  - No final do mês, realizar uma regressão sobre as últimas `RegressionMonths` observações de `(EarningsYield - BondYield)` e prever o próximo valor.
  - Comprar o ETF de ações quando a previsão estiver acima de zero e a ordem atender a `MinTradeUsd`.
- **Comprado/Vendido**: Somente comprado em ações ou caixa.
- **Critérios de saída**: Encerrar a posição em ações quando o diferencial de rendimento previsto ficar negativo.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Universe` – [ETF de ações, ETF de caixa opcional].
  - `BondYieldSym` – série de rendimentos do Tesouro de 10 anos.
  - `EarningsYieldSym` – rendimento de lucros do mercado acionário.
  - `RegressionMonths` = 12.
  - `CandleType` = 1 dia.
  - `MinTradeUsd` – valor mínimo de negociação.
- **Filtros**:
  - Categoria: Macro.
  - Direção: Somente comprado.
  - Período: Mensal.
  - Rebalanceamento: Mensal.

