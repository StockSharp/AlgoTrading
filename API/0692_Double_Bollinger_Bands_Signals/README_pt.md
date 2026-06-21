# Estratégia de Sinais de Bollinger Bands Duplo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza dois conjuntos de Bollinger Bands. Compra quando o preço cruza acima da banda inferior de 3 desvios padrão e vende quando o preço cruza abaixo da banda superior de 3 desvios padrão. As posições são fechadas nas bandas opostas de 2 desvios padrão.

## Detalhes

- **Critérios de entrada**:
  - Comprado: o fechamento cruza acima da banda inferior de 3 SD
  - Vendido: o fechamento cruza abaixo da banda superior de 3 SD
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Comprado: o fechamento cruza acima da banda superior de 2 SD
  - Vendido: o fechamento cruza abaixo da banda inferior de 2 SD
- **Stops**: Nenhum
- **Valores padrão**:
  - `Length` = 20
  - `Width1` = 2m
  - `Width2` = 3m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Bollinger Bands
  - Stops: Não
  - Complexidade: Básico
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
