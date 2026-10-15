# Estratégia Color Step Xccx
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no indicador Color Step XCCX. O indicador mede o desvio do preço em relação a uma média suavizada e traça duas linhas escalonadas. Uma operação comprada é aberta quando a linha rápida cai abaixo da linha lenta. Uma operação vendida é aberta quando a linha rápida sobe acima da linha lenta.

## Detalhes

- **Critérios de entrada**:
  - Comprado: linha rápida cruza abaixo da linha lenta
  - Vendido: linha rápida cruza acima da linha lenta
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Comprado: linha rápida cruza acima da linha lenta
  - Vendido: linha rápida cruza abaixo da linha lenta
- **Stops**: Nenhum
- **Valores padrão**:
  - `DPeriod` = 30
  - `MPeriod` = 7
  - `StepSizeFast` = 5
  - `StepSizeSlow` = 30
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Custom, EMA
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
