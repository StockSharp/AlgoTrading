# Estratégia de Negociação em Pares de ETFs de Países
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia de reversão à média negocia um par de ETFs de países com base no z-score da relação de preços deles. Quando a relação se desvia além de um limiar, o sistema entra numa posição comprado/vendido esperando que o spread reverta para sua média.

A relação de preços é rastreada com uma janela deslizante e as posições são fechadas quando o z-score cruza o nível de saída.

## Detalhes

- **Universo**: exatamente dois ETFs de países.
- **Sinal**: z-score da relação de preços deslizante excedendo `EntryZ`.
- **Saída**: fechar quando o z-score reverter para `ExitZ`.
- **Dados**: velas diárias, janela de 60 dias por padrão.
- **Controle de risco**: ordens ignoradas se o valor da negociação estiver abaixo de `MinTradeUsd`.
