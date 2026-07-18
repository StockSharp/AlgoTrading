# Bot de Trailing TP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera cruces de SMA tanto en posiciones largas como cortas. Cada posición define un take profit y stop loss fijos. Después de alcanzar el objetivo de ganancia, el stop puede seguir al precio para proteger las ganancias.

## Detalles

- **Entrada**: La SMA rápida cruza la SMA lenta.
- **Salida**: Stop loss, take profit o trailing stop.
- **Indicadores**: SMA.
- **Dirección**: Ambos.
- **Riesgo**: Stop loss fijo con trailing opcional.
