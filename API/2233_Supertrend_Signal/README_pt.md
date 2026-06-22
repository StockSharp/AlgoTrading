# Estratégia de Sinal Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia abre posições quando o preço de fechamento cruza a linha SuperTrend. Uma operação comprada é colocada quando o preço sobe acima da linha, e uma operação vendida é aberta quando o preço cai abaixo dela. Sinais opostos fecham e invertem as posições existentes.

O indicador SuperTrend usa o Average True Range (ATR) para acompanhar o preço e definir a tendência predominante. Os parâmetros permitem configurar o período ATR, o multiplicador e o período de tempo das velas.

## Detalhes

- **Critérios de entrada**:
  - Comprado: O preço de fechamento cruza acima do SuperTrend
  - Vendido: O preço de fechamento cruza abaixo do SuperTrend
- **Comprado/Vendido**: Comprado e Vendido
- **Critérios de saída**:
  - Cruzamento oposto do SuperTrend
- **Stops**: Nenhum
- **Valores padrão**:
  - `AtrPeriod` = 5
  - `Multiplier` = 3
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SuperTrend (baseado em ATR)
  - Stops: Não
  - Complexidade: Iniciante
  - Período: Médio prazo
  - Sazonalidade: Nenhum
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
