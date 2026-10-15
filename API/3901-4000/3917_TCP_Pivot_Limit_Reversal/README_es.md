# Estrategia de límite de inversión de pivote TCP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia TCP Pivot Limit es una conversión del clásico MetaTrader 4 experto **gpfTCPivotLimit.mq4**. El experto original calcula los niveles de pivote diarios y busca rupturas falsas alrededor de estos niveles utilizando velas horarias. Una vez que falla una ruptura, la estrategia ingresa inmediatamente a una operación de reversión dirigida a los niveles de pivote opuestos. Esta implementación reproduce la misma lógica utilizando la StockSharp estrategia de alto nivel API.

La estrategia opera con velas horarias y mantiene solo una posición abierta en cualquier momento. Cada nuevo día de negociación, recalcula la cuadrícula dinámica a partir de los valores máximo, mínimo y de cierre del día anterior. Estos niveles guían los activadores de entrada, el stop-loss, la toma de ganancias y la gestión de seguimiento opcional.

## Lógica de trading

1. **Cálculo de pivote**
   - En la primera vela de cada nuevo día de negociación, la estrategia agrega el máximo, el mínimo y el cierre del día anterior para calcular los niveles de pivote del operador de piso clásico (Pivot, R1–R3, S1–S3).
   - Se genera una entrada de registro cada vez que se generan nuevos niveles para que puedas seguir cómo evoluciona la cuadrícula.

2. **Condiciones de entrada**
   - En cada vela horaria terminada, la estrategia verifica las dos últimas velas completadas.
   - Una posición *corta* se abre cuando la vela de hace dos períodos subió por encima de un nivel de resistencia (o cerró en/por encima de él) mientras se abría por debajo de él, y la vela más reciente volvió a cerrar por debajo de ese nivel. Esto indica una ruptura fallida y se espera una reversión a la baja.
   - Una posición *larga* se abre simétricamente cuando el mercado cae por debajo de un nivel de soporte pero la siguiente vela vuelve a cerrar por encima de él.
   - Sólo puede haber una posición activa a la vez. El volumen del pedido está definido por el parámetro `OrderVolume`.

3. **Gestión de salida**
   - Cada entrada utiliza los niveles de stop-loss y take-profit definidos por el ajuste preestablecido `TargetMode` seleccionado. Los ajustes preestablecidos reflejan las opciones `TgtProfit` del asesor experto original y combinan diferentes niveles de pivote:
     | Modo | Entrada corta | Parada corta | Objetivo corto | Entrada larga | Parada larga | Objetivo largo |
     |------|-------------|------------|--------------|------------|-----------|-------------|
     | 1    | R1          | R2         | T1           | T1         | T2        | R1          |
     | 2    | R1          | R2         | T2           | T1         | T2        | R2          |
     | 3    | R2          | R3         | T1           | T2         | T3        | R1          |
     | 4    | R2          | R3         | T2           | T2         | T3        | R2          |
     | 5    | R2          | R3         | T3           | T2         | T3        | R3          |
   - Si `IntradayTrading` está habilitado, cualquier posición abierta se cierra en el cierre de la vela de las 23:00 para evitar mantenerla durante la noche.
   - Un trailing stop opcional en puntos (múltiplos del paso del precio del instrumento) emula el comportamiento de MetaTrader. El seguimiento se activa solo después de que el movimiento ha avanzado la distancia configurada y cierra la operación cuando el precio retrocede en la misma cantidad.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `OrderVolume` | Volumen utilizado para órdenes de mercado tanto de compra como de venta. |
| `TargetMode` | Número entero del 1 al 5 que selecciona qué combinación de resistencia/soporte se utiliza para entradas, paradas y objetivos. |
| `TrailingPoints` | Distancia del trailing stop medida en puntos de precio. Establezca en cero para desactivar el seguimiento. |
| `IntradayTrading` | Cuando `true`, las posiciones se cierran a las 23:00 para seguir operando intradía. |
| `CandleType` | Tipo de datos de vela. El valor predeterminado es un período de tiempo de una hora para coincidir con el experto original. |

## Notas

- La estrategia espera un flujo continuo de velas por hora. Aplicarlo a otros marcos temporales cambia el comportamiento y se debe realizar una prueba retrospectiva.
- Los niveles de stop-loss y take-profit se evalúan utilizando velas extremas, por lo que las brechas entre niveles pueden resultar en salidas a peores precios, al igual que en la versión MetaTrader.
- La gestión de seguimiento se realiza en los cierres de velas y en los máximos y mínimos, coincidiendo estrechamente con la lógica original basada en ticks y al mismo tiempo siendo eficiente en el entorno StockSharp.
