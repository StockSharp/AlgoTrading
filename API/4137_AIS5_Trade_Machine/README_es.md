# Máquina comercial AIS5
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
AIS5 Trade Machine traslada el MetaTrader 4 asesor experto `AIS5TM.mq4` a la StockSharp estrategia de alto nivel API. el original
programa centrado en construir histogramas de perfil de mercado en dos períodos de tiempo y ofrecer una consola de ejecución semiautomática. el
La versión StockSharp mantiene la idea de resaltar las zonas de precios fuertes y débiles a partir de la agregación de volumen de ticks y lo convierte en un
sistema de ruptura automatizado con control de riesgo adaptativo basado en el rango verdadero promedio (ATR).

La estrategia se suscribe a dos flujos de velas:
* Un **período de tiempo del perfil** (predeterminado 15 minutos) que acumula volumen para detectar zonas fuertes y débiles.
* Un **período de negociación** (predeterminado 1 minuto) que busca rupturas confirmadas por volumen fuera de esas zonas.

Las posiciones están protegidas por paradas proporcionales ATR y objetivos escalables. Las contracciones de volumen desencadenan salidas tempranas para imitar la
disciplina de monitoreo que se encuentra en el código MT4.

## Lógica estratégica
### Detección de zona de volumen (período de tiempo del perfil)
* Cada vela terminada con un período de tiempo más alto actualiza dos promedios móviles simples (SMA) del volumen de ticks.
* Una vela se etiqueta como **zona fuerte** cuando su volumen excede el promedio por el multiplicador configurable (`Strong Volume Mult`).
El precio de cierre de la vela se convierte en el nivel fuerte más reciente.
* Una vela se etiqueta como **zona débil** cuando su volumen cae por debajo del promedio dividido por el divisor configurado
(`Weak Volume Divider`). El precio de cierre de esa vela se convierte en el último nivel débil.
* Sólo participan velas terminadas. La estrategia ignora zonas hasta que el perfil SMA esté completamente formado para evitar cambios prematuros.
señales durante el período de calentamiento.

### Entradas de ruptura (período de negociación)
* El período de tiempo inferior espera a que terminen de formarse tanto su volumen SMA como el indicador ATR.
* Una configuración larga requiere que el cierre supere el nivel fuerte más reciente por la suma de los **Puntos Base de Zona** y
**Búfers de puntos de paso de zona** (convertidos a través del paso de precio del instrumento). La vela también debe generar un pico de volumen relativo
al promedio intradiario.
* Una configuración breve refleja la lógica en torno al último nivel débil, lo que requiere una ruptura más allá del buffer combinado y confirma
expansión de volumen.
* El experto MT4 original permitía comandos manuales y cuadrículas de múltiples órdenes. El puerto StockSharp mantiene un modelo de posición única, por lo que un
La ruptura solo se activa cuando la posición neta actual es plana.

### Gestión de salidas
* Al ingresar, la estrategia almacena el precio de ejecución, calcula una parada protectora basada en ATR (ATR multiplicada por `ATR Multiplier` y
sujeta por el buffer de la zona base), y establece el objetivo como la distancia de parada multiplicada por el divisor de volumen débil. Esto mantiene
riesgo y recompensa alineados con la estructura de volumen.
* Mientras una posición está abierta, la estrategia monitorea cada vela comercial terminada:
  * Si el precio alcanza el objetivo de beneficio o el stop de protección, la posición se aplana inmediatamente.
  * Si el volumen de ticks se contrae por debajo del umbral de volumen débil antes de alcanzar cualquiera de los niveles, la operación se cierra anticipadamente para evitar
persistiendo en zonas inactivas.
* Cuando la posición neta vuelve a cero, el estado interno se restablece, lo que permite evaluar la siguiente ruptura desde cero.

## Parámetros
* **Vela de perfil**: tipo de vela que alimenta el perfil de volumen (predeterminado: velas de 15 minutos).
* **Vela comercial**: período de tiempo más bajo utilizado para rupturas y salidas (predeterminado: velas de 1 minuto).
* **Retrospectiva de volumen**: número de velas tanto para las SMA de volumen como para el período ATR.
* **Múlt. de volumen fuerte**: multiplicador por encima del volumen promedio que marca una zona fuerte (se asigna a `Parameter.1` en MQL).
* **Divisor de volumen débil**: divisor por debajo del volumen promedio que marca zonas débiles y dimensiona el objetivo de ganancias (se asigna a
`Parameter.2`).
* **ATR Multiplicador**: factor de escala aplicado a ATR al calcular la distancia de parada adaptativa (se asigna a `Parameter.3`).
* **Puntos base de zona**: búfer mínimo en puntos agregados al nivel de zona antes de verificar las rupturas (se asigna a `ZoneBasePoints`).
* **Puntos de paso de zona**: zona de influencia de ruptura adicional en puntos que amplía la distancia desde la zona antes de que se realicen las entradas.
activado (se asigna a `ZoneStepPoints`).
* **Volumen** – heredado de la clase base `Strategy`; define el tamaño de la orden para las entradas al mercado.

## Notas adicionales
* La estrategia vuelve automáticamente a un paso de precio predeterminado de `0.0001` si el valor no especifica uno. Esto mantiene el
Parámetros basados en puntos compatibles con la mayoría de los símbolos FX.
* Todos los cálculos de indicadores se basan en velas terminadas para que coincidan con la implementación de MT4 que funcionó en barras completamente cerradas.
* A diferencia del EA original, no hay un panel de control manual ni un cargador de perfiles basado en archivos. Las zonas se reconstruyen exclusivamente a partir de datos en vivo.
datos de velas para mantener el puerto autónomo.
* La versión StockSharp no incluye una traducción de Python.
