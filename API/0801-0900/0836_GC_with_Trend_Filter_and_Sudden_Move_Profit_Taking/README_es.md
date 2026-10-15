# Estrategia GC con Filtro de Tendencia y Toma de Ganancias en Movimientos Bruscos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia usa un cruce de SMA 5/25 con un filtro de tendencia de 75 períodos y una confirmación ADX. Las posiciones se cierran cuando el precio se mueve más de un porcentaje especificado desde el cierre anterior, capturando movimientos bruscos.

## Detalles
- **Entrada**: Largo cuando la SMA 5 cruza por encima de la SMA 25, el precio está por encima de la SMA 75 y el ADX supera el umbral. Corto en condiciones opuestas.
- **Salida**: Señal opuesta o movimiento brusco que supera el porcentaje configurado.
- **Indicadores**: SMA, Average Directional Index.
- **Mercados**: Cualquiera.
