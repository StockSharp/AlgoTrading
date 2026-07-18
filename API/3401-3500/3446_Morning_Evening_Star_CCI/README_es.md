# Estrategia estrella de la mañana/tarde CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia replica el MetaTrader 5 Asesor Experto **Expert_AMS_ES_CCI** usando la API de alto nivel de StockSharp. Busca patrones de reversión de tres velas Morning Star y Evening Star y requiere confirmación del Commodity Channel Index (CCI) antes de abrir nuevas posiciones. La lógica funciona solo con velas terminadas y opera con el valor principal especificado en la configuración de la estrategia.

## Lógica de trading
- **Entrada larga de Morning Star**
  - Detecta tres velas consecutivas que forman un patrón de Estrella de la Mañana:
    - Vela 1: cuerpo bajista fuerte (tamaño del cuerpo mayor que el cuerpo promedio en la ventana seleccionada).
    - Vela 2: vela de cuerpo pequeño con un gap más bajo que la vela 1.
    - Vela 3: cierra por encima del punto medio de la Vela 1.
  - Confirme que el valor CCI en la barra de señal sea menor que el umbral de entrada negativo (predeterminado −50).
- **Entrada corta de Evening Star**
  - Detectar un patrón de Estrella Vespertina válido:
    - Vela 1: fuerte cuerpo alcista.
    - Vela 2: vela de cuerpo pequeño que se abre por encima de la Vela 1.
    - Vela 3: cierra por debajo del punto medio de la Vela 1.
  - Confirme que el valor CCI en la barra de señal sea mayor que el umbral de entrada positivo (predeterminado +50).
- **Reglas de salida de posición**
  - Cierre las posiciones cortas cuando CCI vuelva a cruzar por encima de −NeutralThreshold o caiga por debajo de +NeutralThreshold (predeterminado ±80).
  - Cierre las posiciones largas cuando CCI vuelva a cruzar por debajo de +NeutralThreshold o caiga por debajo de −NeutralThreshold.
  - No se incluyen reglas adicionales de limitación de pérdidas o toma de ganancias; los usuarios pueden agregar protecciones externas si es necesario.

## Indicadores
- **Índice de canales de productos básicos (CCI)**: filtro de confirmación, período predeterminado 25.
- **Promedio móvil simple de cuerpos de velas**: calcula el tamaño promedio del cuerpo durante las últimas velas *BodyAveragePeriod* (predeterminado 5) para validar la fuerza del patrón.

## Parámetros
| Nombre | Descripción | Predeterminado | Notas |
| --- | --- | --- | --- |
| `CciPeriod` | Número de barras utilizadas en el cálculo CCI. | 25 | Optimizable. |
| `BodyAveragePeriod` | Número de velas utilizadas para medir el tamaño corporal promedio. | 5 | Optimizable. |
| `EntryThreshold` | Valor absoluto de CCI requerido para nuevas operaciones. | 50 | Valor positivo; la estrategia comprueba ±EntryThreshold. |
| `NeutralThreshold` | Nivel absoluto CCI que define la zona de salida. | 80 | Valor positivo; la estrategia comprueba ±NeutralThreshold. |
| `CandleType` | Tipo de vela (período de tiempo) utilizado para el análisis. | plazo de 1 hora | Cambie para que coincida con la resolución deseada. |

## Notas
- La estrategia se suscribe a actualizaciones de velas a través de `SubscribeCandles` y utiliza `Bind` para recibir valores del indicador.
- Las operaciones se ejecutan con órdenes de mercado usando `BuyMarket` y `SellMarket`.
- Todos los comentarios en el código están escritos en inglés según sea necesario.
- Para ampliar la gestión de riesgos, combine la estrategia con `StartProtection` o módulos personalizados de gestión de dinero.
