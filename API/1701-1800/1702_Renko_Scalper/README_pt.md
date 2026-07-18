# Estratégia de Scalping Renko
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia tenta capturar momentum de curto prazo comparando o fechamento atual com o fechamento anterior.
Se a última vela fechar mais alto do que a anterior, a estratégia abre uma posição comprada.
Se a última vela fechar mais baixo do que a anterior, abre uma posição vendida.

Stops e trailing stop opcional são gerenciados através do módulo de proteção integrado.
A abordagem funciona em ambos os lados do mercado e depende exclusivamente da ação do preço.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `Close(t) > Close(t-1)`.
  - **Vendido**: `Close(t) < Close(t-1)`.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto ou stops de proteção.
- **Stops**: Trailing stop, stop loss e take profit opcionais via `StartProtection`.
- **Valores padrão**:
  - `CandleType` = 1 minuto.
  - `StopLossPercent` = 1.
  - `TakeProfitPercent` = 2.
  - `IsTrailingStop` = true.
- **Filtros**:
  - Categoria: Scalping.
  - Direção: Ambos.
  - Indicadores: Nenhum.
  - Stops: Sim.
  - Complexidade: Simples.
  - Período: Curto prazo.
  - Sazonalidade: Não.
  - Redes neurais: Não.
  - Divergência: Não.
  - Nível de risco: Alto.
