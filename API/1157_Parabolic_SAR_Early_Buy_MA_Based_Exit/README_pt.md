# Estratégia Parabolic SAR de Compra Antecipada com Saída Baseada em MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa o indicador Parabolic SAR para entrar em operações quando o indicador muda de lado em relação ao preço. Uma média móvel simples fornece uma regra de saída adicional: posições compradas são fechadas quando o preço cai abaixo da média móvel enquanto o SAR está acima do preço.

## Detalhes

- **Critérios de entrada**: SAR muda de lado em relação ao preço.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Para posições compradas, sair quando SAR > preço e preço < MA.
- **Stops**: Não definidos.
- **Valores padrão**:
  - `Acceleration` = 0.02
  - `AccelerationStep` = 0.02
  - `MaxAcceleration` = 0.2
  - `MaPeriod` = 11
  - `CandleType` = 5 minutos
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Parabolic SAR, SMA
  - Stops: Nenhum
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
