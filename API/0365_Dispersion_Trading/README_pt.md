# Estratégia de Trading de Dispersão
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de trading de dispersão explora períodos em que um índice de renda variável e seus componentes divergem. Quando a correlação média por pares entre os membros do índice cai abaixo de um limiar, a estratégia compra as ações individuais e vende o índice a descoberto, apostando que as correlações irão reverter à média.

Velas diárias alimentam uma janela deslizante de correlação. Se as correlações se recuperarem acima do limiar, todas as posições são fechadas. Um valor mínimo de negociação é imposto para evitar ordens pequenas.

## Detalhes

- **Universo**: Um ativo de índice mais as ações que o compõem.
- **Sinal**: Abrir uma negociação de dispersão quando a correlação média dos componentes está abaixo de `CorrThreshold`.
- **Rebalanceamento**: Correlação verificada todos os dias.
- **Posicionamento**: Comprado nos componentes e vendido no índice enquanto o sinal está ativo.
- **Parâmetros**:
  - `Constituents` – lista de ativos componentes.
  - `LookbackDays` – tamanho da janela para cálculo de correlação.
  - `CorrThreshold` – nível de correlação que aciona as negociações.
  - `MinTradeUsd` – valor mínimo de ordem em USD.
  - `CandleType` – período das velas (padrão: 1 dia).
- **Nota**: O exemplo omite custos de transação e assume igual ponderação.
