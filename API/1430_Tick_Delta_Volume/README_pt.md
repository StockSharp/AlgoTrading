# Estratégia de Volume Delta de Tick
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Analisa as variações de preço e volume por tick. O delta é comparado com sua média móvel e desvio padrão para gerar entradas simples baseadas em momentum.

## Detalhes

- **Critérios de entrada**: delta > média + desvio padrão para comprado, delta < -(média + desvio padrão) para vendido
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `Mode` = Volume
  - `Length` = 10
- **Filtros**:
  - Categoria: Volume
  - Direção: Ambos
  - Indicadores: EMA, StandardDeviation
  - Stops: Não
  - Complexidade: Básico
  - Período: Tick
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
