# Estratégia Uptrick Intensity Index
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia calcula o Índice de Intensidade de Tendência a partir de três médias móveis e opera nos cruzamentos do TII com sua própria média móvel.

## Detalhes

- **Critérios de entrada**: TII cruza acima da sua SMA (compra) ou abaixo (venda)
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `Ma1Length` = 10
  - `Ma2Length` = 20
  - `Ma3Length` = 50
  - `TiiMaLength` = 50
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SMA, TII
  - Stops: Não
  - Complexidade: Básico
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
