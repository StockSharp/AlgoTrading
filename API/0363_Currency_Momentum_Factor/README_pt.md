# Estratégia de Fator de Momentum de Moedas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia de fator classifica as moedas pelo momentum de médio prazo e constrói uma carteira comprado/vendido. As moedas com o melhor desempenho durante a janela de lookback são compradas, enquanto as mais fracas são vendidas a descoberto em tamanhos iguais.

O momentum é avaliado usando velas diárias e o livro é rebalanceado no primeiro dia de negociação de cada mês. Ordens menores que um valor mínimo em USD são ignoradas para reduzir o ruído.

## Detalhes

- **Universo**: Lista de pares de moedas ou ETFs.
- **Sinal**: Ficar comprado nas `K` moedas com maior momentum e vendido nas `K` mais fracas.
- **Lookback**: Retorno calculado sobre `Lookback` velas diárias (padrão 252).
- **Rebalanceamento**: Mensal.
- **Posicionamento**: Comprado/Vendido, dólar-neutro.
- **Parâmetros**:
  - `Universe` – símbolos de moedas negociáveis.
  - `Lookback` – número de velas para o momentum.
  - `K` – quantidade de ativos para comprado e vendido.
  - `MinTradeUsd` – tamanho mínimo de negociação.
  - `CandleType` – período das velas (padrão: 1 dia).
- **Nota**: O exemplo não possui cálculo real de momentum para fins de demonstração.
