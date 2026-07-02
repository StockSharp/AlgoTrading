# Estrategia experta de IMF ALD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de IMF experta en ALD** replica el MetaTrader 5 asesores expertos "Expert_AML_MFI" utilizando la API de alto nivel de StockSharp. Se centra en el patrón de velas *Meeting Lines* y valida cada señal con el oscilador **Money Flow Index (MFI)**. La estrategia mantiene automáticamente las estadísticas de velas necesarias, identifica reversiones alcistas o bajistas y gestiona las posiciones abiertas cada vez que la IMF cruza los umbrales de sobreventa o sobrecompra.

## Lógica de trading
1. **Preparación de velas**: la estrategia se suscribe al período de tiempo seleccionado (H1 por defecto) y mantiene las dos últimas velas completadas junto con el promedio móvil de los cuerpos de las velas. El tamaño medio del cuerpo se calcula mediante un `SimpleMovingAverage` aplicado al tamaño absoluto del cuerpo de la vela, reflejando la implementación de MT5.
2. **Detección de patrones**: dos ayudantes especializados reconocen *Líneas de encuentro alcistas* y *Líneas de encuentro bajistas*:
   - Configuración alcista: una vela bajista larga seguida de una vela alcista larga que cierra cerca del cierre anterior (dentro del 10% del cuerpo promedio).
   - Configuración bajista: una vela alcista larga seguida de una vela bajista larga con precios de cierre similares.
3. **Confirmación de MFI**: el valor de MFI anterior debe estar por debajo del nivel de entrada alcista (predeterminado 40) para operaciones largas o por encima del nivel de entrada bajista (predeterminado 60) para operaciones cortas.
4. **Gestión de posición** – se realiza un seguimiento de las dos últimas lecturas del MFI para detectar cruces de los niveles de sobreventa (30) y sobrecompra (70):
   - Un cruce por encima de cualquiera de los niveles sale de las posiciones cortas.
   - Un cruce por debajo del nivel de sobreventa o por encima del nivel de sobrecompra sale de las posiciones largas.
5. **Ejecución de la orden**: cuando se produce un patrón válido y una confirmación de la MFI, la estrategia cierra cualquier exposición opuesta y abre una nueva posición en el mercado con el volumen base configurado.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Plazo utilizado para la suscripción de velas. | plazo de 1 hora |
| `MfiPeriod` | Número de barras del oscilador MFI. | 12 |
| `BodyAveragePeriod` | Longitud de la ventana para el cálculo del tamaño corporal promedio. | 4 |
| `BullishEntryLevel` | Valor máximo de MFI permitido para entradas alcistas. | 40 |
| `BearishEntryLevel` | Valor mínimo de MFI requerido para entradas bajistas. | 60 |
| `OversoldLevel` | Nivel de sobreventa utilizado para señales de salida. | 30 |
| `OverboughtLevel` | Nivel de sobrecompra utilizado para señales de salida. | 70 |
| `TradeVolume` | Volumen de orden base aplicado a nuevas operaciones. | 1 |

Todos los parámetros se pueden optimizar directamente dentro de StockSharp Designer gracias a los contenedores `StrategyParam`.

## Indicadores y visuales
- **Índice de flujo de dinero**: vinculado a la suscripción de vela para su confirmación y mostrado en el gráfico cuando un área del gráfico está disponible.
- **Promedio móvil simple de cuerpos de velas**: solo para uso interno, que reproduce el cálculo del cuerpo promedio MT5.

## Notas
- La estrategia llama a `StartProtection()` una vez para habilitar las funciones de protección de posición integradas.
- Los comandos comerciales utilizan los ayudantes `BuyMarket` y `SellMarket` para aplanar la posición actual antes de abrir una nueva, coincidiendo con el comportamiento del asesor experto MetaTrader.
- No se proporciona ningún puerto Python de acuerdo con los requisitos del proyecto.
