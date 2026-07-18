# Estratégia Nova Futures PRO SAFE v6
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina sinais de tendência, volatilidade e estrutura. Usa uma EMA de 200 com ADX para confirmar tendências, Bollinger Bands versus Keltner Channels para detectar rompimentos de compressão, e níveis de Donchian para quebra de estrutura em máximos ou mínimos. Filtros opcionais de período superior e um índice de agitação evitam operar em regimes de baixa qualidade. Um período de resfriamento previne a reentrada imediata após o fechamento de uma posição.

## Entradas
- **EMA Length** — comprimento da média móvel exponencial base
- **DMI Length** — período para ADX e movimento direcional
- **Min ADX** — valor mínimo de ADX para considerar tendência
- **BB Length** — período de Bollinger Bands
- **BB Mult** — multiplicador de Bollinger Bands
- **KC Length** — período de Keltner Channels
- **KC Mult** — multiplicador de Keltner Channels
- **Donchian Length** — lookback para níveis de estrutura
- **Use HTF** — habilitar confirmação de período superior
- **HTF Candle** — período superior para filtros
- **HTF EMA** — comprimento de EMA no período superior
- **HTF Min ADX** — ADX mínimo no período superior
- **Use Choppiness** — habilitar filtro de agitação
- **Chop Length** — período do índice de agitação
- **Chop Threshold** — agitação máxima permitida
- **Cooldown** — velas a aguardar após uma saída
- **Candle Type** — período principal de velas

## Notas
Port simplificado do script do TradingView "Nova Futures PRO (SAFE v6) — HTF + Choppiness + Cooldown".
