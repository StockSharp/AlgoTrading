# Estrategia de Arbitraje de Clubes de Fútbol
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia compara los cierres de velas completadas de dos instrumentos relacionados. Calcula la prima relativa como `principal / segundo - 1` y opera ambas patas cuando la prima absoluta supera el umbral de entrada.

Si el instrumento principal es más caro, la estrategia lo vende y compra el segundo con el mismo volumen en unidades. Si el segundo es más caro, invierte las direcciones. Ambas posiciones se cierran cuando la prima absoluta cae por debajo del umbral de salida.

## Detalles

- **Datos**: Velas completadas del valor principal y de `Security2Id`; el marco temporal predeterminado es de cinco minutos.
- **Entrada**: Órdenes de mercado opuestas con igual número de unidades cuando la prima supera `EntryThreshold` en cualquier dirección.
- **Salida**: Cierre de la posición real de cada pata cuando la prima absoluta es inferior a `ExitThreshold`.
- **Pausa**: Tras una entrada, salida o inversión, espera `CooldownBars` actualizaciones emparejadas de velas.
- **Riesgo de ejecución**: Las dos órdenes de mercado se envían por separado y no son atómicas. Unidades iguales tampoco garantizan nocionales iguales, por lo que persisten los riesgos de ejecución de una sola pata, liquidez y tamaño de contrato.

