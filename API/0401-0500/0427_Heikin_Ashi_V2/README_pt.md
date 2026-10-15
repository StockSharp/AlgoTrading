# Estratégia Heikin Ashi V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta segunda versão do sistema Heikin Ashi adiciona um filtro EMA. As operações ocorrem apenas quando a direção da vela Heikin Ashi concorda com a tendência definida pela EMA. O filtro ajuda a evitar sinais contra a tendência que a abordagem HA pura pode gerar.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `HA_Close > HA_Open` e `Close > EMA`
  - **Vendido**: `HA_Close < HA_Open` e `Close < EMA`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Sinal oposto
- **Stops**: Nenhum
- **Valores padrão**:
  - `EmaLength` = 20
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Heikin Ashi, EMA
  - Stops: Não
  - Complexidade: Baixo
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
