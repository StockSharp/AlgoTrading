# Estratégia Z-Strike Recovery
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Entra comprado quando o z-score da variação de preço supera um limiar e sai após um número fixo de barras.

## Detalhes

- **Critérios de entrada**: Z-score da variação de preço > limiar
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**: Saída por tempo
- **Stops**: Não
- **Valores padrão**:
  - `ZLength` = 16
  - `ZThreshold` = 1.3
  - `ExitPeriods` = 10
- **Filtros**:
  - Categoria: Estatístico
  - Direção: Comprado
  - Indicadores: SMA, StandardDeviation
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
