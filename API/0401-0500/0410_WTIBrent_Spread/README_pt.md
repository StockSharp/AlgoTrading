# Estratégia de Spread WTI-Brent
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A operação tem como alvo o diferencial de preço entre o petróleo WTI e o Brent. Quando o spread desvia das normas históricas, o sistema aposta na reversão à média comprando um tipo e vendendo o outro.

As posições são roladas com os futuros do mês próximo e encerradas quando o spread converge.

## Detalhes

- **Dados**: Preços dos futuros do mês próximo de WTI e Brent.
- **Entrada**: Comprado no tipo mais barato e vendido no mais caro quando o spread > limite.
- **Saída**: Fechar quando o spread retorna à média ou no roll do contrato.
- **Instrumentos**: Futuros de petróleo bruto.
- **Risco**: Neutralidade em dólares com stop na ampliação do spread.

