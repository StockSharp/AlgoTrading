# Estratégia FlexiSuperTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia combina um filtro Supertrend com um oscilador de desvio suavizado.
Uma posição é aberta quando o preço concorda com a direção do Supertrend e o
oscilador confirma o momentum.

## Detalhes

- **Critérios de entrada**:
  - Preço acima do Supertrend e desvio (SMA do preço menos Supertrend) > 0 → compra.
  - Preço abaixo do Supertrend e desvio < 0 → venda.
- **Comprado/Vendido**: Ambas as direções podem ser habilitadas.
- **Critérios de saída**:
  - Reversão de tendência quando o preço cruza a linha do Supertrend.
- **Stops**: Sem lógica de stop por padrão.
- **Valores padrão**:
  - Período ATR = 10.
  - Fator ATR = 3.0.
  - Comprimento SMA = 10.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SuperTrend, SMA
  - Stops: Nenhum
  - Complexidade: Básico
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
