# Estratégia de Cruzamento Fast2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no histograma Fast2. O histograma combina o corpo das últimas três velas com pesos de raiz quadrada e aplica duas médias móveis ponderadas. Uma posição comprada é aberta quando a média rápida cruza abaixo da média lenta, e uma posição vendida quando cruza acima.

## Detalhes

- **Critérios de entrada**:
  - Comprado: WMA rápida cruza abaixo da WMA lenta
  - Vendido: WMA rápida cruza acima da WMA lenta
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Cruzamento oposto
- **Stops**: Nenhum
- **Valores padrão**:
  - `FastLength` = 3
  - `SlowLength` = 9
  - `CandleType` = TimeSpan.FromHours(8).TimeFrame()
- **Filtros**:
  - Categoria: Cruzamento
  - Direção: Ambos
  - Indicadores: WeightedMovingAverage
  - Stops: Não
  - Complexidade: Básico
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
