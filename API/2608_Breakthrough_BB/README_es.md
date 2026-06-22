# Estrategia de Rompimiento BB
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia de Rompimiento BB replica el asesor experto de MetaTrader *Breakthrough_BB* dentro de la API de alto nivel de StockSharp. El sistema combina Bandas de Bollinger con una media móvil simple rápida para capturar rupturas explosivas que ocurren después de que el precio se comprime cerca de los límites de la banda. Las operaciones se generan exclusivamente en velas completadas para mantener señales deterministas y reflejar el comportamiento original de MQL5.

## Lógica de trading
- **Filtro de tendencia:** Una media móvil simple (SMA) con período configurable valida la dirección de la tendencia. La estrategia compara el último valor de la SMA con el valor de la SMA de cuatro barras antes. Las operaciones largas requieren que la SMA tenga pendiente ascendente, mientras que los cortos requieren una pendiente descendente.
- **Ruptura de Bandas de Bollinger:** La estrategia observa cómo el cierre de hace cuatro barras interactuó con la banda superior o inferior de Bollinger y lo compara con el precio de cierre más reciente. Una ruptura válida ocurre cuando el precio se mueve desde el interior de la banda hacia el exterior entre esos dos momentos.
- **Modelo de posición única:** El algoritmo mantiene como máximo una posición abierta. Cualquier operación abierta se cierra antes de evaluar nuevas entradas para evitar exposición solapada.

## Condiciones de entrada
### Configuración larga
1. El precio de cierre de hace cuatro velas completas estaba por debajo de la Banda de Bollinger superior.
2. El precio de cierre más reciente terminó por encima de la Banda de Bollinger superior actual.
3. El valor de la SMA calculado en la última vela es mayor que el valor de la SMA de hace cuatro velas (pendiente positiva).
4. No hay ninguna posición abierta actualmente.

### Configuración corta
1. El precio de cierre de hace cuatro velas completas estaba por encima de la Banda de Bollinger inferior.
2. El precio de cierre más reciente terminó por debajo de la Banda de Bollinger inferior actual.
3. El valor de la SMA calculado en la última vela es menor que el valor de la SMA de hace cuatro velas (pendiente negativa).
4. No hay ninguna posición abierta actualmente.

Cuando se cumple una condición de entrada, la estrategia envía una orden de mercado usando el parámetro de volumen configurado.

## Reglas de salida
- **Salida de posición larga:** Si una operación larga está activa y el último cierre cae por debajo de la línea media de Bollinger, la posición se cierra inmediatamente con una orden de venta a mercado.
- **Salida de posición corta:** Si una operación corta está abierta y el último cierre sube por encima de la línea media de Bollinger, la posición se cubre con una orden de compra a mercado.

Estas reglas de salida imitan el asesor experto original, que eliminaba operaciones cada vez que el mercado revertía de vuelta dentro de la línea media de la banda.

## Indicadores
- **Media Móvil Simple (SMA):** Define el sesgo direccional y proporciona la comparación de pendiente en un intervalo de cuatro velas.
- **Bandas de Bollinger:** Suministra las envolventes superior, media e inferior usadas para detectar entradas por ruptura y gestionar salidas.

## Parámetros
| Nombre | Descripción | Predeterminado | Optimizable |
| --- | --- | --- | --- |
| `MaPeriod` | Longitud de la SMA usada para el filtro de tendencia. | `9` | ✔ |
| `BandsPeriod` | Longitud de retrospectiva para los cálculos de Bandas de Bollinger. | `28` | ✔ |
| `Deviation` | Multiplicador de desviación estándar aplicado a las Bandas de Bollinger. | `1.6` | ✔ |
| `Volume` | Tamaño de la orden (en lotes o contratos, según el instrumento). | `1` | ✔ |
| `CandleType` | Tipo de agregación de velas procesado por la estrategia. | Marco temporal de `1 hora` | ✖ |

Todos los parámetros exponen metadatos `StrategyParam` de StockSharp para que puedan ajustarse en la interfaz o optimizarse en el diseñador.

## Requisitos de datos
- Funciona con cualquier instrumento que proporcione datos de velas compatibles con el `CandleType` seleccionado.
- Las señales se evalúan solo en velas terminadas. Las velas incompletas se ignoran para mantener la lógica determinista.
- La configuración predeterminada usa velas horarias, pero se puede suministrar cualquier marco temporal compatible con la fuente de datos.

## Notas adicionales
- El algoritmo evita búsquedas en el historial del indicador y en su lugar mantiene una caché deslizante de cuatro barras para valores de cierre y SMA, cumpliendo con las pautas del proyecto.
- Las funciones de protección como stop-loss o take-profit pueden agregarse mediante `StartProtection` si se desea; no forman parte de la implementación MQL original y por lo tanto se omiten aquí.
- Dado que la estrategia emite órdenes de mercado, asegúrese de contar con suficiente liquidez en el instrumento elegido para minimizar el deslizamiento.
