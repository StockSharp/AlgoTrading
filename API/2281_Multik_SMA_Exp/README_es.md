# Estrategia Multik SMA Exp
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia implementa un enfoque contrario basado en la pendiente de una media móvil simple (SMA). Fue portada del asesor experto de MetaTrader 5 "Multik_SMA_Exp".

La estrategia monitorea los últimos tres valores de la SMA. Si la SMA ha estado cayendo durante los dos segmentos completados más recientes, la estrategia entra en una posición larga. Si la SMA ha estado subiendo durante los dos segmentos, abre una posición corta. Las posiciones se cierran cuando la pendiente de la SMA se revierte.

## Parámetros
- **MA Period** – longitud de la media móvil simple. Predeterminado: 50.
- **Candle Type** – tipo de velas usadas para los cálculos. Predeterminado: marco temporal de 1 minuto.

## Reglas de trading
1. En cada vela cerrada, calcular la SMA.
2. Determinar las pendientes:
   - `dsma1 = SMA[n-1] - SMA[n-2]`
   - `dsma2 = SMA[n-2] - SMA[n-3]`
3. Entrada:
   - Si `dsma1 < 0` y `dsma2 < 0` y no hay posición larga, comprar.
   - Si `dsma1 > 0` y `dsma2 > 0` y no hay posición corta, vender.
4. Salida:
   - Si se mantiene una posición larga y `dsma1 > 0`, cerrar la posición larga.
   - Si se mantiene una posición corta y `dsma1 < 0`, cerrar la posición corta.

El volumen de las nuevas órdenes usa el `Volume` de la estrategia más el valor absoluto de la posición actual para revertirla completamente cuando sea necesario.
