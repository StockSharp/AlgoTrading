# CCI Automatizado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

CCI Automatizado é uma estratégia de reversão que reage a cruzamentos de limiar do Índice de Canal de Commodities (CCI). Vai comprado quando o CCI sobe acima de −80 após cair abaixo de −90, e vai vendido quando o CCI cai abaixo de 80 após superar 90. O sistema duplica operações até um limite definido pelo usuário, gerencia o risco com níveis fixos de take-profit e stop-loss, e acompanha os lucros com um stop de rastreamento configurável.

A abordagem visa capturar mudanças antecipadas de momentum após condições de sobrecompra ou sobrevenda. Ao acumular múltiplas posições e mover o stop conforme o preço avança, tenta capitalizar reversões sustentadas enquanto limita o risco de queda.

## Detalhes

- **Critérios de entrada**: CCI cruza acima de -80 após estar abaixo de -90 para comprados; cruza abaixo de 80 após estar acima de 90 para vendidos.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop loss, take profit ou stop de rastreamento.
- **Stops**: Sim.
- **Valores padrão**:
  - `CciPeriod` = 9
  - `TradesDuplicator` = 3
  - `Volume` = 0.03
  - `StopLoss` = 50
  - `TakeProfit` = 200
  - `TrailingStop` = 50
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: CCI
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
