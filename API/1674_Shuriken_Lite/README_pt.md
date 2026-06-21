# Estratégia Shuriken Lite
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica a funcionalidade da ferramenta MQL original *Shuriken Lite*. Rastreia as operações executadas na conta e as agrupa por identificadores numéricos conhecidos como **magic numbers**. Para cada grupo a estratégia calcula:

- Número de operações
- Operações vencedoras e perdedoras
- Lucro ou perda total em pips
- Fator de lucro

As estatísticas são registradas após cada nova operação quando a exibição de pontuação está habilitada.

## Parâmetros

- **Magic Numbers** — lista separada por vírgulas de identificadores usados para agrupar operações. Cada identificador deve corresponder ao valor numérico colocado no comentário da ordem.
- **Show Scores** — habilitar ou desabilitar o registro de estatísticas.

## Uso

1. Defina os magic numbers desejados no parâmetro.
2. Execute a estratégia junto com outras estratégias que inserem comentários numéricos nas suas ordens.
3. Verifique o log para as métricas de desempenho agregadas.
