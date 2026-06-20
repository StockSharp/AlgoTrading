# Estratégia de Prêmio de Risco de Volatilidade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia vende opções para capturar o prêmio de risco de volatilidade, esperando que a volatilidade implícita supere a realizada em média. As posições são delta-cobertas para isolar o prêmio.

A exposição short em opções é gerenciada com controles de risco rígidos e recobertura periódica.

## Detalhes

- **Dados**: Volatilidade implícita de opções e volatilidade realizada.
- **Entrada**: Vender opções fora do dinheiro quando implícita > realizada.
- **Saída**: Recomprar no vencimento ou quando a volatilidade disparar.
- **Instrumentos**: Opções sobre índices ou FX.
- **Risco**: Hedge delta e stop-loss no vega.

