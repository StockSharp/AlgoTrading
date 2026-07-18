# Estratégia AFL Winner Sign
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no indicador AFL WinnerSign. Aplica um oscilador estocástico de duplo suavizamento a uma série de preços ponderada por volume. Uma posição comprada é aberta quando a linha estocástica rápida cruza acima da linha lenta, e uma posição vendida é aberta quando a linha rápida cruza abaixo da linha lenta.

## Detalhes

- **Critérios de entrada**:
  - Comprado: %K rápido cruza acima do %D lento
  - Vendido: %K rápido cruza abaixo do %D lento
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: O sinal oposto fecha ou reverte a posição
- **Stops**: Baseados em percentual usando `StartProtection`
- **Valores padrão**:
  - `Period` = 10
  - `KPeriod` = 5
  - `DPeriod` = 5
  - `CandleType` = `TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Oscilador Estocástico
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
