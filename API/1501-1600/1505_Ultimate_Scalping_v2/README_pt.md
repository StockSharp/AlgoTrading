# Estratégia Ultimate Scalping v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sistema de scalping que combina EMAs rápidas e lentas com VWAP. Filtros opcionais de candle engolidor e pico de volume refinam as entradas. As posições usam stops baseados em ATR e podem ser encerradas em sinais opostos.

## Detalhes

- **Comprado**: EMA rápida cruza acima da EMA lenta e o preço está acima do VWAP.
- **Vendido**: EMA rápida cruza abaixo da EMA lenta e o preço está abaixo do VWAP.
- **Indicadores**: EMA, VWAP, ATR, SMA.
- **Stops**: múltiplos de ATR.
