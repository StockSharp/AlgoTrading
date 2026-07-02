# Estrategia de Aussie Surfer Ltd
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Aussie Surfer Ltd** es una versión API de alto nivel de StockSharp del asesor experto MetaTrader 5 "Aussie Surfer Ltd" (MQL carpeta `43278`). La estrategia combina rápidas reversiones de banda Bollinger con un filtro de tendencia Alligator para automatizar la configuración discrecional utilizada en el EA original. Las operaciones se realizan con el instrumento principal configurado para la estrategia y se evalúan en una serie de velas de 15 minutos de forma predeterminada.

## Indicadores y datos
- **Bollinger Bandas (precio de cierre, longitud predeterminada 5, ancho 2,5)**: detecta cuando el mercado se extiende temporalmente fuera de las bandas y vuelve a entrar.
- **Promedio móvil suavizado (longitud 21)**: reproduce la línea de "dientes" Alligator para juzgar la desaceleración de la tendencia.
- **Precio medio de cada vela ((Máximo + Mínimo) / 2)**: alimenta el cálculo Alligator para que la pendiente coincida con la implementación original.

La estrategia se suscribe a un único flujo de velas. Los valores del indicador son impulsados ​​únicamente por velas terminadas, lo que garantiza que las señales se generen a partir de datos confirmados.

## Lógica de trading
1. **Configuración de entrada**
   - Cuando la vela anterior se abrió por encima de la banda inferior Bollinger y la vela actual se abre por debajo del valor de la banda observado hace dos barras, se abre una posición **larga** (después de aplanar cualquier exposición corta). Esto recrea la lógica EA en la que el precio perfora la banda inferior e inmediatamente rebota hacia adentro.
   - Cuando la vela anterior se abrió por debajo de la banda superior Bollinger y la vela actual se abre por encima del valor de la banda observado hace dos barras, se abre una posición **corta** (después de aplanar cualquier exposición larga).
2. **Salida basada en Alligator**
   - La línea de dientes Alligator se monitorea una y dos barras atrás. Una posición larga se liquida siempre que la pendiente gira hacia abajo (el valor de hace dos barras es mayor que el valor de hace una barra). Una posición corta se cierra cuando la pendiente aumenta.
3. **Capas de riesgo**
   - Al entrar se aplican un stop-loss y una take-profit de pips fijos. Ambos son opcionales y se pueden desactivar estableciendo su distancia de pip en cero.
   - Un trailing stop opcional realinea el stop-loss con el máximo (para largos) o mínimo (para cortos) de la vela previamente completada menos/más la distancia de pips configurada. La lógica de seguimiento solo está activa si el stop-loss está habilitado y `EnableTrailingStop` está establecido en `true`.

## Gestión del riesgo
- **Stop-loss**: convierte la distancia de pip configurada en unidades de precio utilizando el paso del precio del valor.
- **Take-profit**: se calcula una vez al momento de la entrada y se mantiene estático hasta que se alcanza o la posición se cierra mediante otra regla.
- **Trailing stop**: avanza el stop-loss cuando aparece un máximo (para largos) o un mínimo (para cortos) más favorable en la vela anterior.
- **Manejo de reversión**: si llega una señal mientras una posición opuesta está abierta, la estrategia envía una orden de mercado de tamaño para revertir completamente y establecer la nueva exposición en una sola transacción.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `OrderVolume` | Tamaño base del comercio en lotes o contratos. | `0.30` |
| `StopLossPips` | Distancia de parada de protección en pips. `0` desactiva la parada. | `46` |
| `TakeProfitPips` | Distancia objetivo de ganancias en pips. `0` desactiva el objetivo. | `0` |
| `EnableTrailingStop` | Habilita el seguimiento basado en pips cuando un stop-loss está activo. | `true` |
| `BollingerPeriod` | Longitud de la ventana Bollinger Bandas. | `5` |
| `BollingerDeviation` | Multiplicador de desviación estándar para las bandas. | `2.5` |
| `TeethPeriod` | Longitud media móvil suavizada para la línea de dientes Alligator. | `21` |
| `CandleType` | Serie de velas utilizadas para los cálculos (período de tiempo de 15 minutos por defecto). | `15m` velas |

Todos los parámetros numéricos incluyen metadatos de optimización para que puedan ajustarse a través del Analizador de estrategias.

## Notas de implementación
- Sólo se procesan velas completas; los datos sin terminar se ignoran para imitar la ejecución controlada por temporizador MetaTrader que se ejecutó al comienzo de cada nueva barra.
- La lógica de seguimiento requiere una distancia de stop-loss positiva. Se produce una excepción durante la inicialización si la opción de seguimiento está habilitada sin parada.
- Las instancias de indicador se dibujan automáticamente cuando hay un área de gráfico disponible, lo que ayuda a validar que el puerto StockSharp coincida con la plantilla MetaTrader.

## Uso
1. Cargue la estrategia en una terminal StockSharp o en un entorno de backtesting.
2. Configure la seguridad comercial y ajuste los parámetros (especialmente las distancias de pips) para que coincidan con las especificaciones del contrato del corredor.
3. Inicia la estrategia. Se suscribirá a la serie de velas configuradas, evaluará las entradas en cada vela terminada y gestionará la posición utilizando las reglas descritas.

Para operaciones en vivo, asegúrese de que el corredor admita órdenes de mercado y que el símbolo `PriceStep` esté disponible para que las conversiones de pips sean precisas.
