# Estrategia Crossover 2 EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia replica el asesor experto de MetaTrader "Crossover_2EMA" operando la relación entre una media móvil exponencial (EMA) rápida y una lenta calculadas sobre precios de cierre. Cuando la EMA rápida sube por encima de la lenta, el algoritmo entra largo. Cuando vuelve a caer por debajo, revierte a una posición corta. El enfoque mantiene siempre la posición alineada con el estado actual de tendencia rápida/lenta y, por tanto, funciona como un sistema totalmente reversible.

## Lógica de negociación
1. Suscribirse a la serie de velas configurada y calcular dos EMA con periodos definidos por el usuario.
2. Seguir el spread entre los valores de EMA rápida y lenta en cada vela terminada.
3. Detectar un cruce alcista cuando el spread pasa de no positivo a positivo. Cerrar cualquier exposición corta y abrir una posición larga con el volumen configurado.
4. Detectar un cruce bajista cuando el spread pasa de no negativo a negativo. Cerrar cualquier exposición larga y abrir una posición corta con el volumen configurado.
5. Las órdenes se emiten a mercado para asegurar reacción inmediata al cruce. El volumen se aumenta automáticamente al revertir para aplanar la posición existente antes de abrir una nueva.

## Gestión de riesgo
- La estrategia invoca `StartProtection()` al iniciarse para que puedan configurarse los mecanismos protectores estándar de StockSharp (por ejemplo, protección contra drawdown, límites de horario o circuit breakers).
- Las reversiones de posición usan una única orden de mercado combinada, reduciendo la latencia frente a una salida y reentrada secuenciales.

## Parámetros
- **Candle Type:** serie de datos usada para los cálculos EMA.
- **Fast EMA Period:** periodo de la EMA rápida. Debe ser menor que el periodo de la EMA lenta.
- **Slow EMA Period:** periodo de la EMA lenta. Debe ser mayor que el periodo de la EMA rápida.

## Notas adicionales
- Ambas EMA deben estar completamente formadas antes de iniciar operaciones, evitando señales prematuras.
- La configuración predeterminada usa EMA de 12/24 periodos sobre velas de un minuto, replicando el experto MQL original.
- Los parámetros están marcados como optimizables, permitiendo optimización por lotes en StockSharp.
