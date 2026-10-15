# Estratégia de Fator de Valor por País
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Fator de Valor por País classifica os mercados de renda variável pelo índice CAPE de Shiller. Os países com o CAPE mais baixo são considerados baratos e comprados, enquanto os mercados caros são evitados. A abordagem explora a tendência dos mercados subavaliados de superarem os demais ao longo do tempo.

A cada mês a estratégia redistribui o capital igualmente entre os países mais baratos do universo fornecido pelo usuário. As posições são dimensionadas pelo valor da carteira e apenas executadas quando a negociação ultrapassa um valor mínimo em USD.

## Detalhes

- **Universo**: Coleção de ETFs de renda variável por país.
- **Sinal**: Comprar os países com os menores índices CAPE.
- **Rebalanceamento**: Primeiro dia de negociação de cada mês.
- **Posicionamento**: Somente comprado.
- **Parâmetros**:
  - `Universe` – ativos representando cada país.
  - `MinTradeUsd` – valor mínimo em dólares por ordem.
  - `CandleType` – período das velas (padrão: 1 dia).
- **Nota**: O código de exemplo contém lógica de marcador de posição e deve ser expandido com cálculos de fatores reais.
