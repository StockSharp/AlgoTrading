# Estratégia Rally Base Drop SND Pivots
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Rally Base Drop SND Pivots opera rompimentos de níveis de oferta e demanda baseados em pivôs. Os pivôs são detectados quando sequências de candles de alta e baixa formam padrões rally-base-drop ou drop-base-rally. Quando o preço cruza esses níveis de pivô, uma posição é aberta. As saídas utilizam um stop baseado em ATR e um alvo de risco-retorno.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O preço cruza acima de um pivô de alta (ou pivô de baixa quando revertido).
  - **Vendido**: O preço cruza abaixo de um pivô de baixa (ou pivô de alta quando revertido).
- **Comprado/Vendido**: Configurável (somente comprado, somente vendido ou ambos).
- **Critérios de saída**:
  - O preço atinge o stop ATR ou o alvo de risco-retorno.
- **Stops**: Multiplicador ATR com alvo de risco-retorno.
- **Valores padrão**:
  - `Length` = 3
  - `Mult` = 1.0
  - `RiskReward` = 6.0
  - `ReverseConditions` = false
- **Filtros**:
  - Categoria: Rompimento de suporte/resistência
  - Direção: Ambos
  - Indicadores: ATR
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
