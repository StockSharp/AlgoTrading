# Estrategia de Ciclo de Tendencia Color Schaff RVI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa el Color Schaff RVI Trend Cycle usando la API de alto nivel de StockSharp. El indicador aplica un proceso de doble estocástico a la diferencia entre los valores del Índice de Vigor Relativo rápido y lento y suaviza el resultado.

## Parámetros
- `FastRviLength` – período para el cálculo del RVI rápido (predeterminado 23).
- `SlowRviLength` – período para el cálculo del RVI lento (predeterminado 50).
- `CycleLength` – longitud de los ciclos estocásticos (predeterminado 10).
- `HighLevel` – umbral superior para detectar condiciones alcistas (predeterminado 60).
- `LowLevel` – umbral inferior para detectar condiciones bajistas (predeterminado -60).
- `CandleType` – tipo de vela procesada por la estrategia (marco temporal de 4 horas por defecto).

## Lógica de trading
1. Calcular los valores RVI rápido y lento.
2. Construir el Schaff Trend Cycle a partir de la diferencia RVI.
3. **Comprar** cuando el valor STC está por encima del nivel superior y subiendo.
4. **Vender** cuando el valor STC está por debajo del nivel inferior y bajando.

## Notas
- La estrategia procesa únicamente velas terminadas.
- La protección de posición se activa al inicio.
- Este ejemplo se proporciona con fines educativos y no constituye asesoramiento financiero.
