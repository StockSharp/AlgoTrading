# Somente Comprado: Rompimento do Intervalo de Abertura (ORB) com Pontos Pivô
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia compra quando o preço rompe acima da máxima do intervalo de abertura e a primeira resistência (R1) dos pivôs diários está acima dessa máxima. Um stop trailing segue os níveis de pivô.

## Detalhes

- **Critérios de entrada**:
  - Após o intervalo de abertura, entrar comprado em um rompimento acima da máxima da sessão se R1 for mais alto.
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**:
  - Stop trailing ajustado aos níveis de pivô e fechamento diário.
- **Stops**: Sim
- **Valores padrão**:
  - `RangeMinutes` = 15
  - `SessionStart` = 09:30
  - `MaxTradesPerDay` = 1
  - `StopLossPercent` = 3
  - `InitialSlType` = Percentage
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Comprado
  - Indicadores: Pivot Points
  - Stops: Sim
  - Complexidade: Médio
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
