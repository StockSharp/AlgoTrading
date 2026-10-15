# Estratégia de Seguidor de Tendência em Ações
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia opera ações individuais usando um filtro de tendência simples. Ações negociadas acima de uma média móvel são compradas, enquanto as que estão abaixo são evitadas ou vendidas a descoberto.

A carteira é atualizada semanalmente com tamanho de posição igual e stops de rastreamento para proteger o capital.

## Detalhes

- **Dados**: Fechamentos diários de ações.
- **Entrada**: Comprar quando o preço > média móvel; vendido quando abaixo.
- **Saída**: O preço cruza de volta a média ou o stop é atingido.
- **Instrumentos**: Ações líquidas.
- **Risco**: Trailing stop e limite de posição.

