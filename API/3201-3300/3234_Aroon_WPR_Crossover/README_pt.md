# Estratégia de Aroon WPR Crossover
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia seguidora de tendência que combina cruzamentos de Aroon com filtros de momentum de Williams %R. Uma operação comprada é aberta quando a linha Aroon Up rápida cruza acima de Aroon Down enquanto Williams %R confirma um ambiente de sobrevendido. Operações vendidas seguem a lógica inversa com Williams %R em território de sobrecomprado. Posições abertas podem ser fechadas por reversões de Williams %R ou por níveis opcionais de stop-loss e take-profit medidos em passos de preço.

## Detalhes

- **Critérios de entrada**:
  - Comprado: Aroon Up cruza acima de Aroon Down e Williams %R < `-(100 - OpenWprLevel)`
  - Vendido: Aroon Down cruza acima de Aroon Up e Williams %R > `-OpenWprLevel`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Williams %R sai da zona de sobrevendido/sobrecomprado definida por `CloseWprLevel`
  - Limiares opcionais de take-profit e stop-loss em passos de preço
- **Stops**: Stop-loss e take-profit fixo opcional em passos de preço
- **Valores padrão**:
  - `AroonPeriod` = 14
  - `WprPeriod` = 35
  - `OpenWprLevel` = 20
  - `CloseWprLevel` = 10
  - `TakeProfitSteps` = 0m (desativado)
  - `StopLossSteps` = 0m (desativado)
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Aroon, Williams %R
  - Stops: Opcional
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Moderado
