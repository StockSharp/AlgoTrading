# Estratégia de Compra/Venda por ZScore
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia usa o Z-Score para detectar desvios extremos em relação à média móvel.
Uma posição é aberta quando o Z-Score cruza acima ou abaixo de um limiar, e um período de resfriamento evita sinais repetidos.

## Detalhes

- **Critérios de entrada**:
  - Vendido quando o Z-Score > `ZThreshold` e o resfriamento de venda passou.
  - Comprado quando o Z-Score < -`ZThreshold` e o resfriamento de compra passou.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Sinal inverso.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `RollingWindow` = 80
  - `ZThreshold` = 2.8
  - `CoolDown` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: SMA, StandardDeviation, Z-Score
  - Stops: Não
  - Complexidade: Básico
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
