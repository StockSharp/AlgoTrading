# Estrategia Trend Me Leave Me
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia **Trend Me Leave Me** es un port directo del clásico asesor experto MQL5 de Yury Reshetov. Espera pacientemente
períodos de acción de precio tranquila, se une a la dirección predominante indicada por el Parabolic SAR y alterna la
dirección de la operación después de salidas rentables. Cuando una operación es detenida, la estrategia intentará la
misma dirección de nuevo, recreando el comportamiento original "trend me, leave me". Esta implementación en C# usa la
API de alto nivel de StockSharp y conserva el flujo completo de decisiones del sistema fuente mientras expone cada entrada
numérica como un parámetro configurable.

## Ideas principales
### Filtro de mercado tranquilo
- El Average Directional Index (ADX) con longitud `AdxPeriod` mide la fuerza direccional.
- Solo cuando la media móvil del ADX cae por debajo de `AdxQuietLevel` la estrategia permite nuevas entradas, imitando
  el enfoque del EA en retrocesos de baja volatilidad.

### Alineación SAR para el timing
- Los puntos del Parabolic SAR actúan como guía direccional. Una señal larga requiere que el cierre de la vela esté
  por encima del punto SAR, mientras que una señal corta requiere un cierre por debajo del punto.
- Los parámetros `SarStep` y `SarMax` coinciden con la configuración de aceleración de la versión MQL y pueden optimizarse si es necesario.

### Programador de dirección
- Un flag `TradeDirections` representa la variable original `cmd`. Comienza en el estado *compra*.
- Después de una salida por **take-profit** el flag cambia al lado opuesto, invitando a una operación de reversión.
- Después de una salida por **stop-loss** (o break-even) el flag permanece en el mismo lado para que la próxima
  oportunidad reintente la dirección anterior.

## Gestión de operaciones
- `StopLossPips` y `TakeProfitPips` definen distancias fijas desde el precio promedio de ejecución. Establecer cualquier
  parámetro en `0` deshabilita la protección correspondiente.
- `BreakevenPips` mueve el stop al precio de entrada una vez que el mercado se desplaza a favor por la distancia de pips
  especificada. Si el precio luego regresa al nivel de entrada, la operación se cierra con ganancias aproximadamente nulas,
  lo que mantiene la próxima señal en el mismo lado.
- La lógica de stop/take se evalúa en cada vela completada usando tanto el máximo como el mínimo para aproximar los
  toques intrabarra, preservando el comportamiento tick a tick del EA lo más fielmente posible en un entorno basado en barras.

## Dimensionamiento de la posición
- El volumen de la orden es controlado por la propiedad base `Strategy.Volume`. El ejemplo mantiene el modelo de riesgo
  simple y no incluye el objeto de gestión de dinero de riesgo fijo del script MQL. Ajustar `Volume` o anular la estrategia
  si se requiere un dimensionamiento más avanzado.

## Parámetros
| Parámetro | Descripción | Por defecto |
|-----------|-------------|-------------|
| `StopLossPips` | Distancia en pips entre el precio de entrada y el stop de protección. | `50` |
| `TakeProfitPips` | Distancia en pips entre el precio de entrada y el objetivo. | `180` |
| `BreakevenPips` | Mover el stop a la entrada después de este número de pips de movimiento favorable. | `5` |
| `AdxPeriod` | Período de suavizado para el filtro ADX. | `14` |
| `AdxQuietLevel` | Lectura máxima de ADX que aún califica como mercado tranquilo. | `20` |
| `SarStep` | Paso de aceleración del Parabolic SAR. | `0.02` |
| `SarMax` | Factor de aceleración máximo del Parabolic SAR. | `0.2` |
| `CandleType` | Marco temporal usado para los cálculos. | Velas de `1h` |

## Notas de implementación
- Los cálculos de pips siguen el ajuste de dígitos del EA: si el instrumento usa 3 o 5 decimales, el paso de precio se
  multiplica por 10 para convertir el tamaño del tick del broker en un pip estándar.
- Los enlaces de indicadores se apoyan en la API de alto nivel de StockSharp, y todas las acciones de trading usan
  `BuyMarket`/`SellMarket` para mantenerse en línea con las convenciones de S#.
- Aún no se incluye traducción a Python. El directorio `PY/` está intencionalmente ausente según lo solicitado.
- Adjunte la estrategia a cualquier símbolo compatible con StockSharp. Establezca `Volume` antes de iniciar la estrategia
  y ajuste los parámetros para coincidir con la volatilidad del instrumento.
