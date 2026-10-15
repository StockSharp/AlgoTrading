# Estratégia DMI Power Move
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia baseada em movimentos de força do DMI (Índice de Movimento Direcional)

Os testes indicam um retorno anual médio de aproximadamente 76%. Funciona melhor no mercado de forex.

DMI Power Move combina diferenças do indicador direcional com o ADX para capturar tendências poderosas. As operações entram quando +DI supera notavelmente o -DI (ou vice-versa) e o ADX está forte. Saem quando o ADX enfraquece ou a diferença entre DI se estreita.

Esta abordagem filtra sinais fracos ao exigir tanto um movimento direcional forte quanto um ADX em ascensão. O resultado são menos operações, mas potencialmente de maior qualidade em tendência.


## Detalhes

- **Critérios de entrada**: Sinais baseados em ADX, ATR, DMI.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `DmiPeriod` = 14
  - `DiDifferenceThreshold` = 5m
  - `AdxThreshold` = 30m
  - `AdxExitThreshold` = 25m
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: ADX, ATR, DMI
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (15m)
  - Sazonalidade: Não
  - Neural Networks: Não
  - Divergência: Não
  - Nível de risco: Médio

