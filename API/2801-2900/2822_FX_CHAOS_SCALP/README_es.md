# Estrategia de Scalping FX-CHAOS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
La estrategia de scalping FX-CHAOS replica el asesor experto MT5 que combina el Awesome Oscillator con niveles ZigZag basados en fractales en múltiples marcos temporales. La versión de StockSharp se suscribe a velas horarias para la ejecución de operaciones y velas diarias para un filtro de marco temporal superior. Los rastreadores internos reconstruyen la lógica del "ZigZag on Fractals" detectando patrones fractales de cinco velas y uniéndolos en puntos de oscilación alternos.

## Flujo de Trading
1. **Recolección de datos**
   - Las velas horarias impulsan las entradas y la gestión de riesgo.
   - Las velas diarias alimentan el filtro ZigZag de mayor marco temporal.
   - Se calcula un Awesome Oscillator (5, 34) sobre el feed horario.
2. **Seguimiento del ZigZag fractal**
   - Cada vela terminada se incorpora a una ventana deslizante de cinco elementos.
   - Cuando la barra central forma un fractal ascendente/descendente, se actualiza el último valor de oscilación; las oscilaciones consecutivas en la misma dirección solo se reemplazan por valores más extremos.
3. **Detección de señales al cierre horario**
   - Aparece una señal larga cuando la vela abre por debajo del máximo anterior, cierra por encima de él, permanece por debajo del último giro ZigZag horario, está por encima del nivel ZigZag diario más reciente y el Awesome Oscillator es negativo.
   - Una señal corta refleja la lógica usando el mínimo anterior y la polaridad opuesta del oscilador.
4. **Ejecución de órdenes**
   - Las posiciones opuestas existentes se cierran antes de colocar una nueva entrada con el volumen configurado.
   - El precio de entrada se almacena para la gestión posterior de stop loss y take profit.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `Volume` | Volumen de trading en lotes. Se aplica a cada orden de mercado. |
| `Stop Loss (pts)` | Distancia en puntos para el stop protector. El valor se multiplica por el paso de precio del instrumento. Establezca `0` para deshabilitar. |
| `Take Profit (pts)` | Distancia en puntos para el objetivo de beneficio. Se convierte con el paso de precio de la misma manera. Establezca `0` para deshabilitar. |
| `Trading Candle` | Marco temporal principal utilizado para las entradas (por defecto 1 hora). |
| `Daily Candle` | Marco temporal superior utilizado para el filtro ZigZag (por defecto 1 día). |

## Gestión de Riesgo
- En cada vela horaria terminada, la estrategia verifica si el precio tocó el nivel de stop loss o take profit derivado del precio de entrada almacenado.
- Una orden protectora ejecutada cierra la posición inmediatamente y reinicia el indicador del precio de entrada, evitando una re-entrada en el mismo ciclo de vela.
- Las posiciones también se cierran cuando aparece una nueva señal en la dirección opuesta.

## Notas de Implementación
- La lógica ZigZag personalizada evita los búferes directos de indicadores y sigue las directrices del repositorio trabajando en suscripciones de velas con estado local mínimo.
- Los valores ZigZag permanecen `null` hasta que se procesan suficientes velas (dos barras a cada lado de un fractal potencial). El trading se suspende hasta que ambos rastreadores horarios y diarios produzcan oscilaciones válidas.
- El Awesome Oscillator se solicita mediante `BindEx`, asegurando que la estrategia use solo valores finales del indicador cuando todas las entradas están listas.
- Las distancias de precio se escalan por `Security.PriceStep`. Si el instrumento carece de un paso, la estrategia recurre a un multiplicador de un punto.

## Archivos
- `CS/FxChaosScalpStrategy.cs` – implementación de la estrategia con el rastreador ZigZag, el filtro Awesome Oscillator y la lógica de órdenes.
- `README_zh.md` – documentación en chino simplificado.
- `README_ru.md` – documentación en ruso.
