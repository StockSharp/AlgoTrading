# Estratégia Parabolic SAR de Compra Antecipada com Saída por MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera reversões do Parabolic SAR e fecha posições compradas antecipadamente quando o SAR vira acima do preço e o fechamento fica abaixo de uma média móvel de N períodos.

## Detalhes

- **Critérios de entrada**:
  - Cruzamento do preço com o Parabolic SAR.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Para posições compradas: SAR acima do preço e fechamento abaixo da MA (`MaPeriod`).
  - Para posições vendidas: cruzamento inverso do SAR (gerenciado pela lógica de entrada).
- **Stops**: Nenhum.
- **Valores padrão**:
  - `SarStart` = 0.02
  - `SarIncrement` = 0.02
  - `SarMax` = 0.2
  - `MaPeriod` = 11
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado e vendido
  - Indicadores: Parabolic SAR, SMA
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
