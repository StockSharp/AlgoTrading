# Estrategia de Three Breaky
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia de Three Breaky** es una conversión completa del experto asesor MetaTrader 4 `ThreeBreaky_v1.mq4`. La versión de StockSharp conserva el trío original de subsistemas de ruptura, traduce su lógica basada en velas a la API de alto nivel y añade una gestión clara de posiciones para cada módulo. La estrategia trabaja en un único marco temporal configurable y puede habilitar o deshabilitar cualquier subsistema sin afectar a los otros.

## Módulos de trading

1. **Sistema 1 – Ruptura de expansión ATR**
   - Usa solo la vela anterior.
   - Va largo cuando la vela anterior es alcista y su rango alto-bajo supera cuatro veces el rango verdadero promedio de 72 períodos.
   - Va corto cuando la vela anterior es bajista y se cumple la misma condición de rango.

2. **Sistema 2 – Cambio de nube Ichimoku**
   - Observa los límites de la nube (Senkou Span A y Senkou Span B) con períodos predeterminados 9/26/52.
   - Una señal larga se activa cuando hace dos velas el cierre fue por debajo de ambos spans y el último cerró por encima de ambos (un cambio alcista a través de la nube).
   - Una señal corta se activa cuando hace dos velas el cierre fue por encima de ambos spans y el último cerró por debajo de ambos.

3. **Sistema 3 – Ruptura de cuerpo excepcional**
   - Registra el tamaño del cuerpo de las últimas 20 velas completadas.
   - Una configuración larga requiere que la vela anterior sea alcista y su cuerpo sea más de tres veces el cuerpo máximo observado en esa historia de 20 velas.
   - Una configuración corta refleja la condición para cuerpos bajistas.

Cada subsistema opera una posición virtual dedicada. Las marcas de tiempo de las órdenes se almacenan para garantizar que un módulo pueda abrir como máximo una operación por vela, igual que la lógica original `buyTag` y `sellTag`.

## Lógica de salida

- **Reversión de SAR Parabólico**: Todas las posiciones abiertas comparten una salida de SAR Parabólico (0.005/0.2). Cuando el precio cruza el SAR entre las dos últimas velas, la posición afectada se cierra.
- **Gestión de riesgo**: Las distancias opcionales de stop-loss y take-profit (en pips) se evalúan en cada vela completada. Si se superan los umbrales configurados, la posición relevante se cierra inmediatamente.

## Indicadores utilizados

- Rango verdadero promedio (período 72) para la línea base de volatilidad promedio.
- Ichimoku Kinko Hyo (9, 26, 52) para el filtro de cambio de nube.
- SAR Parabólico (aceleración 0.005, máximo 0.2) para salidas y lógica de trailing.
- Buffer de tamaño de cuerpo rodante (20 velas) para reproducir la comparación de cuerpo máximo MQL.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `UseSystem1` | Habilita el módulo de ruptura de expansión ATR. |
| `UseSystem2` | Habilita el módulo de cambio de nube Ichimoku. |
| `UseSystem3` | Habilita el módulo de ruptura de cuerpo grande. |
| `OrderVolume` | Volumen usado para cada orden de mercado generada por cualquier módulo. |
| `StopLossPips` | Distancia protectora del stop en pips. Establecer en cero para deshabilitar. |
| `TakeProfitPips` | Distancia del take-profit en pips. Establecer en cero para deshabilitar. |
| `CandleType` | Marco temporal para las velas de trabajo (predeterminado 1 hora). |

## Resumen del flujo de trabajo

1. Suscribirse a la serie de velas configurada y procesar solo las velas finalizadas.
2. Actualizar los indicadores ATR, Ichimoku y SAR Parabólico junto con el historial de cuerpos rodante.
3. Cerrar posiciones que alcancen stops, objetivos o reversiones de SAR Parabólico.
4. Si el trading está permitido, evaluar cada subsistema de forma independiente y emitir órdenes de mercado cuando se cumplan todas las condiciones respectivas.
5. Almacenar las últimas salidas de los indicadores para que la siguiente vela pueda acceder a los mismos valores históricos que en la implementación MQL original.

## Notas

- La estrategia asume un valor de pip basado en el paso de precio del instrumento; las cotizaciones FX de cinco y tres dígitos se normalizan a tamaños de pip de cuatro y dos decimales respectivamente.
- Los subsistemas pueden ejecutarse simultáneamente. Cada uno mantiene su propio precio de entrada, dirección de posición y últimas marcas de tiempo de señal para reflejar la separación `MagicNumber+N` del EA fuente.
- La implementación de StockSharp retiene el patrón de ejecución "una vez por barra" usando los tiempos de apertura de velas para bloquear órdenes duplicadas dentro de una sola barra.
