# Estrategia Up3x1 desplazada SMA (conversión MT4)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Conversión del MetaTrader 4 asesor experto `up3x1.mq4` ubicado en `MQL/8097`.
- Implementa el cruce triple de media móvil simple con un cambio positivo en el gráfico exactamente como en el guión original.
- Los procesos solo completaron velas para emular la guardia `Volume[0] > 1` que obligó al experto a evaluar una vez por barra.
- Las funciones de gestión de riesgos incluyen toma de ganancias, stop loss, reducción dinámica de lotes después de perder operaciones y un trailing stop opcional.

## Lógica de trading
1. **Indicadores**
   - Tres medias móviles simples con un desplazamiento del gráfico de 6 barras (rápida = 24, media = 60, lenta = 120 por defecto).
2. **Entrada larga**
   - Barra anterior: `SMAfast₍t-1₎ < SMAmedium₍t-1₎ < SMAslow₍t-1₎`.
   - Barra actual: `SMAmedium₍t₎ < SMAfast₍t₎ < SMAslow₍t₎`.
   - La condición replica `ma1 < ma2 < ma3 && ma5 < ma4 < ma6` de MQL.
3. **Entrada corta**
   - Barra anterior: `SMAfast₍t-1₎ > SMAmedium₍t-1₎ > SMAslow₍t-1₎`.
   - Barra actual: `SMAmedium₍t₎ > SMAfast₍t₎ > SMAslow₍t₎`.
4. **Reglas de salida**
   - Take Profit y Stop Loss respetan la distancia de puntos configurada multiplicada por `Security.PriceStep` (o se usa directamente cuando se desconoce el paso).
   - El trailing stop bloquea las ganancias una vez que el precio avanza más de `TrailingStopPoints` y sigue el extremo alcanzado después de la entrada.
   - Salida a prueba de fallos cuando las medias móviles cambian al orden opuesto, reflejando la lógica `OrderClose` original.

## Dimensionamiento de posiciones
- El volumen predeterminado es igual a `BaseVolume` (0,1 lote) siempre que las métricas de la cartera no estén disponibles.
- Cuando existe `Portfolio.CurrentValue`, la estrategia lo multiplica por `RiskFraction` (por defecto `0.00002`, equivalente a la fórmula MQL `FreeMargin * 0.02 / 1000`).
- Después de más de una salida perdedora, el volumen se reduce en `volume * losses / 3`, exactamente como en la rutina `LotsOptimized`.
- El volumen se redondea a la baja a `Security.VolumeStep` y se reduce a cero si no puede satisfacer `Security.MinVolume`.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `FastPeriod` | 24 | Longitud del desplazamiento más rápido SMA. |
| `MediumPeriod` | 60 | Longitud del medio desplazada SMA. |
| `SlowPeriod` | 120 | Duración del cambio lento SMA. |
| `TakeProfitPoints` | 150 | Distancia en puntos de precio entre el precio de entrada y la toma de ganancias. |
| `StopLossPoints` | 100 | Distancia en puntos de precio entre el precio de entrada y el stop loss. |
| `TrailingStopPoints` | 100 | Distancia de trailing stop opcional en puntos (establecido en 0 para desactivarlo). |
| `BaseVolume` | 0.1 | Tamaño del comercio alternativo y volumen mínimo después de las reducciones. |
| `RiskFraction` | 0.00002 | Fracción del valor de la cartera utilizada para calcular el volumen dinámico. |
| `CandleType` | plazo de 1 hora | Serie de velas utilizadas para alimentar indicadores. |

## Notas de conversión
- La estrategia utiliza el nivel alto API (`SubscribeCandles` + `Bind`) y evita los buffers de historial manuales.
- Los valores del indicador se almacenan entre llamadas para imitar el parámetro `shift` sin acceso directo al índice.
- Las salidas protectoras se ejecutan con comandos de mercado en el nivel de precio detectado para seguir siendo compatibles con la abstracción StockSharp.
- Todos los comentarios en línea están escritos en inglés y cumplen con las pautas del proyecto.

## Uso
1. Adjunte la estrategia a un valor y una cartera en StockSharp Designer o código.
2. Seleccione una serie de velas (`CandleType`) que coincida con su período de tiempo MT4 (H1 de forma predeterminada).
3. Revise los parámetros de riesgo basados en puntos para alinearlos con el tamaño del tick del instrumento (por ejemplo, 0,0001 para la mayoría de los pares de Forex).
4. Establezca `TrailingStopPoints` en cero cuando no sea necesario el seguimiento.
5. Supervise los registros en busca de mensajes como "Introducir largo" y "Salir breve" que reflejan los diagnósticos MQL.

## Estructura del repositorio
```
API/3924/
├── CS/Up3x1ShiftedSmaStrategy.cs # Estrategia C# convertida con comentarios en inglés
├── README.md # Documentación en inglés (este archivo)
├── README_zh.md # traducción al chino
└── README_ru.md # traducción al ruso
```

## Descargo de responsabilidad
El comercio implica un riesgo significativo. La estrategia se proporciona con fines educativos y debe validarse con datos históricos y simulados antes de operar en vivo.
