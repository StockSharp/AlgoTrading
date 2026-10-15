# Estrategia Martingala con Condiciones de Apertura MACD y KDJ
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra en operaciones cuando tanto la línea MACD como la línea %K del KDJ cruzan sus líneas de señal en la misma dirección. Piramida posiciones usando un enfoque martingala, añadiendo cuando el precio se mueve contra la operación un porcentaje configurado y luego rebota.

Las posiciones se cierran cuando se cumple una condición de take profit, stop loss o trailing stop.

## Detalles

- **Entrada**: La línea MACD y la línea %K del KDJ cruzan sus líneas de señal en la misma dirección.
- **Adiciones**: Hasta `Max Additions` veces cuando el precio se mueve `Add Position Percent` y rebota `Rebound Percent`. El tamaño de cada adición se multiplica por `Add Multiplier`.
- **Salida**: Cerrar en `Take Profit Trigger`, `Stop Loss Percent` o al activarse el trailing stop.
- **Dirección**: Largo y corto.

