# Estrategia TimeEA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia TimeEA** replica el asesor experto original de MetaTrader "TimeEA" dentro de StockSharp. Gestiona una sola posición basándose exclusivamente en la hora del día: abre a un momento configurado, mantiene la posición en una dirección fija y sale ya sea en un tiempo de cierre programado o una vez que se superan los niveles opcionales de stop-loss / take-profit.

A diferencia de los sistemas basados en indicadores, esta implementación se centra en la gestión disciplinada de sesiones. Asegura solo una entrada por día de trading, limpia la exposición opuesta antes de abrir, y aplica distancias mínimas configurables para órdenes protectoras que imitan las limitaciones de nivel de stop del bróker.

## Cómo funciona

1. La estrategia se suscribe a una serie de velas configurable (1 minuto por defecto) y evalúa solo las velas completadas.
2. Cuando el cierre de una vela cruza el **Tiempo de apertura** configurado, la estrategia:
   - Cierra cualquier posición opuesta que todavía pueda estar abierta.
   - Coloca una orden de mercado en la dirección elegida (Compra o Venta) con el volumen especificado.
   - Registra precios de stop-loss y take-profit en puntos (pasos de precio) desde la entrada, aplicando el multiplicador de distancia mínima.
3. Durante la sesión la estrategia monitorea las velas:
   - Si una vela toca el nivel de stop-loss o take-profit almacenado, la posición se cierra inmediatamente.
   - Si la vela cruza la ventana del **Tiempo de cierre**, la posición se aplana independientemente de la ganancia o pérdida.
4. Después de cerrar la operación (por stop, objetivo o programación) la estrategia permanece plana hasta el próximo día de trading.

Este flujo reproduce el comportamiento "abrir una vez por día" de la versión de MetaTrader que se basaba en comparaciones `TimeCurrent()` y `Time[0]`.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| **Open Time** | Hora del día para abrir la operación. Acepta `HH:MM:SS`. |
| **Close Time** | Hora del día para aplanar todas las posiciones. Puede ser el mismo día o pasar al día siguiente. |
| **Position Type** | Dirección de la posición (`Buy` o `Sell`). |
| **Order Volume** | Cantidad usada al enviar la orden de mercado. |
| **Stop Loss (points)** | Distancia en pasos de precio para el stop protector. Establecer en 0 para deshabilitar. |
| **Take Profit (points)** | Distancia en pasos de precio para el objetivo de ganancia. Establecer en 0 para deshabilitar. |
| **Minimum Distance Multiplier** | Offset mínimo aplicado tanto al stop como al objetivo (en pasos de precio) para emular la verificación original del nivel de stop contra el spread. |
| **Candle Type** | Serie de datos usada para detectar los límites de tiempo. Por defecto son velas de 1 minuto. |

## Notas prácticas

- **Entrada única por día** – Una vez que el tiempo de apertura se dispara, la estrategia no volverá a entrar hasta el siguiente día de calendario incluso si la posición fue stoped tempranamente.
- **Soporte de cruce de medianoche** – Tanto los tiempos de apertura como de cierre pueden establecerse antes o después de la medianoche. El helper respeta sesiones que continúan después de las 00:00.
- **Manejo de volumen** – Las órdenes de mercado respetan el parámetro `Order Volume`; ajustar al tamaño del contrato del instrumento seleccionado.
- **Emulación de nivel de stop** – El multiplicador de distancia mínima asegura que los stops/objetivos estén al menos un número definido de puntos lejos de la entrada, reflejando la regla original "spread × multiplicador".
- **Requisitos de datos** – La estrategia depende de velas consistentes para el timing. Usar marcos temporales locales del mercado para evitar la deriva de zona horaria.
- **Gestión de riesgos** – Los stops y objetivos se mantienen internamente; no se crean órdenes OCO del lado del servidor. Cuando una vela cruza los umbrales, la estrategia emite una orden de mercado para salir.

## Casos de uso

- Automatizar entradas basadas en sesión (p. ej., abrir posiciones en la apertura de Londres o Nueva York).
- Ejecutar estrategias de sesgo direccional donde la dirección se conoce de antemano pero la ejecución debe seguir un calendario preciso.
- Emular disparadores de tiempo al estilo MetaTrader dentro de la API de alto nivel de StockSharp sin temporizadores manuales.

## Limitaciones

- El deslizamiento se maneja implícitamente por órdenes de mercado; no hay un parámetro de desviación separado como en MetaTrader.
- El multiplicador de distancia mínima no lee spreads dinámicos; aplica un cojín estático expresado en pasos de precio.
- La estrategia asume que solo se negocia un instrumento/instrumento por instancia.

## Primeros pasos

1. Configurar los parámetros de la estrategia en Designer o vía código (tiempos de apertura/cierre, dirección, volumen, distancias de riesgo).
2. Adjuntar la estrategia al instrumento y fuente de datos deseados.
3. Asegurarse de que la serie de velas use la misma zona horaria que el horario previsto.
4. Ejecutar la estrategia y monitorear el registro de operaciones; las superposiciones visuales pueden habilitarse vía `DrawCandles` y `DrawOwnTrades` si se desea.

La lógica está completamente contenida en `CS/TimeEaStrategy.cs` con extensos comentarios en línea que explican cada etapa del flujo de trabajo.
