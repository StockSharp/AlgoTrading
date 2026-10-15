# Estratégia de Negociação em Pares de Ações
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia simplificada de negociação em pares opera em múltiplos pares de ações. Para cada par, a relação de preços é rastreada em uma janela deslizante e seu z-score é calculado. Quando o z-score ultrapassa um limiar de entrada, uma negociação comprado/vendido é aberta; as posições são fechadas quando o z-score reverte.

O algoritmo suporta a negociação de múltiplos pares independentes simultaneamente.

## Detalhes

- **Universo**: lista de pares de ações.
- **Sinal**: z-score da relação de preços cruzando `EntryZ`.
- **Saída**: fechar quando o z-score atingir `ExitZ`.
- **Dados**: velas diárias com retrospectiva de 60 dias por padrão.
- **Controle de risco**: negociações ignoradas quando o valor da ordem estiver abaixo de `MinTradeUsd`.
