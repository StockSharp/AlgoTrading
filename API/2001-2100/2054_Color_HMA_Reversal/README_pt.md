# Estratégia de Reversão Color HMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada em mudanças de inclinação da Hull Moving Average. Ela fecha posições contra a nova direção e abre posições a favor da tendência quando o HMA reverte.

## Parâmetros
- `HmaPeriod` — período para a Hull Moving Average.
- `CandleType` — tipo de velas a utilizar.
- `BuyOpen`, `SellOpen` — permitir abertura de posições compradas/vendidas.
- `BuyClose`, `SellClose` — permitir fechamento de posições compradas/vendidas.

## Sinais
- **Reversão ascendente**: o HMA anterior estava caindo e o valor atual sobe → fechar vendidos e abrir comprado.
- **Reversão descendente**: o HMA anterior estava subindo e o valor atual cai → fechar comprados e abrir vendido.

A estratégia usa ordens a mercado e opera com o volume especificado em `Strategy.Volume`.
