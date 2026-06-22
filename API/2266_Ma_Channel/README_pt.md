# Estratégia de Canal MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Canal MA opera rompimentos de um canal de médias móveis construído a partir dos preços máximos e mínimos. Uma posição é aberta quando o preço sai do canal na direção correspondente e revertida quando a tendência muda. Os limites do canal são calculados a partir de médias móveis exponenciais com um deslocamento fixo.

O sistema é projetado tanto para negociação comprada quanto vendida e reage apenas a velas finalizadas. O objetivo é capturar transições de tendência cedo, evitando o ruído dentro do canal.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O preço rompe acima do canal superior.
  - **Vendido**: O preço rompe abaixo do canal inferior.
- **Critérios de saída**:
  - Um rompimento oposto aciona uma reversão da posição.
- **Indicadores**: Médias móveis exponenciais de máximos e mínimos com comprimento e deslocamento configuráveis.
- **Stops**: Não utilizados por padrão; as operações são fechadas apenas em sinais opostos.
- **Valores padrão**:
  - `Length` = 8
  - `Offset` = 10
  - `CandleType` = velas de 1 hora
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Único
  - Stops: Não
  - Complexidade: Simples
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Moderado
