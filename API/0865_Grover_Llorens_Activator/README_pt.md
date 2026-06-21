# Estratégia Grover Llorens Activator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de trailing adaptativa baseada em ATR que muda de direção quando o preço cruza a linha ativadora interna.

Compra quando a diferença entre o preço e a linha de trailing cruza acima de zero. Vende quando cruza abaixo de zero.

## Detalhes

- **Critérios de entrada**: O preço cruza o stop de trailing calculado a partir do ATR.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `Length` = 480
  - `Multiplier` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: ATR
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
