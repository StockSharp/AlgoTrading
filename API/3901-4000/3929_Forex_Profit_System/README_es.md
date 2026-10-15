# Estrategia del sistema de ganancias Forex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia reproduce el clásico MetaTrader asesor experto "Forex Profit System" dentro dla API de alto nivel de StockSharp. se utiliza
tres promedios móviles exponenciales (EMA 10, 25 y 50) en el precio medio de la vela combinados con un filtro Parabolic SAR. el
La combinación detecta ráfagas de impulso de corta duración que aparecen después de que el promedio rápido cruza la línea de tendencia lenta mientras el Parabolic
SAR ya está volteado hacia el mismo lado que el precio.

## Lógica de trading

1. **Pila de indicadores**
   - El precio medio derivado de la vela terminada impulsa todos los indicadores para que los resultados coincidan con el MetaTrader "PRICE_MEDIAN" original
entrada.
   - Fast EMA (longitud 10) reacciona rápidamente a cambios de impulso a corto plazo.
   - El medio EMA (longitud 25) y el EMA lenta (longitud 50) definen el sesgo direccional.
   - Parabolic SAR con paso 0.02 y máximo 0.2 confirma que el precio ya rompió al nuevo lado de la tendencia.
2. **Entrada larga**
   - EMA(10) es mayor que EMA(25) y EMA(50).
   - EMA(10) estaba por debajo de EMA(50) en la vela cerrada anterior (confirmación cruzada).
   - El valor Parabolic SAR está por debajo del cierre de la vela, lo que significa que los puntos cambiaron al modo alcista.
   - No existe ninguna posición abierta y la estrategia puede operar (en línea + permisos).
3. **Entrada corta**
   - EMA(10) es menor que EMA(25) y EMA(50).
   - EMA(10) estaba por encima de EMA(50) en la vela cerrada anterior (confirmación cruzada hacia abajo).
   - Parabolic SAR está por encima del cierre de la vela.
4. **Gestión de salida**
   - Los stop-loss y take-profit estrictos se aplican inmediatamente después de la entrada con configuraciones asimétricas para operaciones largas y cortas.
   - Se activa un trailing stop una vez que el precio se mueve lo suficiente a favor de la posición. La parada se detiene en `current price -/+ trailing`
distancia dependiendo de la dirección.
   - La salida anticipada ocurre cuando EMA(10) invierte la dirección (cae por debajo de su valor anterior para largos o sube por encima para cortos) y el
el beneficio abierto supera una distancia mínima de activación.

## Valores de parámetros predeterminados

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `CandleType` | marco de tiempo de 15 minutos | Serie de velas procesadas por la estrategia. |
| `FastEmaLength` | 10 | Período de la EMA rápida. |
| `MediumEmaLength` | 25 | Período del medio EMA. |
| `SlowEmaLength` | 50 | Periodo de la lentitud EMA. |
| `SarStep` | 0,02 | Aceleración inicial para Parabolic SAR. |
| `SarMax` | 0,2 | Aceleración máxima para Parabolic SAR. |
| `Volume` | 0.1 | Volumen de negociación en lotes/contratos. |
| `LongTakeProfitPoints` | 50 | Distancia de obtención de beneficios para operaciones largas medida en puntos de precio. |
| `ShortTakeProfitPoints` | 50 | Distancia de obtención de beneficios para operaciones cortas medida en puntos de precio. |
| `LongStopLossPoints` | 30 | Distancia de stop-loss para operaciones largas medida en puntos de precio. |
| `ShortStopLossPoints` | 30 | Distancia de stop-loss para operaciones cortas medida en puntos de precio. |
| `LongTrailingStopPoints` | 10 | Distancia de activación del trailing stop para operaciones largas. |
| `ShortTrailingStopPoints` | 10 | Distancia de activación del trailing stop para operaciones cortas. |
| `LongProfitTriggerPoints` | 10 | Se requiere un beneficio abierto mínimo (puntos) antes de que se pueda cerrar una operación larga en la reversión de EMA. |
| `ShortProfitTriggerPoints` | 5 | Se requiere una ganancia abierta mínima (puntos) antes de que se pueda cerrar una operación corta en la reversión de EMA. |

## Notas de implementación

- La estrategia utiliza suscripciones de velas y vinculación de indicadores en el nivel alto API mientras mantiene todo el control de riesgos dentro del
clase de estrategia. No se requiere acceso al libro de pedidos de bajo nivel.
- Todas las distancias de gestión comercial se convierten de puntos en compensaciones de precios reales utilizando el instrumento `PriceStep`. Si `PriceStep`
no está disponible, se utiliza el valor de puntos sin procesar, por lo que el algoritmo aún funciona en instrumentos sintéticos.
- Las paradas de protección (`SetStopLoss`, `SetTakeProfit`) se establecen utilizando la posición resultante después de que se envía la orden de mercado para permanecer en
sincronizar con posibles rellenos parciales.
- El estado interno realiza un seguimiento del último precio de entrada por dirección para que las salidas finales y basadas en EMA puedan evaluar el precio realizado.
progresar precisamente.
- Debido a que toda la lógica se ejecuta en velas terminadas, no hay riesgo de volver a pintar y las señales reflejan el comportamiento MetaTrader original que
calculó todo sobre los precios de cierre de `start()`.

## Uso sugerido

- El método es adecuado para pares de divisas líquidos en gráficos intradiarios (por defecto de 15 minutos). Se pueden utilizar plazos más altos ajustando el
EMA períodos y distancias de gestión comercial en consecuencia.
- Para activos con diferentes tamaños de ticks o niveles de volatilidad, ajuste los parámetros basados en puntos (`StopLoss`, `TakeProfit`,
`TrailingStop`, `ProfitTrigger`) para que las distancias coincidan con el perfil del instrumento.
- Combínelo con filtros de distribución o de sesión si el lugar tiene amplia distribución durante ciertas horas; la estrategia espera razonable
ejecución para darse cuenta de los estallidos de impulso a corto plazo.
