# Estratégia de Prêmio de Rebalanceamento Cripto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Prêmio de Rebalanceamento Cripto mantém uma carteira de igual ponderação entre Bitcoin e Ethereum. Ao rebalancear a cesta semanalmente, tenta capturar o prêmio gerado pela volatilidade entre os dois ativos.

A estratégia monitora velas horárias e realiza um rebalanceamento na primeira hora de cada segunda-feira. As negociações são ignoradas se o ajuste necessário for menor que um limite em USD definido pelo usuário.

## Detalhes

- **Universo**: Símbolos de Bitcoin e Ethereum.
- **Sinal**: Manter BTC e ETH com pesos 50/50.
- **Rebalanceamento**: Semanal, na segunda-feira às 00:00 UTC.
- **Posicionamento**: Somente comprado, igual ponderação.
- **Parâmetros**:
  - `BTC` – ativo Bitcoin.
  - `ETH` – ativo Ethereum.
  - `MinTradeUsd` – valor mínimo de negociação em USD.
  - `CandleType` – período das velas (padrão: 1 hora).
- **Nota**: A implementação é simplificada e não inclui taxas ou custos de financiamento.
