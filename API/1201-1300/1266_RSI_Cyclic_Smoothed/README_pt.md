# RSI Cíclico Suavizado Estratégia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no indicador RSI ciclicamente suavizado. Calcula bandas de percentil dinâmicas e opera reversões quando o oscilador as cruza.

## Detalhes

- **Critérios de entrada**: CRSI cruza acima da banda inferior ou abaixo da banda superior.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Cruzamento da banda oposta.
- **Stops**: Sim.
- **Valores padrão**:
  - `DominantCycleLength` = 20
  - `Vibration` = 10
  - `Leveling` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: RSI
  - Stops: Sim
  - Complexidade: Avançado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
