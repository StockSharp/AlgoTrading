# Monitor de Regras FTMO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que monitora as regras do desafio FTMO e gerencia as operações com base no risco ATR.

A estratégia dimensiona as posições usando ATR e para quando os objetivos do desafio são atingidos. Monitora a perda diária máxima, a perda total, o objetivo de lucro e os dias mínimos de negociação.

## Detalhes

- **Critérios de entrada**: Vela altista abre comprado, vela baixista abre vendido.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Desafio concluído ou sinal oposto.
- **Stops**: Baseados em ATR.
- **Valores padrão**:
  - `AccountSize` = 10000m
  - `RiskPercent` = 1m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Gestão de risco
  - Direção: Ambos
  - Indicadores: ATR
  - Stops: ATR
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
