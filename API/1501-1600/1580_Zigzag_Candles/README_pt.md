# Estratégia de Velas Zigzag
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia simples que reage aos pontos pivô do ZigZag. Uma posição comprada é aberta quando um novo pivô mínimo se forma, enquanto uma posição vendida é tomada em novos pivôs máximos.

## Detalhes
- **Critérios de entrada**: Máximos e mínimos de pivô do ZigZag.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Pivô oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `ZigzagLength` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Highest, Lowest
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
