# Estratégia de Reversão de Tendência Fibonacci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia constrói um canal Fibonacci usando máximos e mínimos recentes. Uma posição é aberta quando o preço cruza o nível de 50% na direção do rompimento. O controle de risco baseia-se em um stop loss baseado em ATR e takes profits por risco/retorno, com saída parcial opcional.

## Parâmetros
- **Candle Type** — série de candles.
- **Sensitivity** — sensibilidade base para o cálculo do canal.
- **ATR Period** — comprimento do ATR para o stop loss.
- **ATR Multiplier** — fator ATR para o stop loss.
- **Risk Reward** — múltiplo de ganho sobre o risco.
- **Use Partial TP** — fechar metade da posição no primeiro alvo.
- **Trade Direction** — direção de negociação permitida.
