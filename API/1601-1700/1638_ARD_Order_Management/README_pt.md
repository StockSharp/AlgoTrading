# Estratégia ARD de Gerenciamento de Ordens
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que usa o indicador DeMarker cruzando um limiar de 0.5 para abrir posições.

Quando o DeMarker cai abaixo do limiar depois de estar acima, a estratégia compra. Quando o DeMarker sobe acima do limiar depois de estar abaixo, ela vende. A saída ocorre no sinal oposto. Não é utilizado stop-loss nem take-profit.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `DeMarker cruza abaixo de Threshold`
  - Vendido: `DeMarker cruza acima de Threshold`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `DeMarkerPeriod` = 2
  - `Threshold` = 0.5
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Indicador
  - Direção: Ambos
  - Indicadores: DeMarker
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
