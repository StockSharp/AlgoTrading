# Estrategia de Diferencia Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera basándose en la diferencia entre las líneas %K y %D del oscilador Stochastic. La diferencia se suaviza con una media móvil exponencial para reducir el ruido. Se abre una posición larga cuando la diferencia suavizada forma un mínimo local y gira al alza. Se abre una posición corta cuando la diferencia suavizada forma un máximo local y gira a la baja.

## Cómo Funciona

1. Calcular el Stochastic %K y %D con períodos definidos por el usuario.
2. Calcular la diferencia `%K - %D` y suavizarla con una EMA.
3. Detectar puntos de giro en la diferencia suavizada:
   - Si el valor estaba bajando y luego sube, abrir una posición larga.
   - Si el valor estaba subiendo y luego baja, abrir una posición corta.
4. Aplicar protecciones opcionales de stop-loss y take-profit en porcentaje.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| Candle Type | Tipo de vela usado para los cálculos |
| %K Period | Período para la línea %K |
| %D Period | Período para la línea %D |
| Slowing | Suavizado adicional de %K |
| Smoothing Length | Longitud de la EMA para la diferencia |
| Stop Loss % | Tamaño del stop-loss en porcentaje |
| Take Profit % | Tamaño del take-profit en porcentaje |

## Notas

- Funciona en cualquier instrumento y marco temporal compatible con el feed de datos.
- Diseñado con fines educativos para demostrar señales de entrada basadas en indicadores.
