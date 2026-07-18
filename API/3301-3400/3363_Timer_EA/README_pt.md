# Estratégia do temporizador EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma porta StockSharp do robô MetaTrader TimerEA. Ele se concentra na abertura e fechamento de negociações em horários programados
com ordens pendentes opcionais, proteção de rastreamento e tratamento de ponto de equilíbrio.

## Lógica de negociação

- **Programação**
  - `OpenTime` aciona a colocação do pedido assim que a primeira vela concluída atinge o minuto configurado.
  - `CloseTime` força a liquidação da posição e opcionalmente cancela as ordens pendentes restantes.
- **Modos de pedido**
  - Entradas de mercado, stop ou limite podem ser selecionadas. As ordens pendentes são colocadas a uma distância configurável (em etapas de preço) e podem
expiram após o número especificado de minutos.
- **Controle de direção**
  - Switches separados permitem permitir negociações longas e/ou curtas. Cada lado envia um pedido por corrida.
- **Gerenciamento de Riscos**
  - Volume fixo ou dimensionamento baseado em equilíbrio (usando `RiskFactor`) imita a seleção do lote original.
  - As distâncias de stop-loss e take-profit são expressas em etapas de preço e recriadas após cada entrada.
  - A lógica de trailing stop mantém o stop em um deslocamento constante quando o lucro excede o buffer `BreakEvenSteps`. A trilha ativa
somente quando a parada já estiver além do deslocamento inicial mais o `TrailingStep`.
- **Proteções**
  - O requisito opcional de ponto de equilíbrio evita o rastreamento até que o limite mínimo de lucro seja alcançado.
  - Pedidos pendentes que expiram são cancelados automaticamente.

## Parâmetros padrão

- Modo de pedido: Mercado.
- Compra/venda aberta: desativado.
- Take Profit/Stop Loss: 10 passos cada.
- Trailing stop e ponto de equilíbrio: desativados.
- Distância pendente: 10 passos com expiração de 60 minutos.
- Dimensionamento do lote: Volume manual = 1,0 (fator de risco = 1,0 para modo balanceado).
- Tipo de vela: período de 1 minuto.

## Notas

- A estratégia opera em velas finalizadas e, portanto, reage com até uma barra de latência.
- StockSharp usa um modelo de posição compensada, portanto, a exposição simultânea longa e curta não é suportada, mesmo que ambas as alternâncias estejam
habilitado.
- As etapas de preço são calculadas com `Security.PriceStep`. Instrumentos sem etapa configurada tratarão distâncias como preço bruto
pontos.
