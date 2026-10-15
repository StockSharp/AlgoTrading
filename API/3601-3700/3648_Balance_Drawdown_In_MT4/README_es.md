# Reducción de saldo en la estrategia MT4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia traslada el MetaTrader 4 asesor experto original **BalanceDrawdownInMT4** al API de alto nivel de StockSharp. El EA abre inmediatamente una única posición larga y mide continuamente la reducción de la cuenta en relación con el saldo máximo alcanzado desde que comenzó la sesión.

## Lógica de trading

1. Cuando comienza la estrategia, llama a `StartProtection` para armar niveles administrados de stop-loss y take-profit que imitan las entradas de MQL expresadas en puntos de precio.
2. En la primera vela finalizada (plazo predeterminado: 1 minuto), la estrategia verifica si una posición está abierta. Si no existe exposición, envía una orden de compra de mercado utilizando el `Volume` configurado.
3. Después de cada vela terminada, actualiza la métrica de reducción:
   - La estrategia rastrea el saldo máximo alcanzado como **StartBalance + PnL realizado**.
   - El capital actual es igual a **StartBalance + PnL realizado + PnL no realizado**, donde el PnL no realizado se deriva del último precio de cierre de vela, el precio de entrada promedio y el `PriceStep`/`StepPrice` del instrumento.
   - La reducción es la disminución porcentual desde el saldo máximo almacenado hasta el capital actual. El valor se registra con un mensaje informativo en cada actualización.

El algoritmo nunca abre posiciones adicionales ni invierte. Una vez que se establece la posición inicial, permanece activa hasta que se detiene, se activa la toma de ganancias o el usuario interviene manualmente.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `StartBalance` | `1000` | Saldo de referencia utilizado al calcular el capital máximo y el porcentaje de reducción. |
| `Volume` | `0.01` | Volumen neto (en unidades de instrumentos) de la orden de compra inicial en el mercado. |
| `StopLossPoints` | `300` | Distancia desde el precio de entrada hasta el stop de protección, medida en puntos de precio. Un valor de `0` desactiva la parada. |
| `TakeProfitPoints` | `400` | Distancia desde el precio de entrada al objetivo de protección, medida en puntos de precio. Un valor de `0` deshabilita el objetivo. |
| `CandleType` | `1m` período de tiempo | Marco de tiempo que impulsa las actualizaciones periódicas de la reducción y la verificación de entrada inicial. |

## Notas de implementación

- El contador de reducción utiliza el PnL realizado de la estrategia (`PnL`) combinado con el PnL no realizado estimado a partir de las diferencias de precios, lo que coincide con la lógica del saldo corriente que se encuentra en la versión MT4.
- Si `PriceStep` o `StepPrice` no está disponible para la seguridad, el cálculo de PnL no realizado devuelve cero de forma segura, lo que evita errores de división por cero.
- `Volume` se valida para garantizar un valor positivo antes de la operación inicial; de lo contrario, se registra una advertencia y la estrategia permanece estable.
- `DrawdownPercent` expone la última lectura de reducción para que otros módulos (paneles de control, controladores de riesgos) puedan extraer el valor mediante programación.

## Consejos de uso

- Establezca `StartBalance` en el saldo real de la cuenta (o el saldo al comienzo de la sesión de negociación) para obtener estadísticas de reducción significativas.
- Mantenga las velas predeterminadas de 1 minuto para actualizaciones oportunas o elija un tipo de vela sintética más rápida si necesita una precisión cercana al tick.
- Debido a que esta estrategia mantiene intencionalmente una única posición larga, combínela con controles de riesgo manuales o automatización externa si necesita volver a ingresar después de alcanzar una parada o un objetivo.
- Pruebe siempre en un simulador para confirmar que el corredor proporciona `PriceStep` y `StepPrice` para que la conversión PnL no realizada coincida con las expectativas.
