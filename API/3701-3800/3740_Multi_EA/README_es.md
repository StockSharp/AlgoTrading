# MultiStrategyEA v1.2 (puerto StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una versión StockSharp de alto nivel del asesor experto MetaTrader **MultiStrategyEA v1.2**. El EA original agrega siete osciladores y gestiona múltiples cuadrículas de órdenes. La versión StockSharp se centra en el aspecto de generación de señales y comercializa una única posición neta impulsada por un consenso entre los módulos de indicadores. La gestión de pedidos, los perfiles de gestión de dinero, las cuadrículas y las funciones de recuperación del código MT5 se omiten intencionalmente para mantener la implementación alineada con el nivel alto API de StockSharp y para mantener la claridad.

## Módulos
La estrategia evalúa los siguientes módulos de indicadores en el marco temporal seleccionado:

1. **Oscilador de aceleración/deceleración (AC)**: utiliza la diferencia entre el Awesome Oscillator y su SMA de 5 períodos. Requiere que el valor actual supere el umbral `AcLevel` y aumente (o disminuya) en relación con la lectura anterior.
2. **Índice direccional promedio (ADX)**: confirma las tendencias cuando la fuerza de ADX está por encima de `AdxTrendLevel` y el movimiento direccional que domina también supera `AdxDirectionalLevel`.
3. **Awesome Oscillator (AO)**: detecta ráfagas de impulso cuando el oscilador cruza más allá de `AoLevel` y continúa en la misma dirección.
4. **DeMarker**: señala posibles reversiones cuando el oscilador abandona territorios de sobreventa (`100 - DeMarkerThreshold`) o sobrecompra (`DeMarkerThreshold`).
5. **Índice de fuerza + Bollinger bandas**: requiere que el precio toque una banda Bollinger, mientras que el índice de fuerza (escalado en el puerto exactamente como en el script MT5) confirma el impulso más allá de `ForceConfirmationLevel`. Un `BandDistanceFilter` opcional rechaza señales cuando el ancho de banda, medido en pips, es demasiado estrecho o demasiado ancho.
6. **Índice de flujo de dinero (IMF)** – Similar a DeMarker; reacciona a las zonas de sobrecompra y sobreventa determinadas por `MfiThreshold`.
7. **MACD + Stochastic**: exige que tanto MACD (`MacdLevel`) como Stochastic (`StochasticLevel`) confirmen el mismo sesgo direccional. MACD debe estar por encima/por debajo del nivel y por encima/por debajo de su línea de señal. Stochastic debe estar por encima/por debajo del umbral y por encima/por debajo de la línea de señal.

Cada módulo aporta un voto de **compra**, **venta** o **neutral** según la última vela terminada.

## Lógica del consenso
- Cuando `TradeAllStrategies` es **verdadero** (predeterminado), la estrategia espera hasta que aparezcan al menos `RequiredConfirmations` votos alcistas con **cero** votos bajistas antes de entrar en largo. La misma lógica se refleja en los pantalones cortos.
- Cuando `TradeAllStrategies` es **falso**, un solo voto alcista o bajista es suficiente para operar.
- Si `CloseInReverse` está habilitado, la estrategia cierra inmediatamente una posición opuesta antes de abrir una nueva.

La implementación opera solo una posición agregada y no intenta recrear la contabilidad de pedidos por módulo del EA original.

## Gestión del riesgo
- `StopLossPips` y `TakeProfitPips` se traducen en compensaciones de precios utilizando el `PriceStep` del instrumento. Para símbolos con 3 o 5 dígitos decimales, el tamaño del pip se multiplica automáticamente por 10, imitando el comportamiento de los pips de FX.
- Las paradas y los objetivos se verifican en cada vela terminada utilizando máximos y mínimos de velas. Cuando se alcanza cualquiera de los umbrales, se cierra toda la posición.

## Diferencias con el Asesor Experto MT5
- Sin funciones de cuadrícula, martingala o recuperación. El tamaño de la posición se fija mediante el parámetro `Volume`.
- Las variantes de señal de cierre (opciones `CloseOrdersType` en MT5) no están implementadas; las salidas se basan en un stop-loss/take-profit global o en el comportamiento opcional de inversión de señal opuesta.
- La configuración del indicador en StockSharp refleja la idea principal de cada módulo, pero solo admite la interpretación más común en lugar de las muchas enumeraciones de modos que se encuentran en el script original.
- Los bloques de administración de dinero (lote automático, protección de cuenta, valoración de pips de símbolos específicos) están fuera del alcance de esta portabilidad de alto nivel.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Serie de datos utilizados por cada módulo indicador. |
| `Volume` | Volumen neto negociado cuando aparece una señal de consenso. |
| `TradeAllStrategies` | Permite la votación por consenso; de lo contrario, cualquier voto desencadena un intercambio. |
| `RequiredConfirmations` | Número de votos coincidentes alcistas o bajistas necesarios cuando se habilita el consenso. |
| `CloseInReverse` | Cierre una posición existente antes de abrir el lado opuesto. |
| `StopLossPips` / `TakeProfitPips` | Stop protector y objetivo de ganancias medidos en pips. |
| `UseAcModule`, `AcLevel` | Alternancia y umbral para el módulo Accelerator Oscillator. |
| `UseAdxModule`, `AdxPeriod`, `AdxTrendLevel`, `AdxDirectionalLevel` | Configuración ADX. |
| `UseAoModule`, `AoLevel` | Impresionante configuración del oscilador. |
| `UseDeMarkerModule`, `DeMarkerPeriod`, `DeMarkerThreshold` | Configuración del oscilador DeMarker. |
| `UseForceBollingerModule`, `BollingerPeriod`, `BollingerDeviation`, `ForceConfirmationLevel`, `BandDistanceFilter` | Índice de fuerza + Bollinger configuración del filtro de banda. |
| `UseMfiModule`, `MfiPeriod`, `MfiThreshold` | Configuración del índice de flujo de dinero. |
| `UseMacdStochasticModule`, `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod`, `MacdLevel`, `StochasticPeriod`, `StochasticSignalPeriod`, `StochasticSlowing`, `StochasticLevel` | Configuración combinada MACD y Stochastic. |

## Notas de uso
1. Adjunte la estrategia a un instrumento con suficientes datos históricos para que se formen todos los indicadores.
2. Configure los umbrales de plazo y módulo para que coincidan con las condiciones de mercado deseadas. Los valores predeterminados replican los valores utilizados en las entradas MT5 EA.
3. La lógica de consenso es sensible a cuántos módulos están activos. Si desactiva los módulos, considere reducir `RequiredConfirmations` en consecuencia.
4. Debido a que la estrategia negocia una única posición neta, es adecuada para su uso dentro de Designer, Runner u otros StockSharp entornos de alto nivel sin enrutamiento de cartera adicional.

## Descargo de responsabilidad
Este puerto se centra en la paridad de señales en lugar de reproducir toda la pila de gestión de riesgos y dinero del experto original MetaTrader. La arquitectura simplificada hace que sea más fácil probar, ampliar o integrar en soluciones basadas en StockSharp, pero los resultados diferirán de la versión MT5 cuando las funciones complejas (cuadrículas, lotes de recuperación, cierres parciales) eran el principal impulsor del rendimiento.
