# Estrategia Spreader 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia Spreader 2** es un sistema de trading por pares convertido del asesor experto de MetaTrader "Spreader 2". Observa dos instrumentos correlacionados en un marco temporal de un minuto y busca desviaciones a corto plazo entre sus movimientos de precio. Cuando ambas patas divergen dentro de límites de volatilidad controlada mientras mantienen correlación positiva, la estrategia abre un spread de mercado neutral yendo largo en un símbolo y corto en el otro. La posición combinada se cierra cuando la ganancia flotante total cumple el objetivo configurado o cuando se violan las reglas de correlación.

## Lógica principal

1. Recibir velas finalizadas para los símbolos primario y secundario y alinearlas por tiempo de cierre.
2. Mantener listas continuas de precios de cierre para que el algoritmo pueda hacer referencia a valores que están `ShiftLength`, `2 * ShiftLength` y `1440` barras en el pasado.
3. Calcular primeras diferencias (`x1`, `x2` para el símbolo primario e `y1`, `y2` para el símbolo secundario) para detectar oscilaciones locales.
4. Omitir el trading cuando cualquier instrumento muestra dos movimientos consecutivos en la misma dirección (filtro de tendencia) o cuando los productos `x1 * y1` indican correlación negativa.
5. Evaluar la relación de volatilidad `a / b` donde `a = |x1| + |x2|` y `b = |y1| + |y2|`. Solo proceder cuando la relación permanece entre `0.3` y `3.0`.
6. Escalar el volumen de la pata secundaria proporcionalmente a la relación de volatilidad y ajustarlo al paso de volumen del contrato, mínimo y máximo.
7. Confirmar la dirección de operación pretendida con el histórico de 1440 barras (aproximadamente un día de trading). El spread solo se abre cuando el movimiento diario apoya la señal a corto plazo.
8. La estrategia abre ambas patas simultáneamente: el símbolo primario opera con el `PrimaryVolume` configurado, mientras que el símbolo secundario opera el tamaño ajustado en la dirección opuesta.
9. Mientras las posiciones están abiertas, el sistema rastrea continuamente la ganancia flotante de ambas patas. Cuando la ganancia combinada supera `TargetProfit`, cierra el spread y restablece las referencias de entrada.
10. Las comprobaciones de seguridad cierran automáticamente posiciones huérfanas si una pata sale inesperadamente y reabren las patas faltantes cuando es posible para mantener el hedge equilibrado.

## Parámetros

- **SecondSecurity** – instrumento secundario que participa en el spread. Este parámetro es obligatorio.
- **PrimaryVolume** – volumen de operación (en lotes/contratos) para el símbolo primario. El valor predeterminado es `1`.
- **TargetProfit** – objetivo de ganancia monetaria absoluta para el par combinado. El valor predeterminado es `100`.
- **ShiftLength** – número de velas entre los puntos de comparación usados en los cálculos de primera diferencia. El valor predeterminado es `30`.
- **CandleType** – tipo de dato usado para las suscripciones de velas. Por defecto la estrategia trabaja con velas de un minuto.

## Reglas de trading

- Solo se procesan velas finalizadas para evitar actuar sobre datos incompletos.
- Los filtros de tendencia deben mostrar movimientos opuestos en las últimas dos ventanas de `ShiftLength` para ambos símbolos.
- La correlación debe ser positiva, y la relación de volatilidad debe permanecer en la banda `[0.3, 3.0]`.
- La comprobación de confirmación contra el histórico de 1440 barras previene operaciones que contradigan la dirección a más largo plazo.
- Las órdenes se envían con `OrderTypes.Market`. La pata secundaria se registra explícitamente con el instrumento secundario y la cartera para reflejar el comportamiento de MetaTrader.
- La ganancia abierta se calcula usando los últimos cierres de velas y los precios de entrada almacenados para determinar cuándo salir del spread.

## Notas

- La estrategia asume que ambos instrumentos comparten especificaciones de contrato compatibles. Si los multiplicadores difieren, el trading se deshabilita y se registra una advertencia.
- Debido a que el algoritmo original depende de un día completo de datos históricos, la versión StockSharp también espera hasta que al menos 1440 velas se hayan acumulado antes de la primera entrada.
- Toda la lógica de gestión de riesgo (objetivo de ganancia, manejo de pata huérfana) está contenida dentro de la estrategia. Se pueden agregar protecciones adicionales como stop-losses externamente si es necesario.
