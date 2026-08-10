![Logotipo de StockSharp](logo.png)

# Ejemplos de trading algorítmico con StockSharp

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este es el repositorio oficial de StockSharp con ejemplos de trading algorítmico. Reúne un amplio catálogo organizado de estrategias para la API, ejemplos visuales de Strategy Designer, material educativo y comprobaciones automáticas que mantienen los ejemplos compilables.

El repositorio está pensado para el aprendizaje, la investigación, la creación de prototipos y las pruebas de regresión. Las estrategias ilustran ideas de trading y el uso de las API de StockSharp; no son recomendaciones de inversión listas para usar.

## Por dónde empezar

| Objetivo | Ubicación |
|---|---|
| Explorar estrategias por idea de trading | [Catálogo de estrategias de la API](API/README_es.md) |
| Estudiar implementaciones en C# y Python | [`API`](API/) |
| Explorar esquemas visuales y ejemplos de Designer | [`Designer`](Designer/) |
| Compilar y probar una estrategia en C# | [`Backtester`](Backtester/) |
| Revisar el entorno de pruebas automáticas | [`Tests`](Tests/) |

## Contenido del repositorio

### Catálogo de estrategias de la API

El directorio [`API`](API/) contiene desde componentes conocidos —cruces de medias móviles, rupturas, momentum, volatilidad y reversión a la media— hasta trading de pares, arbitraje, creación de mercado, métodos de cartera, modelos de flujo de órdenes, experimentos de aprendizaje automático y numerosas variantes especializadas.

El catálogo agrupa las estrategias por su idea principal de trading, mientras que el sistema de archivos utiliza directorios con rangos numéricos para que GitHub pueda mostrar la colección de forma eficiente. Un ejemplo típico tiene esta estructura:

```text
API/0001-0100/0001_MA_CrossOver/
├── CS/
│   ├── MaCrossoverStrategy.cs
│   └── logo.svg
├── PY/
│   ├── ma_crossover_strategy.py
│   └── logo.svg
├── README.md
├── README_ru.md
├── README_zh.md
├── README_es.md
├── README_de.md
├── README_pt.md
└── README_ja.md
```

Cada ejemplo de la API implementa la misma idea de estrategia tanto en C# como en Python. La documentación en siete idiomas explica el concepto, los parámetros, la lógica de señales y los riesgos. Los logotipos SVG transparentes identifican la estrategia y su lenguaje de implementación, y siguen siendo legibles con temas claros y oscuros.

### Ejemplos de Strategy Designer

El directorio [`Designer`](Designer/) contiene esquemas visuales, tipos de estrategia reutilizables y ejemplos educativos para [StockSharp Strategy Designer](https://doc.stocksharp.com/en/topics/designer.html). Resultan útiles cuando se prefiere construir y analizar una estrategia gráficamente en lugar de comenzar directamente con código fuente.

### Herramientas de compilación y pruebas

El repositorio incluye dos pequeños proyectos .NET:

- [`Backtester`](Backtester/) compila dinámicamente una estrategia C# seleccionada y la ejecuta con los datos históricos de ejemplo incluidos.
- [`Tests`](Tests/) compila los ejemplos de la API y los ejecuta en el entorno de emulación histórica de StockSharp.

El proyecto de pruebas utiliza un generador de código fuente, por lo que las estrategias normales no necesitan métodos de prueba escritos a mano. Cada prueba generada ejecuta una estrategia con datos de mercado de ejemplo, comprueba que produzca órdenes y operaciones, y verifica la clonación y serialización de la configuración. Las estrategias que requieren varios instrumentos o una preparación especial disponen de implementaciones explícitas en el proyecto de pruebas.

Antes de compilar .NET, [`Tools/validate_api_structure.py`](Tools/validate_api_structure.py) realiza comprobaciones estructurales rápidas: ubicación en el rango numerado correcto, paridad entre C# y Python, traducciones obligatorias, presencia de archivos fuente y ausencia de afirmaciones obsoletas sobre versiones de lenguaje no disponibles.

## Requisitos

Para compilar la solución completa localmente, instala:

- el SDK de .NET 10;
- Python 3 para el validador de estructura;
- una copia del repositorio de la plataforma StockSharp junto a este repositorio.

Las referencias de proyecto esperan la siguiente estructura de directorios:

```text
<workspace>/
├── AlgoTrading/
└── StockSharp (GitHub)/
```

Clona este repositorio como `AlgoTrading` y el [repositorio de la plataforma StockSharp](https://github.com/StockSharp/StockSharp) como `StockSharp (GitHub)` dentro del mismo directorio padre.

## Validar, compilar y probar

Ejecuta primero las comprobaciones rápidas del repositorio:

```bash
python Tools/validate_api_structure.py
```

A continuación, compila y prueba la solución con la misma configuración utilizada por CI:

```bash
dotnet build AlgoTrading.slnx --configuration Release
dotnet test AlgoTrading.slnx --no-build --configuration Release
```

Para ejecutar una sola prueba de estrategia generada, filtra por el nombre de la carpeta de la estrategia en PascalCase. Por ejemplo:

```bash
dotnet test Tests/Tests.csproj --no-build --configuration Release \
  --filter "FullyQualifiedName~MaCrossover"
```

Para compilar y probar directamente un ejemplo en C#:

```bash
dotnet run --project Backtester/Backtester.csproj -- \
  API/0001-0100/0001_MA_CrossOver/CS/MaCrossoverStrategy.cs
```

## Uso de los ejemplos

Elige una estrategia del [catálogo](API/README_es.md), revisa sus supuestos y parámetros, y compara las implementaciones en C# y Python. Considera cada ejemplo como un punto de partida: selecciona datos de mercado, comisiones, deslizamiento, latencia, tamaño de posiciones y límites de riesgo adecuados antes de evaluar la idea.

Para el desarrollo visual, instala [Strategy Designer](https://stocksharp.com/en/store/strategy-designer/), abre su [Strategy Gallery](https://doc.stocksharp.com/en/topics/designer/strategy_gallery.html) y utiliza los esquemas de [`Designer`](Designer/) como material de aprendizaje o prototipos.

Valida siempre una estrategia modificada con datos fuera de muestra y en simulación antes de considerar su ejecución en vivo. Un backtest muestra el comportamiento en un conjunto de datos concreto; no demuestra rentabilidad futura.

## Contribuir

Se agradecen las contribuciones que mejoren la corrección, claridad, cobertura o valor educativo. Al añadir o modificar una estrategia de la API:

1. Mantén la estrategia en su directorio de rango numerado.
2. Conserva las implementaciones tanto en C# como en Python.
3. Mantén los siete README localizados alineados con los parámetros y el comportamiento reales.
4. Añade o actualiza los logotipos SVG transparentes de cada lenguaje cuando cambie la identidad visual de la estrategia.
5. Ejecuta el validador de estructura, la compilación Release y las pruebas correspondientes antes de abrir un pull request.

El generador de pruebas descubre automáticamente las estrategias normales. Añade una implementación manual únicamente cuando el ejemplo necesite instrumentos, carteras, datos de mercado u otra preparación que el entorno estándar no pueda proporcionar.

## Recursos

- [Sitio web de StockSharp](https://stocksharp.com/)
- [Documentación](https://doc.stocksharp.com/en/)
- [Strategy Designer](https://stocksharp.com/en/store/strategy-designer/)
- [Chat de la comunidad](https://stocksharp.com/en/chat/)
- [Seguimiento de incidencias](https://github.com/StockSharp/AlgoTrading/issues)

## Licencia y aviso de riesgo

Consulta [LICENSE](LICENSE) y [NOTICE](NOTICE) para conocer las condiciones aplicables a este repositorio.

El trading algorítmico implica riesgos importantes. Los ejemplos se proporcionan con fines educativos y técnicos, sin garantía alguna de rendimiento. Antes de utilizar dinero real, eres responsable de revisar el código, validar sus supuestos y aplicar controles adecuados de riesgo operativo y financiero.
