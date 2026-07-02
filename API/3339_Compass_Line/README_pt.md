# Estratégia da Linha da Bússola
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o especialista CompassLine mesclando dois filtros complementares:

* **Follow Line** — uma trilha de ruptura de bandas Bollinger opcionalmente deslocada em ATR. Quando o preço fecha fora das bandas, a trilha se estende na direção do rompimento e nunca recua enquanto a tendência persistir.
* **Bússola** — uma transformação logística do preço médio em relação à máxima mais alta e à mínima mais baixa na janela de média móvel. O sinal bruto é duplamente suavizado (média triangular) para produzir um estado estável de alta/baixa.

Uma posição é aberta somente quando ambos os filtros concordam com a tendência. Filtragem de tempo opcional e paradas de proteção espelham a lógica MQL.

## Detalhes

- **Critérios de entrada**:
  - A linha de acompanhamento deve apontar para cima (fechamento recente acima da banda superior) para posições compradas ou para baixo (fechamento recente abaixo da faixa inferior) para vendas. O deslocamento ATR pode ser alternado com `UseAtrFilter`.
  - O estado da bússola (com base em `CompassPeriod`) deve ser positivo para posições compradas ou negativo para posições vendidas após a fase de suavização dupla.
  - A negociação é executada somente quando o filtro de sessão opcional (`UseTimeFilter` com `Session` em HHmm-HHmm) permite.
- **Longo/Curto**: Ambas as direções são suportadas.
- **Critérios de saída**:
  - `CloseMode = None` mantém a posição até que ocorra uma entrada oposta ou parada protetora.
  - `CloseMode = BothIndicators` fecha quando Seguir Linha e Bússola invertem a direção simultaneamente.
  - `CloseMode = FollowLineOnly` sai quando Follow Line vira contra a posição.
  - `CloseMode = CompassOnly` sai quando a bússola muda de polaridade.
- **Paradas**: distâncias `TakeProfit` e `StopLoss` (em etapas de segurança) são aplicadas após cada entrada quando maiores que zero.
- **Valores padrão**:
  - `FollowBbPeriod` = 21
  - `FollowBbDeviation` = 1
  - `FollowAtrPeriod` = 5
  - `UseAtrFilter` = falso
  - `CompassPeriod` = 30 (comprimento de suavização = round(CompassPeriod / 3))
  - `CloseMode` = Nenhum
  - `UseTimeFilter` = falso
  - `Session` = "0000-2400"
  - `TakeProfit` = 0
  - `StopLoss` = 0
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Bollinger Bandas, ATR, Média móvel triangular
  - Paradas: Opcional
  - Complexidade: Intermediário
  - Prazo: intradiário
  - Sazonalidade: Não
  - Redes Neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

## Notas adicionais

- A suavização do Compass usa uma janela triangular igual a round(`CompassPeriod` / 3), correspondendo de perto à implementação original do indicador.
- Sequências de sessão como `0930-1600` restringem a negociação à janela especificada enquanto ainda atualizam os estados do indicador fora da sessão.
- As ordens de proteção reutilizam os auxiliares de alto nível de StockSharp para que a lógica seja compatível com os módulos de gerenciamento de risco do portfólio.
