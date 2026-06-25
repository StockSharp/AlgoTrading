# Estrategia del Sistema Indicador Vortex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- **Fuente**: Convertido del asesor experto de MetaTrader 5 "Vortex Indicator System" (MQL ID 19137).
- **Concepto**: Usa el indicador Vortex para detectar cruces alcistas o bajistas y luego arma disparadores de ruptura en el máximo/mínimo de la vela del cruce.
- **Estilo de ejecución**: Seguimiento de ruptura; las operaciones se inician sólo después de que el precio confirma el cruce superando el nivel del disparador.
- **Régimen de mercado**: Funciona en cualquier instrumento y marco temporal que admita el indicador Vortex y datos de velas; no se requieren características específicas del bróker.
- **Tipos de órdenes**: Órdenes de mercado mediante `BuyMarket` y `SellMarket`. La estrategia cierra automáticamente posiciones opuestas antes de poner en cola un nuevo disparador.

## Lógica de trading
1. Suscribirse al tipo de vela configurado y calcular el indicador Vortex con la longitud especificada.
2. Detectar un cruce alcista cuando `VI+` se mueve por encima de `VI-` después de estar por debajo en la vela anterior:
   - Cerrar cualquier posición corta existente usando `ClosePosition()`.
   - Almacenar el máximo de la vela del cruce como el precio de disparador largo.
   - Cancelar cualquier disparador corto pendiente.
3. Detectar un cruce bajista cuando `VI-` se mueve por encima de `VI+` después de estar por debajo en la vela anterior:
   - Cerrar cualquier posición larga existente.
   - Almacenar el mínimo de la vela del cruce como el precio de disparador corto.
   - Cancelar cualquier disparador largo pendiente.
4. Mientras un disparador está activo, monitorear las velas posteriores:
   - Si el precio máximo rompe el disparador largo almacenado y la posición actual es plana o corta, enviar una compra de mercado dimensionada para revertir cualquier exposición corta.
   - Si el precio mínimo rompe el disparador corto almacenado y la posición actual es plana o larga, enviar una venta de mercado dimensionada para revertir cualquier exposición larga.
5. Cada operación ejecutada borra su disparador correspondiente. Los disparadores opuestos son mutuamente excluyentes.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `Length` | 14 | Período del indicador Vortex. Corresponde a la entrada MQL original `VI_Length`. |
| `CandleType` | Marco temporal de 60 minutos | Tipo de vela usado para el cálculo del indicador y la evaluación del disparador. Puede ajustarse a cualquier marco temporal admitido por la fuente de datos conectada. |
| `Volume` | Tomado de la propiedad base `Strategy` | Volumen de operación usado para las órdenes de mercado. Configúralo antes de iniciar la estrategia si se requiere un valor diferente a 1 contrato/lote. |

### Cómo los parámetros afectan el comportamiento
- Aumentar `Length` suaviza las líneas Vortex, reduciendo el número de cruces pero mejorando su confiabilidad.
- Disminuir `Length` hace el sistema más reactivo, generando más disparadores y operaciones potenciales.
- El `CandleType` debe alinearse con la granularidad de datos en la configuración MQL original (típicamente el marco temporal del gráfico). Las velas más cortas proporcionan señales más rápidas, mientras que las velas más largas se centran en tendencias más amplias.

## Notas de gestión de riesgo
- El asesor experto original no define niveles de stop-loss o take-profit. Esta conversión mantiene ese comportamiento; la gestión de riesgo debe manejarse externamente o extendiendo la estrategia.
- La reversión de posición es inmediata: cuando ocurre una señal opuesta, la estrategia emite `ClosePosition()` y espera un rompimiento más allá del disparador antes de entrar en la nueva dirección.
- Solo puede estar activo un disparador (largo o corto) a la vez. Los disparadores se borran si el precio los rompe o cuando ocurre un cruce opuesto.

## Instrucciones de uso
1. Agrega la estrategia a tu proyecto StockSharp y asegúrate de que el paquete `StockSharp.Algo.Indicators` esté disponible.
2. Configura el instrumento deseado y el conector en la aplicación anfitriona.
3. Establece el parámetro `CandleType` al marco temporal que deseas operar. Debe corresponder a una suscripción de velas disponible para el instrumento seleccionado.
4. Opcionalmente ajusta `Length` y `Volume` antes de iniciar la estrategia o a través de optimización.
5. Inicia la estrategia. Se generarán órdenes una vez que el indicador esté formado y los datos en tiempo real estén disponibles.

## Aspectos destacados de implementación
- Usa la API de alto nivel `SubscribeCandles` con vinculación de indicador (`Bind`) para un procesamiento limpio basado en eventos.
- Almacena los valores anteriores del Vortex para detectar cruces exactamente como lo hace la implementación MQL (comparaciones `VI+` y `VI-` entre dos velas consecutivas).
- Los disparadores de entrada se implementan como campos decimal anulables para imitar el mecanismo original de "armar y romper".
- Los comentarios en línea en inglés en el archivo C# describen cada paso de decisión y ayudan a mantener el código.

## Posibles extensiones
- Agregar reglas de stop-loss y take-profit (p. ej., salidas basadas en ATR) si se requiere un control de riesgo más estricto.
- Introducir un período de enfriamiento o tiempo máximo de mantenimiento para evitar períodos planos prolongados cuando los disparadores no se ejecutan.
- Combinar con un filtro de volatilidad para operar sólo cuando los rangos de precio sean suficientemente amplios para justificar intentos de ruptura.
