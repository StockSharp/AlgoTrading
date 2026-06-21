# Estratégia Supertrend - SSL com Alternância
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina o indicador Supertrend com o canal SSL.
Uma alternância permite exigir confirmação de ambos os indicadores antes de entrar em uma operação.
Se a confirmação estiver ativada, o sinal do primeiro indicador aguarda o segundo antes de ser executado.
As posições são fechadas quando um sinal oposto aparece de qualquer um dos indicadores.

## Detalhes

- **Indicadores**: Supertrend (ATR 10, fator 2.4), canal SSL (período 13)
- **Entrada**: Cruzamento SSL ou mudança de direção do Supertrend com confirmação opcional
- **Saída**: Sinal oposto do SSL ou Supertrend
- **Direção**: Comprado e Vendido
- **Período**: Qualquer
