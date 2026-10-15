# JFATL Sistema Digital
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia construida alrededor de la pendiente de la media móvil Jurik (JFATL). Abre posiciones largas cuando la media móvil gira hacia arriba y posiciones cortas cuando gira hacia abajo. La idea imita el sistema digital codificado por colores de la versión MQL original.

## Detalles
- **Criterios de entrada**: La pendiente de la media móvil Jurik cambia de signo. La pendiente ascendente abre una posición larga, la pendiente descendente abre una posición corta.
- **Largo/Corto**: Se operan ambas direcciones.
- **Criterios de salida**: La posición se revierte en la pendiente opuesta o se cierra por gestión de riesgo.
- **Stops**: Take profit basado en porcentaje y stop loss opcional configurado mediante `StartProtection`.
- **Valores predeterminados**: Length = 5, Phase = -100, Timeframe = 4 hours.
- **Filtros**: Ninguno. La estrategia se basa únicamente en la pendiente JMA.
