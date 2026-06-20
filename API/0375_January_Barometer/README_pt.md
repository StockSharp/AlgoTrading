# Estratégia do Barômetro de Janeiro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Barômetro de Janeiro afirma que o desempenho do mercado em janeiro define o tom para o restante do ano. Esta estratégia investe em um ETF de ações pelo resto do ano apenas se janeiro fechar em alta; caso contrário, permanece em um proxy de caixa. A decisão de alocação é tomada uma vez por ano e mantida até o final do ano.

No primeiro dia útil de fevereiro, o algoritmo mede o retorno total do ETF de ações durante janeiro. Se o retorno for positivo e o valor da ordem superar o limite mínimo, compra o ETF de ações e o mantém até dezembro. Se janeiro foi negativo, mantém o ETF de caixa no lugar. O processo se repete a cada ano.

## Detalhes

- **Critérios de entrada**:
  - No primeiro dia útil de fevereiro, calcular o retorno total de janeiro do `EquityETF`.
  - Comprar `EquityETF` se o retorno for positivo e o tamanho da ordem >= `MinTradeUsd`; caso contrário, manter `CashETF`.
- **Comprado/Vendido**: Somente comprado em ações ou caixa.
- **Critérios de saída**: Encerrar a posição em ações no último dia útil do ano.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `EquityETF` – ETF representando o mercado acionário.
  - `CashETF` – ETF proxy de caixa.
  - `CandleType` = 1 dia.
  - `MinTradeUsd` – valor mínimo de negociação.
- **Filtros**:
  - Categoria: Sazonal.
  - Direção: Somente comprado.
  - Período: Longo prazo.
  - Rebalanceamento: Anual.

