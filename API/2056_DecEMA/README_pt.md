# Estratégia DecEMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que utiliza o indicador DecEMA para seguir a direção da tendência. O indicador aplica dez suavizações exponenciais consecutivas e as combina para criar uma média móvel de baixo atraso. A estratégia compara os últimos três valores de DecEMA. Se a linha vira para cima e o valor mais recente supera o anterior, compra e fecha qualquer posição vendida. Se a linha vira para baixo e o valor mais recente está abaixo do anterior, vende e fecha qualquer posição comprada.

## Detalhes

- **Critérios de entrada**:
  - Comprado: inclinação do DecEMA vira para cima e valor atual > valor anterior
  - Vendido: inclinação do DecEMA vira para baixo e valor atual < valor anterior
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Comprado: inclinação vira para baixo
  - Vendido: inclinação vira para cima
- **Stops**: Nenhum
- **Valores padrão**:
  - `EmaPeriod` = 3
  - `Length` = 15
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `CandleType` = TimeSpan.FromHours(8).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: DecEMA
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
