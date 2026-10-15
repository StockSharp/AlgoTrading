# Estratégia RCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Usa o Índice de Correlação de Classificação e sua média móvel para negociar cruzamentos. Uma posição comprada é aberta quando o RCI sobe acima de sua média móvel. Uma posição vendida é aberta quando cai abaixo. A direção da negociação pode ser restrita a somente comprado ou somente vendido.

## Detalhes
- **Critérios de entrada**: RCI cruzando sua média móvel.
- **Comprado/Vendido**: Configurável (ambos, somente comprado, somente vendido).
- **Critérios de saída**: Cruzamento oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `RciLength` = 10
  - `MaType` = SMA
  - `MaLength` = 14
  - `Direction` = Long & Short
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Configurável
  - Indicadores: RCI, MA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
