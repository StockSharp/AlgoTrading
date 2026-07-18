# Estrategia Up3x1 con Dimensionamiento Dinámico
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
- Conversión del asesor experto de MetaTrader 5 `up3x1.mq5` a la API de alto nivel de StockSharp.
- Opera un cruce de tres medias móviles exponenciales (EMA) con gestión de stop loss, take profit y trailing stop.
- Procesa solo las velas terminadas para emular la protección original `iTickVolume(0) > 1` que forzaba una decisión por barra.
- La serie de velas predeterminada es 1 hora, pero el marco temporal es configurable a través del parámetro `CandleType`.

## Lógica de Trading
1. **Indicadores**
   - EMA Rápida (`FastPeriod`, predeterminado 24).
   - EMA Media (`MediumPeriod`, predeterminado 60).
   - EMA Lenta (`SlowPeriod`, predeterminado 120).
2. **Entrada larga**
   - Barra anterior: EMA rápida por debajo de la EMA media y la media por debajo de la lenta (`EMAfast₍t-1₎ < EMAmedium₍t-1₎ < EMAslow₍t-1₎`).
   - Barra actual: la EMA media por debajo de la EMA rápida mientras la rápida sigue por debajo de la lenta (`EMAmedium₍t₎ < EMAfast₍t₎ < EMAslow₍t₎`).
3. **Entrada corta**
   - Barra anterior: EMA rápida por encima de la EMA media y la media por encima de la lenta (`EMAfast₍t-1₎ > EMAmedium₍t-1₎ > EMAslow₍t-1₎`).
   - Barra actual: la EMA media cruza por encima de la EMA rápida mientras ambas permanecen por encima de la EMA lenta (`EMAmedium₍t₎ > EMAfast₍t₎ > EMAslow₍t₎`).
4. **Lógica de salida para ambas direcciones**
   - Take profit cuando el precio avanza `TakeProfitOffset` desde la entrada (usando el máximo de la vela para largos, el mínimo para cortos).
   - Stop loss cuando el precio retrocede `StopLossOffset` desde la entrada (usando el mínimo de la vela para largos, el máximo para cortos).
   - El trailing stop se activa una vez que la posición se mueve a favor más que `TrailingStopOffset` y luego sigue el precio a esa distancia fija, evaluada en los extremos de la vela.
   - Salida de respaldo cuando la EMA rápida cruza de vuelta por debajo de la EMA media mientras ambas permanecen por encima de la EMA lenta (refleja la comprobación `ma_one_1 > ma_two_1 > ma_three_1` de la versión MQL).

## Dimensionamiento de Posición y Gestión de Riesgos
- `RiskFraction` (predeterminado 0.02) multiplica el valor actual del portafolio para aproximar el dimensionamiento de lotes original `FreeMargin * 0.02 / 1000`.
- `BaseVolume` (predeterminado 0.1) actúa como respaldo cuando los datos del portafolio no están disponibles o el tamaño calculado no es positivo.
- Después de más de una salida perdedora, el volumen se reduce por `volume * losses / 3`, imitando el contador acumulativo `losses` del script (el contador no se reinicia después de operaciones rentables, como en el código original).
- Los volúmenes se redondean hacia abajo a `Security.VolumeStep`, se limitan por `Security.MinVolume` / `Security.MaxVolume`, y se reducen a cero si no se puede cumplir el mínimo del instrumento.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `FastPeriod` | 24 | Longitud de la EMA más rápida. |
| `MediumPeriod` | 60 | Longitud de la EMA media. |
| `SlowPeriod` | 120 | Longitud de la EMA lenta usada como filtro de tendencia a largo plazo. |
| `TakeProfitOffset` | 0.015 | Distancia de precio absoluta para la orden de take profit (adaptar a la cotización del instrumento). |
| `StopLossOffset` | 0.01 | Distancia de precio absoluta para la orden de stop loss. |
| `TrailingStopOffset` | 0.004 | Distancia de trailing que bloquea ganancias una vez que el precio avanza suficientemente; establecer en 0 para deshabilitar. |
| `BaseVolume` | 0.1 | Tamaño de operación de respaldo cuando no se puede calcular el dimensionamiento dinámico. |
| `RiskFraction` | 0.02 | Fracción del valor del portafolio aplicada a la fórmula de dimensionamiento dinámico. |
| `CandleType` | Marco temporal de 1 hora | Serie de velas usada para cálculos de indicadores y toma de decisiones. |

## Notas de Conversión
- El trailing stop y las salidas de protección usan máximos/mínimos de velas en lugar de ticks brutos porque la API de alto nivel procesa velas completadas; esto mantiene el comportamiento determinístico a través de backtests y ejecuciones en vivo.
- El stop loss y el take profit se ejecutan mediante comandos de aplanamiento a mercado en el umbral evaluado en lugar de colocar órdenes de protección separadas, asegurando compatibilidad con el flujo de estrategia de alto nivel.
- El dimensionamiento dinámico de posición depende de `Portfolio.CurrentValue`. Cuando no está disponible, la estrategia recurre a `BaseVolume`, similar al respaldo `LotCheck` del `Lots` manual en el original.
- El contador `losses` es intencionalmente acumulativo (nunca se reinicia en operaciones ganadoras) para seguir la implementación MQL.
- Todos los comentarios están en inglés según las pautas del proyecto.

## Consejos de Uso
1. Adjunte la estrategia a un instrumento y portafolio, luego configure `CandleType` para que coincida con la resolución del gráfico que desea emular desde MT5.
2. Revise los offsets de precio para que reflejen el tamaño del tick de su instrumento (por ejemplo, para un par Forex de 5 dígitos, 0.015 equivale a 150 puntos como en el expert fuente).
3. Ajuste `RiskFraction` / `BaseVolume` para lograr tamaños de posición realistas en relación a su cuenta.
4. Opcional: deshabilite el trailing estableciendo `TrailingStopOffset` en cero.
5. Monitoree los registros para mensajes como "Enter long" o "Exit short" que reflejan los diagnósticos `Print` de MetaTrader.

## Estructura del Repositorio
```
API/2512_Up3x1/
├── CS/Up3x1DynamicSizingStrategy.cs      # Estrategia C# convertida
├── README.md                # Documentación en inglés (este archivo)
├── README_zh.md             # Traducción al chino
└── README_ru.md             # Traducción al ruso
```

## Descargo de Responsabilidad
El trading implica un riesgo significativo. Este ejemplo se proporciona con fines educativos y debe validarse con datos históricos y simulados antes de cualquier implementación en vivo.
