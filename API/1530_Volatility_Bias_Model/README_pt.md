# Estratégia de Modelo de Viés de Volatilidade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Conta fechamentos de alta versus baixa em uma janela e opera na direção do viés dominante quando a volatilidade é suficiente. Usa alvos ATR e encerra a posição após um número máximo de barras.

## Detalhes
- **Critérios de entrada**: Proporção de viés acima de `BiasThreshold` para comprado ou abaixo de `1 - BiasThreshold` para vendido com range acima de `RangeMin`.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop, take profit ou `MaxBars` atingido.
- **Stops**: Sim.
- **Valores padrão**:
  - `BiasWindow` = 10
  - `BiasThreshold` = 0.6
  - `RangeMin` = 0.05
  - `RiskReward` = 2
  - `MaxBars` = 20
  - `AtrLength` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Volatilidade
  - Direção: Ambos
  - Indicadores: ATR, SMA, Highest, Lowest
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
