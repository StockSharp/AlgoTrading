# Estratégia Zakryvator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Zakryvator é um módulo de gerenciamento de risco que monitora a posição aberta atual e a fecha quando a perda não realizada excede um limiar predefinido. A perda permitida depende do volume da posição, replicando a lógica do script MQL original onde diferentes tamanhos de lote correspondem a diferentes drawdowns máximos.

Esta estratégia não gera entradas por si mesma. Espera-se que as posições sejam abertas manualmente ou por outra estratégia. Zakryvator simplesmente protege a conta saindo automaticamente de operações com prejuízo.

## Detalhes

- **Critérios de entrada**: Nenhum. A estratégia apenas gerencia posições existentes.
- **Critérios de saída**: Fecha a posição atual quando a perda atinge o limiar configurado para seu volume.
- **Comprado/Vendido**: Ambas as direções são suportadas.
- **Stops**: Utiliza limites de perda monetária fixos que variam com o tamanho da posição.
- **Filtros**: Sem filtros adicionais.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `Min001002` | Perda máxima para posições com volume ≤ 0.02 lotes. |
| `Min002005` | Perda máxima para posições com volume entre 0.02 e 0.05 lotes. |
| `Min00501` | Perda máxima para posições com volume entre 0.05 e 0.10 lotes. |
| `Min0103` | Perda máxima para posições com volume entre 0.10 e 0.30 lotes. |
| `Min0305` | Perda máxima para posições com volume entre 0.30 e 0.50 lotes. |
| `Min051` | Perda máxima para posições com volume entre 0.50 e 1 lote. |
| `MinFrom1` | Perda máxima para posições com volume maior que 1 lote. |

## Comportamento

1. A estratégia subscreve ticks de operações para rastrear preços em tempo real.
2. Em cada tick calcula o PnL não realizado usando o preço atual e o preço médio de entrada.
3. Se a perda exceder o limiar correspondente ao volume da posição atual, a posição é fechada a mercado.

Isso torna o Zakryvator uma ferramenta simples, mas eficaz, para limitar drawdowns com base no tamanho da operação.
