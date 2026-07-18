# Estrategia Backbone
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce el comportamiento central del asesor experto original **Backbone** de MQL5 usando la API de alto nivel de StockSharp. Alterna entre ciclos de trading alcista y bajista, escala en posiciones según una fracción de riesgo, y protege las operaciones abiertas con objetivos fijos junto con un trailing stop.

## Idea central

1. **Detección de dirección inicial** – la estrategia rastrea el máximo más alto y el mínimo más bajo después del inicio. Un movimiento mayor que la distancia del trailing stop alejándose de cualquier extremo define qué lado operará primero.
2. **Ciclos direccionales** – después de que comienza un ciclo, el algoritmo solo opera en esa dirección hasta que todas las posiciones estén cerradas. Cuando la última posición sale, cambia inmediatamente y se prepara para el ciclo opuesto.
3. **Escalado basado en riesgo** – cada entrada adicional usa un volumen dinámico derivado del capital de la cuenta, la fracción `MaxRisk`, el límite configurado `MaxTrades`, y la distancia del stop-loss. Esto imita la función de dimensionamiento de lotes del EA original.
4. **Salidas protectoras** – cada entrada recalcula un nivel de stop-loss y take-profit alrededor del precio promedio ponderado por volumen del ciclo actual. Un trailing stop ajusta el stop protector siempre que el beneficio no realizado supere la distancia de trailing configurada.

## Parámetros

| Parámetro | Valor predeterminado | Descripción |
|-----------|---------|-------------|
| `MaxRisk` | 0.5 | Fracción del capital de la cuenta disponible para todas las posiciones en la dirección actual. |
| `MaxTrades` | 10 | Número máximo de entradas secuenciales por ciclo direccional. |
| `TakeProfitPips` | 170 | Distancia (en pips) entre el promedio de entrada y el objetivo de take-profit. |
| `StopLossPips` | 40 | Distancia (en pips) entre el promedio de entrada y el stop protector. |
| `TrailingStopPips` | 300 | Distancia (en pips) usada tanto para determinar la dirección inicial como para seguir los beneficios. |
| `CandleType` | Marco temporal de 5 minutos | Tipo de vela usado para la evaluación de señales. |

> **Definición de pip** – la estrategia ajusta automáticamente el tamaño del pip basándose en el instrumento `PriceStep`. Los símbolos cotizados con 3 o 5 decimales usan un multiplicador de 10×, lo que replica el manejo de pips original de MetaTrader.

## Lógica de trading

1. Esperar una vela terminada. Omitir el procesamiento mientras la estrategia se está calentando o el trading está deshabilitado.
2. Actualizar los precios extremos mientras no se haya elegido ninguna dirección todavía. Una vez que el máximo rompe hacia arriba (en más de `TrailingStopPips`) el primer ciclo será corto; si el mínimo rompe hacia abajo, el primer ciclo será largo.
3. Mientras el ciclo es largo:
   - Agregar una nueva entrada larga cuando (a) el ciclo anterior fue corto y no hay posiciones largas abiertas, o (b) el ciclo anterior también fue largo y el número de largos abiertos está por debajo de `MaxTrades`.
   - Salir de todo el ciclo largo cuando se alcanza el take-profit o stop-loss, o cuando el trailing stop eleva el nivel protector por encima del stop actual.
4. Mientras el ciclo es corto se aplican las mismas reglas con condiciones invertidas.
5. Después de que un ciclo cierra, reiniciar sus contadores y esperar la configuración opuesta.

## Dimensionamiento de posición

El tamaño de posición para cada nueva entrada se calcula como:

```
qty = equity * fraction / (pipSize * stopLoss)
donde fraction = 1 / (MaxTrades / MaxRisk - openTrades)
```

La cantidad se alinea entonces al paso de volumen del instrumento y se limita dentro de los límites mínimo/máximo de volumen. Si el tamaño requerido cae por debajo del mínimo permitido, se usa el mínimo. Cuando la información de capital no está disponible, el volumen de estrategia predeterminado actúa como respaldo.

## Gestión de salida

- **Stop-loss / take-profit** – recalculado siempre que se agrega una nueva orden para que todas las operaciones en el ciclo actual compartan los mismos niveles combinados basados en el precio promedio de entrada.
- **Trailing stop** – para un ciclo largo el stop se mueve a `Close - TrailingStopPips * pipSize` una vez que el beneficio no realizado supera ese umbral. El trailing del lado corto se maneja simétricamente.

## Notas y limitaciones

- StockSharp ejecuta operaciones en un entorno de neteo, por lo tanto cada ciclo direccional gestiona la posición combinada en lugar de tickets individuales. La lógica alternante y la fórmula de riesgo reproducen el comportamiento original mientras se ajustan al modelo de API.
- La estrategia depende de velas completadas. Los movimientos intrabar menores que el rango de la vela no se evalúan.
- Asegurarse de que el tipo de vela seleccionado y el instrumento produzcan suficientes datos para construir los extremos iniciales antes de esperar operaciones.
