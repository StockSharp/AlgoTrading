# Estrategia de Horizontal Line Levels
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Horizontal Line Levels** emula el asesor experto de MetaTrader 5 del mismo nombre. Reconstruye continuamente dos niveles de precio alrededor de la cotización actual y notifica al usuario una vez que el mercado los cruza. La implementación se basa en datos de mercado Level1 (bid/ask), imitando el flujo de trabajo OnTick/OnTimer original sin enviar ninguna orden.

## Idea central

1. Suscribirse a los datos Level1 y almacenar en caché los últimos precios de mejor bid y mejor ask.
2. Convertir la distancia de puntos de MetaTrader a la escala de precios de StockSharp.
3. Desplazar el mejor ask hacia arriba y el mejor bid hacia abajo por la distancia configurada, creando dos líneas horizontales virtuales.
4. Verificar periódicamente (mediante un temporizador interno) si el bid o ask cruza esos niveles de referencia y registrar alertas en el diario de la estrategia.

## Parámetros

| Nombre | Por defecto | Descripción |
| --- | --- | --- |
| `TimerPeriodMinutes` | `1` | Minutos entre dos verificaciones consecutivas del temporizador. Debe permanecer positivo. |
| `OffsetPoints` | `50` | Distancia en puntos de MetaTrader aplicada por encima del ask y por debajo del bid al construir las líneas. |

## Detalles de comportamiento

- **Suscripción de datos**: `GetWorkingSecurities` registra un flujo Level1 para que la estrategia reciba actualizaciones de bid/ask incluso sin velas.
- **Inicialización**: La primera vez que tanto el mejor bid como el mejor ask están disponibles, `RecalculateLevels` almacena los niveles horizontales actuales superior e inferior.
- **Temporizador**: Cada tick del temporizador recrea los niveles faltantes (si la inicialización ocurrió antes de que las cotizaciones estuvieran listas) y emite mensajes de registro una vez que el mercado viola cualquiera de los límites.
- **Traducción de puntos de MetaTrader**: El helper `EnsurePointSize` convierte los "puntos" de MetaTrader en incrementos de precio absolutos usando `Security.PriceStep`. La misma técnica se usa en otras estrategias convertidas para mantener la compatibilidad numérica.
- **Sin trading**: La estrategia nunca envía órdenes; solo produce alertas a través de `AddInfoLog`. Esto coincide con el experto original que mostraba alertas emergentes cuando el precio tocaba cualquiera de las líneas.
- **Detención/Restablecimiento**: Detener la estrategia cancela el temporizador y borra todos los valores en caché para que la próxima ejecución comience desde un estado limpio.

## Uso típico

1. Adjunte la estrategia al instrumento deseado y establezca `TimerPeriodMinutes` y `OffsetPoints` en la UI de Designer.
2. Inicie la estrategia. Una vez que llega una instantánea completa de cotización, una entrada de registro como `Horizontal levels updated. Upper: 1.12345, Lower: 1.12245.` confirma los umbrales calculados.
3. Observe la ventana de registro. Cuando el ask sube por encima del nivel superior (o el bid cae por debajo del nivel inferior), la estrategia imprime el mensaje de alerta correspondiente.
4. Si cambia el offset o reinicia la estrategia, los niveles se recalculan usando los nuevos parámetros.

## Clasificación

- **Categoría**: Utilidades / Alertas
- **Dirección**: Ninguno
- **Estilo de ejecución**: Monitoreo basado en eventos
- **Requisitos de datos**: Level1 bid/ask
- **Complejidad**: Básico
- **Marco temporal recomendado**: Cualquiera (puramente basado en cotizaciones)
- **Gestión de riesgos**: No aplica (no se abren posiciones)

Esta conversión mantiene el comportamiento centrado en alertas del original de MetaTrader mientras aprovecha las abstracciones de StockSharp como temporizadores de estrategia y suscripciones Level1.
