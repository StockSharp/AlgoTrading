# Estrategia EurGbp EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general
La estrategia EurGbp EA replica el asesor experto original de MetaTrader comparando el momentum MACD horario de EUR/USD y GBP/USD mientras opera en el instrumento principal configurado (normalmente EUR/GBP). El enfoque explota la fuerza relativa entre los pares principales del euro y la libra para anticipar movimientos en el cruce.

## Indicadores
* **MACD (12, 26, 9)** en EUR/USD (señal e histograma).
* **MACD (12, 26, 9)** en GBP/USD (señal e histograma).

Ambos indicadores se evalúan en el mismo marco temporal seleccionado mediante el parámetro `Candle Type` (1 hora por defecto).

## Lógica de negociación
1. Suscribirse a velas del instrumento negociado más EUR/USD y GBP/USD.
2. Calcular señal e histograma MACD para ambos pares de referencia.
3. **Condición de compra:**
   * Histograma EUR/USD &lt; histograma GBP/USD, **y**
   * Señal EUR/USD &gt; señal GBP/USD,
   * Sin posición larga existente (o una corta existente que será aplanada).
4. **Condición de venta:**
   * Histograma GBP/USD &lt; histograma EUR/USD, **y**
   * Señal GBP/USD &gt; señal EUR/USD,
   * Sin posición corta existente (o una larga existente que será aplanada).
5. Solo se permite una operación por barra en cada dirección para evitar entradas duplicadas.
6. Las órdenes de stop-loss y take-profit se adjuntan inmediatamente después de la entrada usando las distancias configuradas en puntos.

## Parámetros
| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| Candle Type | Marco temporal para todas las suscripciones de velas. | 1 hora |
| EURUSD Security | Instrumento que proporciona velas EUR/USD. | Debe configurarse |
| GBPUSD Security | Instrumento que proporciona velas GBP/USD. | Debe configurarse |
| Volume | Volumen de orden (lotes). | 0.01 |
| Stop Loss | Stop protector en pasos de precio. | 75 |
| Take Profit | Objetivo de beneficio en pasos de precio. | 46 |

## Gestión de riesgo
* `Stop Loss` y `Take Profit` se miden en pasos de precio del instrumento negociado. Asegúrese de que el instrumento tenga un valor `PriceStep` válido.
* La protección se inicia automáticamente cuando se lanza la estrategia (`StartProtection`).
* Si cualquiera de las distancias es cero, se omite la orden protectora correspondiente.

## Notas de uso
* Asigne el instrumento principal de negociación a la instancia de estrategia antes de iniciar (por ejemplo, EUR/GBP).
* Configure `EURUSD Security` y `GBPUSD Security` para que referencien fuentes de datos disponibles en su conexión.
* La estrategia requiere datos sincronizados para los tres instrumentos en el marco temporal elegido para generar señales de forma fiable.
* Solo se usan órdenes de mercado. Las posiciones opuestas existentes se cierran enviando el volumen inverso.

## Notas de conversión
* Las entradas originales `_Lots`, `_SL`, `_TP`, `_MagicNumber`, `_Comment`, `_OnlyOneOpenedPos` y `_AutoDigits` se asignan a parámetros StockSharp o comportamiento integrado.
* Las rutinas auxiliares de cierre de órdenes de la versión MQL se reemplazan por la gestión de órdenes protectoras de alto nivel de StockSharp.
* El manejo de errores y los bucles de reintento del código MQL se omiten porque el modelo de ejecución de StockSharp ya gestiona estados de órdenes y reintentos.
