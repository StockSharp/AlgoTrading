# Estrategia de control del panel comercial
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia de control del Panel de operaciones** replica la funcionalidad de la utilidad "Panel de operaciones" MetaTrader 4 dentro de StockSharp. El panel original MQL permitía al operador cambiar el período de tiempo del gráfico activo y saltar entre instrumentos haciendo clic en los botones de la interfaz de usuario. La versión StockSharp expone los mismos controles a través de parámetros de estrategia para que la aplicación host (Designer, Terminal o panel personalizado) pueda ajustarlos sobre la marcha.

A diferencia del Asesor Experto original, este puerto no envía órdenes comerciales. Su objetivo es mantener la suscripción del gráfico sincronizada con el marco temporal y el instrumento seleccionados actualmente y registrar los últimos cierres de velas, proporcionando comentarios similares a las etiquetas de texto en el panel original.

## Conceptos clave

- **Control dinámico de plazos**: elija entre M1, M5, M15, M30, H1, H4, D1 o W1. Al cambiar el parámetro se reconstruye inmediatamente la suscripción de la vela.
- **Búsqueda de instrumentos**: especifique un identificador de seguridad a seguir. Cuando está habilitada, la estrategia busca los `ISecurityProvider` conectados; de lo contrario, vuelve a recurrir a la seguridad ya adjunta a la estrategia.
- **Retroalimentación de velas**: cada vela terminada se registra con su precio de cierre para que el operador pueda verificar la combinación activa de símbolo y período de tiempo.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `TimeFrameName` | Código de período de tiempo preferido (`M1`, `M5`, `M15`, `M30`, `H1`, `H4`, `D1`, `W1`). El valor predeterminado es `M15`. |
| `SecurityId` | Identificador opcional del instrumento a controlar. Déjelo en blanco para utilizar la propiedad `Security` de la estrategia. |
| `AutoLookupSecurity` | Cuando `true`, la estrategia resuelve `SecurityId` hasta `SecurityProvider`. Deshabilítelo para aceptar la seguridad ya asignada tal como está. |
| `DefaultCandleType` | Se utiliza el respaldo `DataType` cuando se ingresa un período de tiempo desconocido. El valor predeterminado es velas de un minuto. |

## Flujo de trabajo

1. **Inicio**: el `OnStarted` la estrategia resuelve la seguridad objetivo y el período de tiempo, luego comienza una suscripción de vela para esa combinación.
2. **Ajustes de tiempo de ejecución**: cambiar `TimeFrameName`, `SecurityId` o `AutoLookupSecurity` mientras se ejecuta la estrategia reinicia la suscripción con la nueva configuración.
3. **Procesamiento de velas**: cada vela terminada actualiza la propiedad `LastFinishedCandle` y escribe una entrada de registro que contiene el identificador de seguridad, el código de período de tiempo y el precio de cierre.
4. **Apagar**: las suscripciones se detienen durante `OnStopped` o cuando la estrategia necesita reconstruirlas porque los parámetros cambiaron.

## Consejos de uso

- Combine la estrategia con un widget de gráfico en StockSharp Designer para reproducir el flujo de trabajo del panel MT4. Los editores de parámetros actúan como botones/combos.
- Deje `SecurityId` en blanco si el host ya asigna un `Security` a la instancia de estrategia.
- La salida del registro se puede conectar a una etiqueta o consola de UI para imitar las etiquetas informativas del script original.

## Diferencias con la versión MQL

- Sin botones gráficos; en su lugar se utilizan cambios de parámetros.
- No se envían acciones comerciales; la lógica se limita a la gestión y el registro de suscripciones de datos.
- La lista de plazos es idéntica al panel original, lo que garantiza un comportamiento familiar para los operadores que migran desde MT4.
