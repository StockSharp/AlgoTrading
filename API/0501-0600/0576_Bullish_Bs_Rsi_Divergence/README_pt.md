# Divergência RSI Altista de B's
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Utiliza RSI para detectar divergências altistas regulares e ocultas com pontos pivô. Abre operações compradas na divergência e fecha em sinais baixistas, alvo de RSI ou stop trailing.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Divergência RSI altista regular ou oculta.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Divergência baixista, RSI cruzando acima do alvo ou stop trailing.
- **Stops**: Stop trailing opcional baseado em ATR ou percentual.
- **Valores padrão**:
  - `RsiPeriod` = 9
  - `PivotLookbackRight` = 3
  - `PivotLookbackLeft` = 1
  - `TakeProfitRsiLevel` = 80
  - `RangeUpper` = 60
  - `RangeLower` = 5
  - `StopType` = None
  - `StopLoss` = 5
  - `AtrLength` = 14
  - `AtrMultiplier` = 3.5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Divergência
  - Direção: Comprado
  - Indicadores: RSI, ATR
  - Stops: Stop trailing opcional
  - Complexidade: Avançado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
