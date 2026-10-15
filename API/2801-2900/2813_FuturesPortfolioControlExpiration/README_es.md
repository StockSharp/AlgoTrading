# Estrategia de Control de Cartera de Futuros con Expiración
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia reconstruye el asesor experto de MetaTrader 5 *Futures Portfolio Control Expiration* sobre la API de alto nivel de StockSharp. Mantiene una cartera de futuros de tres tramos, conserva la exposición largo/corto deseada para cada tramo y rota automáticamente cada contrato al siguiente vencimiento cuando la vida útil restante cae por debajo de un umbral configurable.

La implementación replica el flujo de trabajo original:
1. Identificar el contrato actualmente negociable para cada familia de futuros basándose en un código corto (por ejemplo `MXI` o `BR`).
2. Abrir o ajustar la posición para que el volumen real del portafolio coincida con el valor de lote configurado (positivo = largo, negativo = corto).
3. Monitorear el tiempo de vencimiento en cada vela finalizada de una suscripción de latidos.
4. Cerrar el contrato que vence, descubrir el siguiente vencimiento en la misma familia y recrear la exposición objetivo en el nuevo contrato.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `BoardCode` | Tablero de intercambio adjunto a los identificadores de futuros (por ejemplo `FORTS`). Dejar vacío si el proveedor no requiere un sufijo de tablero. | `FORTS` |
| `Symbol1`, `Symbol2`, `Symbol3` | Códigos cortos de las tres familias de futuros. La estrategia itera los vencimientos de futuros construyendo identificadores como `CODE-M.YY`. | `MXI`, `BR`, `SBRF` |
| `Lot1`, `Lot2`, `Lot3` | Tamaño de posición objetivo por tramo. Los valores positivos crean exposición larga, los negativos crean exposición corta. | `-4`, `-1`, `5` |
| `HoursBeforeExpiration` | Número de horas antes del vencimiento del contrato cuando debe comenzar la rotación. | `25` |
| `MonitoringCandleType` | Tipo de vela utilizado solo como latido para activar las verificaciones de vencimiento (por ejemplo velas por hora). | Período `1H` |

## Gestión de rotación y posición
- **Descubrimiento de contratos.** Para cada tramo la estrategia escanea hasta doce meses consecutivos del calendario. Prueba múltiples formatos de identificador (`CODE-M.YY`, `CODE-MM.YY`, `CODEMMYY`, `CODEMYY`) y opcionalmente agrega el `BoardCode` configurado. Solo los instrumentos con una fecha de vencimiento posterior al tiempo de referencia son elegibles.
- **Actualizaciones de latido.** Una suscripción de velas en cada contrato activo proporciona una devolución de llamada de vela finalizada que re-evalúa los temporizadores de vencimiento y sincroniza la exposición del portafolio.
- **Lógica de rotación.** Cuando la vida útil restante es menor o igual a `HoursBeforeExpiration`, la estrategia cierra cualquier posición abierta en el contrato actual, localiza el siguiente futuro con un vencimiento posterior, se re-suscribe a las velas de latido y restaura el lote objetivo en el nuevo contrato.
- **Sincronización de posición.** Después de cada latido, la posición real se compara con el lote objetivo. La estrategia aumenta o disminuye la exposición con órdenes de mercado para que la posición en vivo siempre coincida con el volumen solicitado (incluido cero).

## Notas de uso
1. Asegúrese de que el `SecurityProvider` conozca todos los símbolos de futuros para las familias seleccionadas. Configure `BoardCode` si su fuente de datos requiere identificadores como `Si-9.23@FORTS`.
2. Inicie la estrategia con los parámetros de portafolio deseados. Las posiciones se abren solo cuando la estrategia está en línea y el trading está permitido.
3. La estrategia registra cada asignación, ajuste y evento de rotación. Use estos mensajes para verificar el mapeo entre códigos cortos y futuros reales.
4. Dado que la suscripción de latido es solo un temporizador, puede elegir cualquier tipo de vela que esté disponible de manera consistente para los instrumentos negociados.

## Detalles de implementación
- Los componentes de la API de alto nivel (`SubscribeCandles`, `StrategyParam`, `BuyMarket`/`SellMarket`) mantienen el código conciso y adhieren a las pautas del proyecto.
- No se almacenan colecciones personalizadas de datos históricos; la estrategia solo trabaja con el último evento de vela y el estado de la posición.
- Los comentarios en inglés dentro del código describen cada paso importante para un mantenimiento más sencillo.
