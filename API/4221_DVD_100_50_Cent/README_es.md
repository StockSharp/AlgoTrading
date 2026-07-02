# DVD Estrategia de 100-50 céntimos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia DVD de 100-50 centavos es un sistema de orden límite contrario adaptado del asesor experto MT4 original. La lógica evalúa el mercado en cuatro períodos de tiempo (M1, M30, H1, D1) y califica las configuraciones potenciales antes de estacionar órdenes límite de compra o venta alrededor de la cuadrícula de precios de "100 niveles" más cercana. Cuando se ejecuta la orden límite, la estrategia gestiona la posición con niveles de stop-loss y take-profit precalculados.

## Indicadores y datos
- **RAVI (Índice de verificación de acción de rango)** en H1 y D1, calculado con SMA(2) y SMA(24) sobre el precio de apertura.
- **Datos de velas sin procesar** en M1, M30 y H1 para filtros de patrones como rechazo de picos, comprobaciones de consolidación y pruebas de impulso.
- **Redondeo de la cuadrícula de precios** que ajusta el precio actual al nivel 100 más cercano usando un redondeo de dos decimales y un desplazamiento configurable de 0,1 pips.

## Lógica de entrada
1. Calcule el precio redondeado "Nivel 100" redondeando el último M1 cerca de dos decimales y desplazándolo en `PointFromLevelGoPips` (predeterminado 50 → 5 pips).
2. Inicialice una puntuación interna (BAL) en 0 y sume/reste puntos según:
   - **Filtro de tendencia:** agregue 10 puntos cuando H1 RAVI esté por debajo de cero para configuraciones largas o por encima de cero para configuraciones cortas.
   - **Confirmación de pico por hora:** agregue 7 puntos cuando los dos máximos/mínimos del primer semestre anteriores superen la cuadrícula en `RiseFilterPips`.
   - **Alineación de la estructura:** agregue 45 puntos cuando el cierre actual de M1 vuelva a cruzar el nivel y los últimos tres mínimos/máximos de H1 permanezcan por encima/por debajo del colchón de seguridad (`PointFromLevelGoPips ± 30 * 0.1 pip`).
   - **Guardias de volatilidad:** reste 50 puntos si los máximos/mínimos recientes de M1 exceden `HighLevelPips` (predeterminado 600 → 60 pips) o si aparecen ráfagas rápidas de impulso mientras el D1 RAVI confirma un fuerte régimen direccional.
   - **Confirmación de ruptura:** reste 50 puntos si las últimas 15 velas H1 nunca cruzaron el umbral `LowLevel2Pips`.
   - **Filtro de consolidación:** reste 50 puntos si las últimas ocho velas M30 permanecen dentro de la banda `LowLevelPips`.
3. Realice una orden limitada solo cuando la puntuación final sea de al menos 50 y no exista otra exposición (posición u orden pendiente).

## Colocación de pedidos
- **Límite de compra:** 10 pips por debajo del último cierre de M1. El stop-loss está `StopLossPips` por debajo del precio límite, la toma de ganancias está `TakeProfitPips` por encima de él. Cuando el D1 RAVI muestra una escalera ascendente entre -1 y +5 durante los últimos cuatro días, la toma de ganancias recibe una extensión adicional de 25 pips.
- **Límite de venta:** 7 pips por encima del último cierre de M1 con reglas de objetivo y stop simétricas. Cuando el D1 RAVI muestra una escalera descendente entre -5 y -1, el objetivo se extiende 25 pips.
- Los pedidos pendientes caducan automáticamente después de `OrderExpiryMinutes` (20 minutos predeterminado). Cuando se cancela una orden, los niveles de protección almacenados se restablecen.

## Gestión de Puestos
- Una vez completada, la estrategia mantiene internamente los valores almacenados de stop-loss y take-profit y emite órdenes de salida del mercado cuando el precio toca cualquiera de los niveles.
- No se aplica ningún trailing stop en la versión portada; el EA original deshabilitó la lógica final de forma predeterminada.
- Las nuevas operaciones se bloquean mientras exista una posición activa o una orden límite pendiente.

## Gestión monetaria
- Cuando `UseMoneyManagement` está habilitado, el tamaño del lote imita la implementación de MT4: escala en `TradeSizePercent` del capital actual, se ajusta para cuentas mini y fija el resultado en `[0.1, MaxVolume]` (mini) o `[1, MaxVolume]` (estándar).
- Deshabilitar la administración del dinero fuerza un volumen fijo controlado por el parámetro `FixedVolume`.
- La negociación se detiene cuando el capital de la cartera cae por debajo de `MarginCutoff`.

## Parámetros
| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `AccountIsMini` | Utilice reglas de redondeo de volumen de minicuentas | `true` |
| `UseMoneyManagement` | Habilitar el tamaño de lote adaptable | `true` |
| `TradeSizePercent` | Porcentaje de capital asignado por operación | `10` |
| `FixedVolume` | Volumen utilizado cuando la administración del dinero está desactivada | `0.01` |
| `MaxVolume` | Volumen comercial máximo permitido | `4` |
| `StopLossPips` | Distancia de stop-loss en pips | `210` |
| `TakeProfitPips` | Distancia de toma de ganancias en pips | `18` |
| `PointFromLevelGoPips` | Cambio de nivel base en 0,1 pips | `50` |
| `RiseFilterPips` | Distancia de confirmación de pico por hora (0,1 pips) | `700` |
| `HighLevelPips` | Umbral de rechazo de picos de un minuto (0,1 pips) | `600` |
| `LowLevelPips` | Banda de consolidación de 30 minutos (0,1 pips) | `250` |
| `LowLevel2Pips` | Distancia de confirmación de ruptura por hora (0,1 pips) | `450` |
| `MarginCutoff` | El suelo de la renta variable inhabilita nuevas operaciones | `300` |
| `OrderExpiryMinutes` | Duración del pedido pendiente en minutos | `20` |

## Notas de uso
- La conversión se basa en las velas terminadas de cada período; asegúrese de que el flujo de datos históricos proporcione velas M1, M30, H1 y D1 sincronizadas.
- El stop y el objetivo de protección se ejecutan con órdenes de mercado para reflejar el comportamiento MT4 de los valores SL/TP adjuntos.
- Debido a que la lógica es sensible al tamaño del pip, verifique que las propiedades `PriceStep` y `Decimals` del instrumento describan correctamente el formato de cotización.
