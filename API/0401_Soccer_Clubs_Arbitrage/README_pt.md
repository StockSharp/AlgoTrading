# Estratégia de Arbitragem de Clubes de Futebol
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia busca oportunidades de arbitragem entre fan tokens de clubes de futebol negociados em múltiplos venues. Monitorando spreads de preços e desequilíbrios nas taxas de financiamento, abre posições compradas e vendidas compensatórias para capturar ineficiências de precificação.

Uma operação é acionada quando o spread entre as exchanges supera um limite. As posições são protegidas e encerradas quando os preços convergem ou um stop protetor é atingido.

## Detalhes

- **Dados**: Preços de fan tokens e taxas de financiamento.
- **Entrada**: Abrir posições opostas quando o spread > X%.
- **Saída**: Fechar quando o spread < Y% ou no stop por tempo.
- **Instrumentos**: Fan tokens listados em exchanges.
- **Risco**: Stop de percentual fixo para proteção contra slippage.

