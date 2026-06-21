# Estratégia de Distância ao Vetor de Demanda
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no indicador de Distância ao Vetor de Demanda. Compara as distâncias aos vetores de demanda comprado e vendido e opera nos seus cruzamentos.

## Detalhes

- **Critérios de entrada**:
  - Comprado: distância ao vetor longo > distância ao vetor curto
  - Vendido: distância ao vetor longo < distância ao vetor curto
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Sinal oposto
- **Stops**: Nenhum
- **Valores padrão**:
  - `Length` = 100
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Highest, Lowest
  - Stops: Não
  - Complexidade: Básico
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
