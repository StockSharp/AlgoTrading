# Estrategia de Canales Donchian
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el clásico asesor experto "Donchian Channels" a la API de alto nivel de StockSharp. Combina una ruptura Donchian de múltiples marcos temporales con medias móviles ponderadas, confirmación de momentum, filtrado de tendencia con MACD y extensivos controles de riesgo (stop loss, take profit, break-even, trailing stop y salida de emergencia basada en equity).

## Visión general de la lógica

- **Régimen de mercado:**
  - El Canal Donchian se calcula en un marco temporal superior (por defecto 4 horas) para detectar la estructura de ruptura prevalente.
  - Un MACD calculado en un marco temporal de tendencia configurable (diario por defecto) asegura que la tendencia del marco temporal superior coincida con la dirección del trade.
- **Condiciones de entrada:**
  - **Configuración larga:**
    - La banda inferior de Donchian o la mediana del canal penetra el cuerpo de la vela anterior del marco temporal superior desde abajo, señalando una potencial ruptura.
    - Las dos últimas velas del marco temporal base forman un swing alcista (`Low[2] < High[1]`).
    - La desviación absoluta del momentum desde 100 en el marco temporal superior supera el umbral de compra en cualquiera de las últimas tres lecturas.
    - La LWMA rápida permanece dentro de la distancia configurada por encima de la LWMA lenta para evitar movimientos sobreextendidos.
    - La línea principal del MACD está por encima de su señal (ambas positivas o ambas negativas) confirmando sesgo alcista.
  - **Configuración corta:** Reglas simétricas reflejadas para la banda superior de Donchian, estructura de swing, desviación de momentum bajista y confirmación de MACD.
  - Se permiten múltiples entradas (pirámide) hasta alcanzar el recuento máximo de trades configurado.
- **Condiciones de salida:**
  - Stop loss y take profit fijos definidos en pasos de precio.
  - Movimiento opcional a break-even una vez que el precio progresa una distancia configurable más allá de la entrada.
  - Trailing stop que puede seguir los extremos de velas recientes (con relleno) o trailear el precio usando un enfoque clásico de disparador/paso.
  - El stop de equity monitorea el drawdown de P&L de la estrategia y fuerza el cierre cuando las pérdidas superan el presupuesto de riesgo permitido.

## Parámetros

| Grupo | Nombre | Descripción |
| ----- | ------ | ----------- |
| General | Base Candle | Marco temporal de ejecución para entradas y controles de riesgo. |
| General | Donchian Candle | Marco temporal superior para el canal Donchian y el filtro de momentum. |
| General | Trend Candle | Marco temporal utilizado por el filtro de tendencia MACD. |
| General | Volume | Tamaño de orden para cada entrada. |
| Indicators | Donchian Length | Período de lookback para el Canal Donchian. |
| Indicators | Fast MA / Slow MA | Longitudes de las medias móviles ponderadas en el marco temporal de trading. |
| Indicators | MA Distance | Distancia máxima permitida entre la LWMA rápida y lenta (en pasos de precio). |
| Indicators | Momentum Period | Lookback para el filtro de momentum en el marco temporal superior. |
| Filtros | Momentum Buy / Sell | Desviación absoluta mínima desde 100 requerida para momentum alcista/bajista. |
| Risk | Stop Loss / Take Profit | Salidas duras medidas en pasos de precio desde el precio de entrada. |
| Risk | Use Trailing | Habilita la gestión del trailing stop. |
| Risk | Trailing Trigger / Step | Parámetros clásicos de trailing cuando el trailing basado en velas está desactivado. |
| Risk | Candle Trail / Trail Candles | Activa el trailing basado en velas y establece el número de velas utilizadas. |
| Risk | Trailing Padding | Buffer extra aplicado alrededor de los extremos de velas. |
| Risk | Use BreakEven | Habilita el movimiento a break-even. |
| Risk | BreakEven Trigger / Offset | Distancia y offset aplicados al mover el stop a break-even. |
| Risk | Use Equity Stop | Activa la salida de emergencia basada en drawdown. |
| Risk | Equity Risk | Drawdown máximo permitido antes de cerrar la posición. |
| Risk | Max Trades | Número máximo de entradas en pirámide concurrentes. |

## Consejos de uso

1. **Marcos temporales:** Alinear el marco temporal base con tu estilo de ejecución (p.ej., 1h/4h) y mantener los marcos temporales de Donchian/MACD más altos para mantener la lógica de confirmación multi-marco temporal.
2. **Umbrales de momentum:** El EA original medía desviaciones de momentum alrededor de 100. Comenzar con umbrales pequeños (0.3) y aumentar para filtrar movimientos débiles en mercados agitados.
3. **Configuración de riesgo:** Convertir distancias basadas en pips de la versión MQL a pasos de precio específicos del instrumento. Siempre verificar el valor `Step` del instrumento al configurar stops y lógica de trailing.
4. **Pirámide:** Reducir `Max Trades` a 1 si prefieres la gestión de posición única. Aumentarlo gradualmente al probar el comportamiento de pirámide.
5. **Stop de equity:** El stop de equity monitorea el P&L de la estrategia dentro de StockSharp. Ajustar `Equity Risk` para reflejar el drawdown máximo (en moneda de cuenta) que estás dispuesto a tolerar.

## Backtesting

- Funciona directamente dentro de StockSharp Designer/Backtester usando solo suscripciones de velas (no se requieren datos de nivel tick).
- Asegurarse de que todos los marcos temporales seleccionados estén disponibles del proveedor de datos antes de lanzar un backtest o sesión en vivo.
- Al optimizar, priorizar la longitud de Donchian, la distancia de MA y los umbrales de momentum — tienen el impacto más fuerte en la tasa de aciertos y la frecuencia de trades.
