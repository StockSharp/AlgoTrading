# Estrategia de Sonido de Barra Grande
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Sonido de Barra Grande** reproduce el comportamiento del asesor experto de MetaTrader "BigBarSound". El algoritmo observa las velas terminadas de un marco temporal configurable y reporta cuando el rango de la vela es lo suficientemente amplio como para considerarse una "barra grande". En lugar de reproducir un archivo de audio, escribe mensajes de registro detallados que pueden enrutarse a cualquier subsistema de notificación compatible con StockSharp.

La estrategia es puramente informativa – no envía órdenes ni gestiona posiciones. Está diseñada para usarse como componente de alerta dentro de un flujo de trabajo de trading automatizado o discrecional más amplio.

## Comportamiento
1. La estrategia se suscribe a la serie de velas especificada por el parámetro **Tipo de vela**.
2. Para cada vela completada mide el tamaño de la barra según el **Modo de diferencia** seleccionado:
   - **OpenClose** – diferencia absoluta entre el precio de cierre y el de apertura.
   - **HighLow** – diferencia absoluta entre el máximo y el mínimo de la barra.
3. El valor medido se compara con el **Umbral de puntos** multiplicado por el `PriceStep` del instrumento. Cuando el tamaño de la barra es mayor o igual a este umbral, la estrategia registra una entrada de log que simula reproducir el archivo de sonido configurado.
4. Si **Mostrar alerta** está activado, se escribe un mensaje de alerta adicional para destacar el evento.

Dado que la implementación solo procesa velas terminadas, cada barra puede disparar como máximo una vez, replicando el comportamiento de disparo único del asesor experto MQL original.

## Parámetros
- **Umbral de puntos (`BarPoint`)** – número de pasos de precio que deben superarse antes de que se dispare una alerta. El valor predeterminado de 200 coincide con el script original. Se proporcionan límites de optimización (50–500 con paso 50) para mayor comodidad.
- **Modo de diferencia (`DifferenceMode`)** – selecciona cómo se mide el tamaño de la vela: distancia apertura/cierre o rango completo máximo/mínimo.
- **Archivo de sonido (`SoundFile`)** – nombre del archivo WAV que debería reproducirse. La estrategia solo registra este valor para emular la llamada `PlaySound` de MetaTrader.
- **Mostrar alerta (`ShowAlert`)** – cuando está activado, la estrategia emite un mensaje de log adicional para imitar el popup opcional `Alert` de la versión MQL.
- **Tipo de vela (`CandleType`)** – tipo de datos de vela (marco temporal) al que suscribirse. Por defecto la estrategia usa velas de 1 minuto.

## Alertas y registro
La estrategia usa `LogInfo` para anunciar que el archivo de sonido habría sido reproducido y `AddInfoLog` para proporcionar un mensaje de alerta separado. Estas entradas contienen el identificador del instrumento, la marca de tiempo de la vela y el tamaño medido de la barra, facilitando la integración con los visores de registro o receptores de notificaciones de StockSharp.

Si el broker no proporciona un `PriceStep` válido, se usa un valor de reserva de `1` para que la estrategia permanezca operativa. Ajuste el **Umbral de puntos** en consecuencia para reflejar el tamaño real del tick del instrumento.

## Notas de uso
- Adjunte la estrategia a cualquier instrumento que exponga datos de velas. La alerta funciona igualmente bien en forex, futuros, acciones o activos cripto.
- Combínela con otras estrategias de trading suscribiéndose a su salida de log o extendiendo la clase para reenviar eventos a manejadores personalizados.
- Dado que la implementación no genera órdenes, `Volume` y los parámetros relacionados con la posición se ignoran.
- Para producir notificaciones audibles, conecte el subsistema de registro de StockSharp a un notificador de sonido o extienda el código para llamar a APIs de audio específicas de la plataforma.

## Diferencias con el asesor experto MQL original
- El script original operaba con datos de ticks y rastreaba los cambios de barra manualmente. El port de StockSharp procesa directamente las velas terminadas, lo que garantiza exactamente una alerta por barra sin mantener un indicador de disparo separado.
- La reproducción de audio es reemplazada por mensajes de log para que el comportamiento permanezca multiplataforma dentro del entorno StockSharp.
- Los nombres de parámetros siguen las convenciones de StockSharp pero retienen la misma semántica: tamaño umbral en puntos, modo de medición, alerta opcional y nombre de sonido.

## Requisitos
No se requieren indicadores adicionales. Simplemente asegúrese de que el `CandleType` seleccionado sea compatible con la fuente de datos conectada para que la estrategia reciba velas completadas para procesar.
