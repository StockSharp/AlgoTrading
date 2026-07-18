# Estrategia de MA con Protección de Profundo Drawdown
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia de MA con Protección de Profundo Drawdown es una conversión directa del asesor experto de MetaTrader 5 "Deep Drawdown MA (barabashkakvn's edition)" a la API de alto nivel de StockSharp. La estrategia opera cruces de medias móviles mientras aplica un mecanismo de punto de equilibrio diseñado para proteger las operaciones que han caído en drawdown. La versión de StockSharp conserva los parámetros configurables de media móvil, la capacidad de limitar el número de entradas agregadas y la opción de liquidar inmediatamente las operaciones perdedoras en una inversión de señal.

## Lógica de operación
- **Indicadores**: Dos medias móviles con períodos individuales, fuentes de precio y desplazamientos históricos. Ambas medias comparten el mismo método de suavizado (SMA, EMA, SMMA o LWMA).
- **Condiciones de entrada**:
  - **Largo**: La media rápida desplazada sube por encima de la media lenta desplazada. La estrategia añade el volumen de orden configurado (y cubre cualquier exposición corta) cuando la última entrada no fue larga y no se supera el límite máximo de posición.
  - **Corto**: La media rápida desplazada cae por debajo de la media lenta desplazada. La estrategia vende el volumen configurado (y cubre cualquier exposición larga) cuando la entrada anterior no fue corta y el límite máximo de posición lo permite.
- **Condiciones de salida**:
  - **Largos**: Cuando la media rápida cruza de vuelta por debajo de la media lenta, la posición se cierra inmediatamente (`CloseLosses = true`) o se marca para una salida de punto de equilibrio. Durante una salida de punto de equilibrio, la estrategia espera hasta que el cierre de vela vuelva al precio de entrada promedio antes de aplanar.
  - **Cortos**: Comportamiento espejado: en un cruce alcista, la posición se cierra instantáneamente o se arma con un objetivo de punto de equilibrio que se activa una vez que el precio regresa a la entrada promedio.
- **Seguimiento de posición**: El precio de entrada promedio y la última dirección abierta se reconstruyen a partir de las propias operaciones para que la API de alto nivel pueda reproducir el comportamiento MQL.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `OrderVolume` | Tamaño de orden para cada operación de mercado. | 0.1 |
| `MaxPositions` | Número máximo de lotes agregados por dirección (exposición neta). | 5 |
| `CloseLosses` | Cerrar operaciones perdedoras inmediatamente en una inversión en lugar de esperar el punto de equilibrio. | false |
| `FastMaPeriod` / `SlowMaPeriod` | Longitud de las medias móviles rápida y lenta. | 10 / 30 |
| `FastMaShift` / `SlowMaShift` | Desplazamiento histórico aplicado a cada media móvil (emula el argumento shift de MT5). | 3 / 0 |
| `FastPriceType` / `SlowPriceType` | Fuente de precio usada por cada media móvil (Close, Open, High, Low, Median, Typical, Weighted). | Close |
| `MaMethod` | Método de suavizado compartido por ambas medias (SMA, EMA, SMMA, LWMA). | SMA |
| `CandleType` | Serie de velas usada para los cálculos. | Velas de 15 minutos |

## Notas de conversión
- El robot original de MetaTrader podía mantener posiciones largas y cortas cubiertas simultáneamente. Las estrategias de StockSharp operan sobre posiciones netas; por tanto, la versión convertida aplica la exposición agregada respetando aún el conteo máximo de posiciones.
- La protección de punto de equilibrio se implementa con indicadores internos en lugar de modificaciones de órdenes de MT5. La estrategia monitorea los cierres de velas y sale al precio de entrada promedio reconstruido.
- Los parámetros de "desplazamiento" de media móvil se reproducen manteniendo una cola corta de valores de indicador recientes, lo que refleja el argumento `shift` de MT5 sin llamar a buffers de indicador de bajo nivel.

## Uso
1. Adjunte la estrategia al instrumento deseado y establezca `OrderVolume`, el tipo de vela y los parámetros de media móvil para que coincidan con su mercado objetivo.
2. Habilite el trading una vez que la estrategia esté en ejecución y la suscripción de velas esté activa.
3. Monitoree los indicadores de punto de equilibrio a través de los registros: las operaciones se aplanarán automáticamente una vez que el precio regrese a la entrada promedio.

## Gestión de riesgo
- Use `CloseLosses = true` para forzar la liquidación rápida de operaciones perdedoras cuando las medias se invierten.
- Ajuste `MaxPositions` para limitar la exposición agregada después de entradas alternativas consecutivas.
- Combine la estrategia con los controles de riesgo a nivel de cuenta disponibles en StockSharp (p. ej., `StartProtection`) para salvaguardas adicionales.

## Archivos
- `CS/DeepDrawdownMaStrategy.cs` – Implementación en C# usando la API de alto nivel de StockSharp.
- `README.md`, `README_ru.md`, `README_zh.md` – Documentación multilingüe del comportamiento y parámetros de la estrategia.
