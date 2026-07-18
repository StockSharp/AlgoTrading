# Estratégia Dkoderweb Repainting Issue Fix
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia detecta padrões harmônicos usando uma abordagem simples de zigzag e opera quando o preço retorna a um nível de retração de Fibonacci. Quando um padrão altista se forma e o preço recua até a janela de entrada, a estratégia abre uma posição comprada com níveis predefinidos de take‑profit e stop‑loss. Um padrão baixista aciona a mesma lógica na direção oposta.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Padrão harmônico ABCD e preço de fechamento no nível Fibonacci de entrada ou abaixo.
  - **Vendido**: Padrão harmônico ABCD e preço de fechamento no nível Fibonacci de entrada ou acima.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - O preço atinge os níveis Fibonacci de take‑profit ou stop‑loss.
- **Stops**: Sim.
- **Valores padrão**:
  - `TradeSize` = 1
  - `EntryRate` = 0.382
  - `TakeProfitRate` = 0.618
  - `StopLossRate` = -0.618
- **Filtros**:
  - Categoria: Reconhecimento de padrões
  - Direção: Ambos
  - Indicadores: ZigZag
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio

