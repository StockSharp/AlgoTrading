# Estratégia Ticker Pulse Meter + Fear EKG
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina períodos curtos e longos para identificar condições de sobrevenda e recuperações.
Compra quando o percentil combinado cruza o gatilho superior e sai em um cruzamento de tomada de lucro.

## Detalhes

- **Critérios de entrada**: percentil cruza acima de `EntryThresholdHigh` ou abaixo de `OrangeEntryThreshold`
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**: cruzamento abaixo de `ProfitTake`
- **Stops**: Não
- **Valores padrão**:
  - `LookbackShort` = 50
  - `LookbackLong` = 200
  - `ProfitTake` = 95
  - `EntryThresholdHigh` = 20
  - `EntryThresholdLow` = 40
  - `OrangeEntryThreshold` = 95
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Comprado
  - Indicadores: Highest, Lowest
  - Stops: Não
  - Complexidade: Avançado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
