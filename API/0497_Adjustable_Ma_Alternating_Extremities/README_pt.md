# Estratégia de MA Ajustável e Extremidades Alternadas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia usa Bandas de Bollinger para emular a Média Móvel Ajustável com extremidades alternadas. Uma posição comprada é aberta quando o preço rompe acima da banda superior, enquanto uma posição vendida é aberta quando o preço cai abaixo da banda inferior. O estado de ultrapassagem alterna, evitando operações consecutivas na mesma direção.

## Detalhes

- **Critérios de entrada**:
  - Comprar quando a máxima do candle cruza acima da banda superior.
  - Vender quando a mínima do candle cruza abaixo da banda inferior.
- **Critérios de saída**:
  - Rompimento da banda oposta.
- **Indicadores**: Bandas de Bollinger (SMA + desvio padrão).
- **Valores padrão**:
  - Length = 50
  - Multiplier = 2
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Período: Curto/médio prazo
  - Nível de risco: Médio
