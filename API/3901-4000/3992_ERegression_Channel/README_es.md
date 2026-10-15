# Estrategia del canal de regresión E
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de canal de regresión E** reproduce el asesor experto "e-Regr" de MetaTrader utilizando la estrategia de alto nivel de StockSharp API. Ajusta una curva de regresión polinómica a los precios de cierre recientes, construye bandas equidistantes a partir de la desviación estándar residual y reacciona cuando el precio traspasa esas envolventes. La estrategia está diseñada para operaciones de reversión a la media con paradas protectoras opcionales, un filtro de volatilidad diario y una ventana de operaciones intradía.

## Lógica de trading
1. Suscríbase al período de tiempo principal especificado por `Candle Type` y calcule un canal de regresión polinómica en los últimos cierres de `Regression Length`.
2. La banda media es el ajuste de regresión; las bandas superior e inferior se desplazan `Std Dev Multiplier` multiplicado por la desviación estándar residual.
3. Cierre cualquier posición larga existente cuando el cierre de la vela cruce por encima de la banda media; cerrar posiciones cortas cuando el cierre cae por debajo de él.
4. Abra una posición larga (después de cerrar cualquier exposición corta existente) cuando el mínimo de la vela actual toque o rompa la banda inferior.
5. Abra una posición corta (después de aplanar la exposición larga) cuando el máximo de la vela actual toque o rompa la banda superior.
6. Opcionalmente, siga las posiciones abiertas usando `Trailing Activation` y `Trailing Distance` una vez que el precio se mueva lo suficiente a favor de la operación.
7. Omita nuevas entradas siempre que el rango de la vela diaria anterior supere el umbral `Daily Range Filter` o la hora actual esté fuera de la ventana `[Trade Start, Trade End)`.

## Parámetros
- `Volume`: tamaño de orden utilizado para cada entrada al mercado (las posiciones netas se aplanan antes de revertirse).
- `Trade Start` / `Trade End`: ventana de negociación diaria, admite rangos nocturnos (por ejemplo, de 21:00 a 02:00).
- `Regression Length`: número de velas utilizadas para el ajuste de regresión polinómica.
- `Degree` – grado polinómico (1–6) aplicado al modelo de regresión.
- `Std Dev Multiplier` – multiplicador aplicado a la desviación estándar residual de la regresión para formar las bandas.
- `Enable Trailing`: alterna la gestión de trailing stop.
- `Trailing Activation`: número de puntos de movimiento favorable necesarios antes de que comience el seguimiento.
- `Trailing Distance`: búfer de seguimiento que se mantiene una vez que el seguimiento está activo (en puntos).
- `Stop Loss` – distancia de parada de protección en puntos (0 desactiva la parada automática).
- `Take Profit` – distancia del objetivo de beneficio protector en puntos (0 desactiva el objetivo automático).
- `Daily Range Filter` – rango máximo permitido de la vela diaria anterior, expresado en puntos.
- `Candle Type`: período de tiempo para la serie de precios principal (período de tiempo predeterminado de 30 minutos).

## Configuración predeterminada
- `Volume` = 0,1
- `Trade Start` = 03:00
- `Trade End` = 21:20
- `Regression Length` = 250 barras
- `Degree` = 3
- `Std Dev Multiplier` = 1,0
- `Enable Trailing` = falso
- `Trailing Activation` = 30 puntos
- `Trailing Distance` = 30 puntos
- `Stop Loss` = 0 puntos (deshabilitado)
- `Take Profit` = 0 puntos (deshabilitado)
- `Daily Range Filter` = 150 puntos
- `Candle Type` = velas de 30 minutos

## Notas adicionales
- La estrategia utiliza la última vela terminada para todas las decisiones y nunca opera varias veces dentro de la misma barra.
- Los stop dinámicos cierran posiciones por mercado cuando el precio toca el nivel móvil calculado internamente.
- Si el día anterior es demasiado volátil (rango por encima del filtro configurado), las posiciones existentes se cierran y las nuevas entradas se suspenden durante el resto de la barra.
- El canal de regresión se vuelve a dibujar en el gráfico en cada actualización para ayudar a visualizar las bandas media, superior e inferior.
