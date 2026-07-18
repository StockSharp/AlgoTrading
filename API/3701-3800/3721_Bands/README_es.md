# Estrategia de bandas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia transfiere el asesor experto MetaTrader 5 **Bands.mq5** al API de alto nivel de StockSharp. Espera una vela terminada
que perfora las bandas Bollinger desde el exterior hacia el canal y solo abre una posición cuando el canal Donchian se configura
Confirma que la pendiente de la banda ha sido estable durante un número configurable de barras. Los múltiplos del rango verdadero promedio (ATR) reproducen el ori
distancias iniciales de stop-loss y take-profit, mientras que un rastreador de regresión opcional imprime el coeficiente de determinación de la curva de acciones
(R cuadrado) cada 100 operaciones, reflejando el resultado de diagnóstico de la versión MQL.

## Lógica comercial
1. Suscríbase a un flujo de velas único y calcule Bollinger Bandas, un Donchian Canal y ATR con los mismos períodos que el MetaT.
robot de radar.
2. Cuando no haya ninguna posición abierta, inspeccione la vela **anterior** completada:
   - Ingrese en largo si esa vela se abrió por debajo de la banda inferior Bollinger y cerró por encima de ella, y la banda inferior Donchian no ha disminuido.
ined durante más de `ConfirmationPeriod` barras.
   - Entre en corto si la vela se abrió por encima de la banda superior Bollinger y cerró por debajo de ella, y la banda superior Donchian no ha subido.
es para más de `ConfirmationPeriod` barras.
3. Cuando exista una posición, salga si se cruza el límite final Donchian (utilizando el cierre anterior) o si la base ATR
Se violan los niveles d proteccion intrabar.
4. Cada operación ejecutada almacena el capital de la cartera actual e imprime la métrica R cuadrado de regresión lineal después de cada bloque de
100 operaciones. Una pendiente negativa produce un R cuadrado negativo al igual que el asesor experto original.

## Gestión de riesgos
- Las órdenes de entrada siempre se envían al mercado con el `TradeVolume` definido por el usuario.
- Los niveles de protección se recrean en el código (en lugar de utilizar órdenes pendientes) comparando los máximos y mínimos de las velas con el ATR mu.
tiples.
- Cuando se activa el stop-loss o el take-profit, la estrategia cierra toda la posición con una orden de mercado y restablece la protección.
en niveles.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `TradeVolume` | Volumen neto (en lotes) para cada orden de mercado. |
| `CandleType` | Tipo de datos de vela/período de tiempo utilizado para todos los indicadores. |
| `BollingerPeriod` | Número de velas utilizadas por las Bollinger Bandas. |
| `BollingerDeviation` | Multiplicador de desviación estándar aplicado a las Bollinger Bandas. |
| `DonchianPeriod` | Longitud del canal Donchian utilizado como filtro de tendencias. |
| `ConfirmationPeriod` | Recuento mínimo de barras consecutivas que deben mantener la pendiente Donchian no decreciente (larga) ni no creciente (corta). |
| `AtrPeriod` | Período del Rango Verdadero Promedio utilizado para la gestión de riesgos. |
| `StopAtrMultiplier` | ATR múltiplo que define la distancia del stop-loss. |
| `TakeAtrMultiplier` | ATR múltiplo que define la distancia de toma de ganancias. |

## Notas
- La verificación de pendiente Donchian se implementa como un contador rodante en lugar de copiar los buffers del indicador, lo que mantiene el StockSharp
versión eficiente y al mismo tiempo coincide con el comportamiento del EA original.
- Todos los comentarios y diagnósticos se proporcionan en inglés según lo exigen las directrices del proyecto.
- Los ayudantes de administración de dinero del código MetaTrader no se reproducen; la implementación de StockSharp se basa en `TradeVolume`
parámetro para dimensionar la posición.
