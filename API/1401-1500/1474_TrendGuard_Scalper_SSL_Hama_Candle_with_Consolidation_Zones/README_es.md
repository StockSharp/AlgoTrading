# Estrategia TrendGuard Scalper SSL + Hama Candle con Zonas de Consolidación
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina un canal SSL simple con la dirección de las velas Hama. Se abre una posición larga cuando el cierre está por encima de la media SSL, el cierre Hama (EMA 20) está por encima de la línea Hama larga (EMA 100) y el precio permanece por encima del cierre Hama. Las operaciones cortas utilizan las condiciones opuestas. El ATR se usa para marcar períodos de baja volatilidad como posibles zonas de consolidación.

## Detalles
- **Entrada**: los tendencias SSL y Hama coinciden con confirmación del precio.
- **Salida**: porcentajes fijos de take‑profit y stop‑loss.
- **Indicadores**: SMA, EMA, ATR.
- **Filtros**: detección de consolidación solo para análisis.
