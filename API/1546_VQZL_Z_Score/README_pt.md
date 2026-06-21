# VQZL Z-Score
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que utiliza o Z-Score relativo a uma média suavizada.

Os testes indicam um retorno anual médio de aproximadamente 42%. Funciona melhor no mercado de ações.

A estratégia calcula uma média móvel suavizada e o desvio padrão para calcular o Z-Score. Quando o preço se desvia além de um limite, entra na direção do movimento.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `Z-Score > threshold`.
  - **Vendido**: `Z-Score < -threshold`.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `PriceSmoothing` = 15
  - `ZLength` = 100
  - `Threshold` = 1.64
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: SMA, StandardDeviation
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
