# Estrategia de 555 Scalper
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia 555 Scalper es un port directo del asesor experto "555 Scalper" de MetaTrader. Opera en cualquier marco temporal primario mientras se apoya en filtros de marcos temporales superiores y confirmación de momentum mensual. El algoritmo combina un cruce de medias móviles ponderadas linealmente (LWMA) rápida/lenta con una confirmación de momentum en marco temporal superior y un filtro MACD mensual. La lógica de protección refleja el EA original, incluyendo movimientos de break-even, trailing clásico basado en pips, stops de emergencia basados en equity y salidas basadas en dinero.

## Lógica de trading
- **Filtro de tendencia:** Calcula una LWMA rápida y una lenta sobre el precio típico del marco temporal de trading. Las posiciones long requieren que la LWMA rápida esté por encima de la lenta; las posiciones short requieren lo contrario.
- **Estructura de velas:** Valida que las dos últimas velas completadas se solapen (mínimo de hace dos barras por debajo del máximo anterior para longs, y viceversa para shorts) para aproximar la confirmación estilo fractal utilizada por el EA.
- **Filtro de momentum:** Utiliza un indicador Momentum de 14 períodos calculado en un marco temporal superior derivado del marco temporal de trading (ej., M1 → M15, M5 → M30, M15 → H1, etc.). Una operación sólo es válida si al menos una de las tres últimas lecturas de momentum se desvía del nivel neutral 100 por el umbral configurado (0.3 por defecto).
- **Confirmación MACD:** Aplica un filtro MACD mensual (12/26/9) y sólo compra cuando la línea principal MACD está por encima de la línea de señal, o vende cuando está por debajo.
- **Dimensionamiento de posición:** Comienza desde un lote base y multiplica cada entrada adicional por el exponente de lote configurado, habilitando una pirámide controlada hasta el número máximo de operaciones por dirección.

## Gestión de riesgos
- **Stop-loss y take-profit iniciales:** Cada nueva posición recibe un stop-loss y take-profit inicial basados en distancias de pip estilo MetaTrader.
- **Movimiento break-even:** Cuando el precio avanza un número configurable de pips en beneficio, el stop se mueve a break-even más un offset.
- **Trailing stop:** Implementa la lógica de trailing de pips original desplazando el stop con el precio una vez que la operación está en beneficio.
- **Objetivos de dinero:** Los take-profits opcionales de dinero y porcentaje cierran la posición una vez que el beneficio flotante alcanza los umbrales configurados.
- **Trailing de dinero:** Rastrea el beneficio flotante máximo y sale si el beneficio retrocede una cantidad configurable después de alcanzar el nivel de activación.
- **Stop de equity:** Monitorea el equity máximo de la cuenta alcanzado durante la sesión y liquida todas las posiciones si el drawdown flotante supera el porcentaje permitido.

## Parámetros
- **BaseVolume / LotExponent:** Define el tamaño inicial de la operación y el multiplicador para entradas adicionales.
- **StopLossSteps / TakeProfitSteps:** Distancias en pips para los niveles de protección.
- **FastMaPeriod / SlowMaPeriod:** Períodos de la LWMA rápida y lenta del filtro de tendencia.
- **Umbrales de momentum:** Desviación requerida de 100 para configuraciones long y short.
- **MaxTrades:** Número máximo de entradas escalonadas por dirección.
- **Ajustes de BreakEven y Trailing:** Configura el disparador de break-even basado en pips, el offset y la distancia de trailing.
- **Gestión de dinero:** Habilita o deshabilita el take-profit de dinero, el take-profit en porcentaje y los controles de trailing de dinero.
- **Stop de equity:** Porcentaje de drawdown desde el pico de equity que activa una salida global.

## Notas de uso
1. Adjunte la estrategia a cualquier instrumento y seleccione el marco temporal de trading deseado a través del parámetro `CandleType`.
2. La fuente de momentum de marco temporal superior se calcula automáticamente en función del marco temporal primario; asegúrese de que haya datos históricos disponibles para ambos marcos temporales.
3. La fuente de MACD mensual requiere datos de velas mensuales. Al probar, proporcione suficiente historial para calentar la señal MACD.
4. Ajuste el volumen, las distancias en pips y los umbrales de gestión de dinero según la volatilidad del instrumento y el perfil de riesgo de la cuenta.

La estrategia reproduce el proceso de decisión central del EA original mientras aprovecha la API de alto nivel de StockSharp para suscripciones de datos, gestión de indicadores y ejecución de órdenes.
