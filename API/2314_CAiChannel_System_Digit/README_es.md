# Estrategia CaiChannel Sistema Dígito
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port simplificado a StockSharp del experto de MetaTrader **i-CAiChannel System Digit**.

El algoritmo monitorea un canal de volatilidad construido a partir de una media móvil y desviación estándar (Bandas de Bollinger).
Cuando una vela cierra fuera del canal y la siguiente vela regresa al interior, la estrategia opera en la dirección del re-ingreso.

## Parámetros
- `Length` – período de la media móvil.
- `Width` – multiplicador de desviación estándar.
- `Candle Type` – marco temporal de procesamiento.

## Lógica de operación
1. Suscribirse a las velas del marco temporal seleccionado.
2. Calcular las Bandas de Bollinger con los parámetros especificados.
3. Si la vela anterior cerró por encima de la banda superior y la vela actual cierra de regreso al interior, ir largo.
4. Si la vela anterior cerró por debajo de la banda inferior y la vela actual cierra de regreso al interior, ir corto.
5. La posición se invierte cuando ocurre la señal opuesta.

Todas las señales se generan únicamente en velas terminadas.
