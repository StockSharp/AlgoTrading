# Estratégia MA Crossover com Zonas de Demanda/Oferta e SLTP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina o cruzamento de médias móveis simples curta/longa com a detecção de zonas de demanda e oferta. O sistema busca cruzamentos que ocorram perto de zonas de demanda ou oferta recentemente confirmadas, então entra na direção do cruzamento e gerencia a posição com stop-loss e take-profit de percentual fixo.

## Detalhes

- **Critérios de entrada**:
  - Comprado: SMA curta cruza acima da SMA longa perto de uma zona de demanda.
  - Vendido: SMA curta cruza abaixo da SMA longa perto de uma zona de oferta.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - O preço atinge os níveis de take-profit ou stop-loss.
- **Stops**: Stop-loss e take-profit baseados em percentual.
- **Valores padrão**:
  - `ShortMaLength` = 9
  - `LongMaLength` = 21
  - `ZoneLookback` = 50
  - `ZoneStrength` = 2
  - `StopLossPercent` = 1
  - `TakeProfitPercent` = 2
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SMA, Highest, Lowest
  - Stops: Sim
  - Complexidade: Básico
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
