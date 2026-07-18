# Estrategia ReInitChart
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta la utilidad **ReInitChart** de MetaTrader a StockSharp. El script original creaba un botón en cada gráfico que cambiaba temporalmente el período de tiempo para forzar el recálculo de los indicadores. La versión de StockSharp mantiene el mismo espíritu exponiendo un interruptor de actualización manual y un temporizador automático opcional que reinician el indicador SMA interno y registran el evento de actualización. Se aplica una regla simple de seguimiento de tendencia SMA para demostrar el trading una vez que el indicador se reconstruye.

## Cómo funciona

1. **Fuente de datos principal** – la estrategia se suscribe al período de tiempo definido por `CandleType` y calcula una media móvil simple con longitud `SmaLength`.
2. **Actualización manual** – cuando `ManualRefreshRequest` se convierte en `true`, el estado de la media móvil se reinicia, el indicador se borra y la acción se reporta en el registro junto con los metadatos del botón preservados (`RefreshCommandName`, `RefreshCommandText`, `TextColorName`, `BackgroundColorName`).
3. **Actualización automática** – habilitar `AutoRefreshEnabled` programa reinicios recurrentes cada `AutoRefreshInterval`, reproduciendo la reinicialización basada en temporizador de MetaTrader.
4. **Lógica de trading** – después de que se forme el SMA, la estrategia mantiene como máximo una posición. Va largo cuando el precio de cierre está por encima del SMA y cambia a corto cuando el precio cae por debajo, cerrando primero el lado opuesto.

Este comportamiento refleja la idea de reinicializar todos los gráficos del Asesor Experto original mientras usa componentes idiomáticos de StockSharp (reinicio del indicador y registro) en lugar de cambiar los períodos de tiempo del gráfico.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `CandleType` | Período de tiempo de trabajo para la suscripción de velas. |
| `SmaLength` | Número de velas usadas para la media móvil que se reconstruye después de cada actualización. |
| `AutoRefreshEnabled` | Habilita el temporizador de actualización periódica. |
| `AutoRefreshInterval` | Intervalo entre eventos de actualización automática. |
| `ManualRefreshRequest` | Establecer en `true` manualmente para activar una actualización inmediata. La estrategia lo borra después de procesarlo. |
| `RefreshCommandName` | Metadatos que reflejan el nombre del botón de MetaTrader; reportado en los registros cuando ocurre una actualización. |
| `RefreshCommandText` | Metadatos que reflejan el título del botón de MetaTrader; reportado en los registros cuando ocurre una actualización. |
| `TextColorName` | Descripción del color del texto del botón preservada del script MQL. |
| `BackgroundColorName` | Descripción del color de fondo del botón preservada del script MQL. |

## Uso

1. Configure `CandleType` y `SmaLength` para que coincidan con el mercado y el período de tiempo que desea monitorear.
2. Habilite `AutoRefreshEnabled` y elija `AutoRefreshInterval` si necesita reconstrucciones periódicas del indicador. Déjelo deshabilitado cuando quiera solo control manual.
3. Cambie `ManualRefreshRequest` a `true` cuando quiera vaciar el estado del indicador. El indicador se establece automáticamente de vuelta a `false` una vez que la actualización está registrada.
4. Inicie la estrategia para suscribirse a los datos del mercado. Dibuja velas, la curva SMA y sus propias operaciones en el gráfico, y ejecuta los trades básicos de seguimiento de tendencia SMA una vez que el indicador esté listo.

## Diferencias con el script MQL original

- StockSharp no expone botones de gráfico de la misma manera, por lo que el activador de actualización se implementa a través de parámetros de estrategia.
- En lugar de saltar entre los períodos de tiempo M1 y M5, el port de StockSharp reinicia sus indicadores directamente, lo que es más confiable dentro del framework.
- Las etiquetas y colores de los botones se conservan como metadatos para el registro para mantener un vínculo con la interfaz de MetaTrader aunque no se creen controles en el gráfico.
