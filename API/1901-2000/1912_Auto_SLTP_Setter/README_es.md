# Estrategia de Configuración Automática de SLTP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia utilitaria que adjunta automáticamente órdenes de stop-loss y take-profit a posiciones abiertas cuando faltan. Las distancias pueden definirse como valores fijos en pips o como múltiplos del Average True Range (ATR).

## Parámetros

- `Candle Type` – marco temporal utilizado para el cálculo del ATR.
- `Set Stop Loss` – habilitar la colocación automática del stop-loss.
- `Set Take Profit` – habilitar la colocación automática del take-profit.
- `Stop Loss Method` – 1 = pips fijos, 2 = múltiplo de ATR.
- `Fixed SL (pips)` – distancia del stop-loss en pips para el método fijo.
- `SL ATR Multiplier` – multiplicador de ATR para el stop-loss al usar el método ATR.
- `Take Profit Method` – 1 = pips fijos, 2 = múltiplo de ATR.
- `Fixed TP (pips)` – distancia del take-profit en pips para el método fijo.
- `TP ATR Multiplier` – multiplicador de ATR para el take-profit al usar el método ATR.
- `ATR Period` – número de períodos utilizados para el cálculo del ATR.

## Cómo funciona

1. Al iniciar, la estrategia evalúa la configuración.
2. Si se solicitan valores basados en ATR, se suscribe a la serie de velas especificada y calcula el ATR.
3. Una vez que el valor ATR está disponible, la estrategia llama a `StartProtection` con las distancias calculadas.
4. `StartProtection` coloca órdenes de protección para cualquier posición existente y para las operaciones futuras abiertas por la estrategia.

La estrategia no genera señales de trading; solo gestiona el riesgo asegurando que cada posición tenga niveles apropiados de stop-loss y take-profit.
