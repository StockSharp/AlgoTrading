# Estratégia de Gastos em R&D
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia de corte transversal classifica as ações pela relação entre despesas de pesquisa e desenvolvimento (R&D) e valor de mercado. No início de cada mês, o quintil superior das empresas com maior intensidade de R&D é comprado, enquanto o quintil inferior é vendido a descoberto, apostando que os gastos intensivos em R&D preveem desempenho superior futuro.

Os pesos são atribuídos igualmente em cada lado e rebalanceados mensalmente usando dados de preço diários.

## Detalhes

- **Universo**: lista de ações com dados de R&D.
- **Sinal**: gastos em R&D divididos pela capitalização de mercado.
- **Carteira**: comprado no quintil mais alto, vendido no quintil mais baixo.
- **Rebalanceamento**: mensal.
- **Controle de risco**: negociações ignoradas quando o valor da ordem estiver abaixo de `MinTradeUsd`.
