# Estratégia do Ciclo de 12 Meses
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia em Python implementa a anomalia do ciclo de 12 meses. As ações são classificadas pelo retorno obtido um ano atrás no mês calendário correspondente. A cada mês o decil superior é comprado e o decil inferior é vendido a descoberto, criando uma carteira neutra ao mercado baseada no desempenho anual defasado.

O sistema usa dados diários para aproximar os fechamentos mensais e faz o rebalanceamento no início de cada mês. Os tamanhos das posições são escalados para manter a exposição em dólares equilibrada entre os lados comprado e vendido.

## Detalhes

- **Universo**: lista de ativos definida pelo usuário.
- **Sinal**: ordenar pela variação percentual em relação ao mesmo mês do ano anterior.
- **Carteira**: comprado no decil superior, vendido no decil inferior com alavancagem por tranche definida por `Leverage`.
- **Rebalanceamento**: mensal.
- **Dados**: velas diárias agregadas em preços de fechamento mensal.
