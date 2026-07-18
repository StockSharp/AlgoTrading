# Estratégia Pin Bar Magic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Detecta pin bars altistas e baixistas dentro de uma tendência definida por um trio de médias móveis. As ordens são colocadas nos extremos da vela e canceladas após alguns barras se não forem executadas. O tamanho da posição é calculado a partir de um percentual de risco do capital e a distância do stop baseada em ATR.

O método visa capturar reversões abruptas em suportes ou resistências significativos. Sai das posições quando as EMAs rápida e média cruzam na direção oposta, sinalizando fraqueza da tendência.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: EMA rápida > EMA média > SMA lenta, pin bar altista que perfura uma das médias.
  - **Vendido**: EMA rápida < EMA média < SMA lenta, pin bar baixista que perfura uma das médias.
- **Critérios de saída**:
  - A EMA rápida cruza a EMA média na direção oposta.
- **Indicadores**:
  - SMA lenta (período 50)
  - EMA média (18) e EMA rápida (6)
  - ATR (comprimento 14)
- **Stops**: Risco de posição = EquityRisk% da conta com stop em ATR * multiplicador.
- **Valores padrão**:
  - `EquityRisk` = 3
  - `AtrMultiplier` = 0.5
  - `SlowSmaLength` = 50
  - `MediumEmaLength` = 18
  - `FastEmaLength` = 6
  - `AtrLength` = 14
  - `CancelEntryBars` = 3
- **Filtros**:
  - Reversão de ação do preço
  - Funciona em velas de 1h por padrão
  - Indicadores: EMA, SMA, ATR
  - Stops: Sim
  - Complexidade: Alto
