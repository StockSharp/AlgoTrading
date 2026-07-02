# Estrategia del panel de intercambio de símbolos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de panel de intercambio de símbolos** es una conversión StockSharp del panel MQL *"Panel de intercambio de símbolos"*. El experto original actuó como un widget de gráfico que permitía a los operadores escribir un símbolo, cambiar el gráfico activo a ese símbolo y monitorear información de mercado en tiempo real, como valores OHLC, volumen de ticks y spread. La estrategia convertida recrea el mismo flujo de trabajo en el entorno StockSharp. Se puede iniciar con cualquier valor y proporciona un cambio manual para saltar a otro instrumento mientras registra continuamente las métricas de mercado más relevantes.

## Comportamiento central
- Se suscribe a datos de velas y cotizaciones de nivel uno para el valor activo.
- Registra cada vela completa con apertura, máximo, mínimo, cierre, volumen total y el último diferencial calculado.
- Almacena cotizaciones de oferta/demanda y obtiene un margen actualizado que refleja la lectura del panel MQL.
- Reacciona a solicitudes de intercambio manuales y reemplaza la seguridad monitoreada con el identificador elegido sin necesidad de reiniciar la estrategia.
- Mantiene la seguridad previamente seleccionada para que se ignoren los intercambios redundantes y las activaciones dobles accidentales no interrumpan las suscripciones.

## Parámetros
| Nombre | Tipo | Descripción |
| --- | --- | --- |
| `TargetSecurityId` | `string` | Identificador de seguridad que debe activarse cuando se activa la solicitud de intercambio. Las cadenas vacías se ignoran con una advertencia. |
| `CandleType` | `DataType` | Agregación de velas para actualizaciones periódicas (el valor predeterminado es velas de 1 hora, replicando el período de tiempo del panel MQL. |
| `SwapRequested` | `bool` | Marcador manual que solicita un cambio inmediato a `TargetSecurityId`. Se restablece a `false` después de que se procesa el intento de intercambio. |

## Suscripciones de datos
- Suscripción de vela creada con `CandleType` para la seguridad actualmente activa.
- Suscripción de nivel uno utilizada para realizar un seguimiento de las cotizaciones de oferta y demanda y calcular un valor de diferencial en vivo.
- Las suscripciones se reinician de forma segura cada vez que cambia la seguridad, lo que garantiza que no se dejen en ejecución flujos de datos obsoletos.

## Flujo de trabajo
1. Cuando la estrategia comienza resuelve la seguridad inicial desde `Strategy.Security` o, si falta, desde `TargetSecurityId`.
2. Se abren suscripciones de vela y de nivel uno para ese instrumento.
3. Cada vela completa activa un mensaje de registro detallado que refleja el texto que se muestra en las etiquetas originales del panel.
4. Las actualizaciones entrantes de nivel uno actualizan los valores de oferta/demanda almacenados en caché.
5. Configurar `SwapRequested` en `true` y proporcionar un `TargetSecurityId` válido cambia inmediatamente la seguridad monitoreada y reinicia las suscripciones.

## Notas de uso
- La estrategia está diseñada para seguimiento manual y no realiza pedidos.
- El diferencial solo se informa cuando los valores de oferta y demanda están presentes y son positivos.
- Cuando se proporciona un símbolo no válido o desconocido, se registra una advertencia y la solicitud se descarta sin interrumpir las suscripciones en ejecución.
- Debido a que la herramienta original actualizaba la interfaz de usuario una vez por segundo, puede reducir el período de tiempo de la vela si necesita actualizaciones de registro más frecuentes.

## Se conservan las características originales de MQL
- Cambio manual de símbolo a través de un identificador textual.
- Visualización en tiempo real de valores, volumen y extensión de OHLC para el símbolo elegido.
- Protege contra entradas vacías y adiciones fallidas de Market Watch (traducidas a advertencias StockSharp.

## Diferencias con la implementación MQL
- La estrategia StockSharp utiliza mensajes de registro en lugar de etiquetas en pantalla. Esto coincide con el flujo de trabajo típico dentro de StockSharp y al mismo tiempo expone la misma información.
- El cambio de gráfico se implementa reasignando la seguridad de la estrategia y recreando las suscripciones en lugar de alterar la ventana del gráfico del terminal.
- La lógica de actualización basada en temporizador se reemplaza por eventos de finalización de velas para mantenerse alineado con las API StockSharp de alto nivel.

## Requisitos
- Conector StockSharp con acceso a los valores deseados.
- Alimentación de datos de nivel uno para obtener cotizaciones de oferta y demanda para el cálculo del diferencial.
