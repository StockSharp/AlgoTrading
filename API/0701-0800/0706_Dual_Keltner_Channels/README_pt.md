# Canais Keltner Duplos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de **Canais Keltner Duplos** usa dois canais Keltner com multiplicadores diferentes para detectar rompimentos.
Uma operação é aberta quando o preço perfura a banda exterior e então retorna através da banda interior.
Stops e alvos são gerenciados com percentuais fixos.

## Detalhes
- **Critérios de entrada**: O preço cruza a banda exterior de Keltner e volta a cruzar a banda interior na mesma direção.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop loss, take profit ou sinal oposto.
- **Stops**: Sim, baseados em percentual.
- **Valores padrão**:
  - `EmaPeriod = 50`
  - `InnerMultiplier = 2.75m`
  - `OuterMultiplier = 3.75m`
  - `MaxStopPercent = 10m`
  - `SlTpRatio = 1m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Keltner
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
