# Estrategia MACD Simple Reshetov
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia reproduce el comportamiento del asesor experto "MACDSimple" de Yury Reshetov de MetaTrader dentro del framework StockSharp. Trabaja con un único instrumento y evalúa señales MACD clásicas que son modificadas por dos parámetros de offset. El algoritmo procesa solo velas completadas, asegurando que todas las decisiones de trading se tomen sobre datos confirmados y evitando el ruido intrabarra.

## Indicadores y cálculos
- **MACD (Moving Average Convergence Divergence)** – la línea MACD y la línea de señal se calculan con períodos personalizados:
  - Período de EMA rápida = `SignalPeriod + DF`
  - Período de EMA lenta = `SignalPeriod + DS + DF`
  - Período de línea de señal = `SignalPeriod`
Los offsets `DF` y `DS` siguen las entradas originales del experto y permiten al trader estirar o comprimir los componentes MACD manteniendo intacta su relación.

## Parámetros
| Nombre | Descripción | Por defecto |
| ------ | ----------- | ----------- |
| `Volume` | Tamaño de orden usado para cada entrada de mercado. | 2 |
| `DF` | Offset añadido a la longitud de la EMA rápida del MACD. Debe ser cero o positivo. | 1 |
| `DS` | Offset adicional aplicado a la longitud de la EMA lenta del MACD. Debe ser cero o positivo. | 2 |
| `SignalPeriod` | Período base del que se derivan las longitudes de EMA rápida y lenta. | 10 |
| `CandleType` | Marco temporal de las velas usadas para análisis y trading. | Marco temporal de 30 minutos |

## Lógica de trading
### Manejo de posición
1. En cada vela terminada, la estrategia actualiza el indicador MACD e ignora la barra si el indicador aún no está completamente formado.
2. Si hay una posición **larga** abierta y la línea MACD cae por debajo de cero, la estrategia cierra toda la posición larga al precio de mercado.
3. Si hay una posición **corta** abierta y la línea MACD sube por encima de cero, la estrategia cierra toda la posición corta al precio de mercado.
4. Después de cerrar una posición en una barra dada, el algoritmo deja de procesar esa barra, reflejando el comportamiento del asesor experto original.

### Reglas de entrada
1. El algoritmo verifica que tanto la línea MACD como la línea de señal compartan el mismo signo (ambas positivas o ambas negativas). Los signos mixtos no producen operaciones.
2. Cuando ambas líneas son **positivas**, se abre una posición larga si la línea MACD está por encima de la línea de señal.
3. Cuando ambas líneas son **negativas**, se abre una posición corta si la línea MACD está por debajo de la línea de señal.
4. Las órdenes de mercado tienen el tamaño configurado con el parámetro `Volume`. Solo puede existir una posición a la vez.

### Reglas de salida
- Las salidas son impulsadas únicamente por la línea MACD cruzando el nivel cero en contra de la posición abierta, como se describe en la sección de manejo de posición. No se implementan salidas parciales, stop losses ni take profits por defecto.

## Notas adicionales
- La estrategia opera solo cuando `IsFormedAndOnlineAndAllowTrading()` se satisface, asegurando que los datos en vivo estén disponibles y el trading esté habilitado antes de entrar en nuevas posiciones.
- No hay gestión de riesgo automática incorporada. Los usuarios pueden agregar protecciones personalizadas como `StartProtection()` o combinar la estrategia con controles de riesgo a nivel de portafolio si lo desean.
- Debido a que los parámetros del MACD se derivan de un único período base más offsets, ajustar `SignalPeriod`, `DF` o `DS` afecta a todos los componentes simultáneamente, preservando el espaciado relativo pretendido por el asesor experto original.

## Detalles de implementación
- El enlace de indicadores usa la API `SubscribeCandles().Bind()` de alto nivel de StockSharp, manteniendo la implementación concisa y orientada a eventos.
- La conversión sigue el conjunto de reglas descrito en `AGENTS.md`: se usan tabulaciones para la indentación, los valores de indicadores se consumen directamente desde el callback de enlace, y las funciones de trading `BuyMarket`/`SellMarket` gestionan entradas y salidas.
- La estructura de la estrategia está lista para extensión (por ejemplo, añadiendo filtros o lógica de riesgo) mientras se mantiene fiel a la lógica del experto MetaTrader original.
